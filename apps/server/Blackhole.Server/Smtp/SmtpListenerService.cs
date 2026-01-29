using System.Net;
using System.Net.Sockets;
using System.Text;
using Blackhole.Server.Models;
using Blackhole.Server.Realtime;
using Blackhole.Server.Storage;
using Microsoft.AspNetCore.SignalR;
using MimeKit;

namespace Blackhole.Server.Smtp;

public sealed class SmtpListenerService : BackgroundService
{
    private const string ServerName = "blackhole.local";

    private readonly ILogger<SmtpListenerService> _logger;
    private readonly IEmailStore _store;
    private readonly IHubContext<EventsHub> _hub;
    private readonly int _port;
    private readonly int _maxMessageBytes;

    public SmtpListenerService(
        ILogger<SmtpListenerService> logger,
        IConfiguration cfg,
        IEmailStore store,
        IHubContext<EventsHub> hub)
    {
        _logger = logger;
        _store = store;
        _hub = hub;
        _port = cfg.GetValue<int?>("Smtp:Port") ?? 2525;
        _maxMessageBytes = cfg.GetValue<int?>("Smtp:MaxMessageBytes") ?? 25 * 1024 * 1024;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        _logger.LogInformation("SMTP listener started on 0.0.0.0:{Port}", _port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                _ = Task.Run(() => HandleClientAsync(client, stoppingToken), stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
            _logger.LogInformation("SMTP listener stopped");
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var _ = client;
        client.NoDelay = true;

        var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
        _logger.LogDebug("SMTP client connected: {Remote}", remote);

        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, bufferSize: 4096, leaveOpen: true);
        using var writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 4096, leaveOpen: true)
        {
            NewLine = "\r\n",
            AutoFlush = true
        };

        var session = new SmtpSession();
        await writer.WriteLineAsync($"220 {ServerName} ESMTP ready");

        while (!ct.IsCancellationRequested && client.Connected)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync();
            }
            catch
            {
                break;
            }

            if (line is null) break;
            line = line.TrimEnd();

            if (line.Length == 0)
            {
                await writer.WriteLineAsync("500 Empty command");
                continue;
            }

            var (command, args) = SplitCommand(line);

            switch (command)
            {
                case "EHLO":
                case "HELO":
                    {
                        session.Helo = args?.Trim();
                        await WriteEhloAsync(writer, _maxMessageBytes);
                        break;
                    }

                case "NOOP":
                    await writer.WriteLineAsync("250 OK");
                    break;

                case "RSET":
                    session.Reset();
                    await writer.WriteLineAsync("250 OK");
                    break;

                case "QUIT":
                    await writer.WriteLineAsync("221 Bye");
                    return;

                case "MAIL":
                    {
                        if (!TryParseMailFrom(args, out var from))
                        {
                            await writer.WriteLineAsync("501 Syntax: MAIL FROM:<address>");
                            break;
                        }

                        session.MailFrom = from;
                        session.RcptTo.Clear();
                        await writer.WriteLineAsync("250 OK");
                        break;
                    }

                case "RCPT":
                    {
                        if (!TryParseRcptTo(args, out var to))
                        {
                            await writer.WriteLineAsync("501 Syntax: RCPT TO:<address>");
                            break;
                        }

                        session.RcptTo.Add(new EmailRecipient(to));
                        await writer.WriteLineAsync("250 OK");
                        break;
                    }

                case "DATA":
                    {
                        if (string.IsNullOrWhiteSpace(session.MailFrom) || session.RcptTo.Count == 0)
                        {
                            await writer.WriteLineAsync("503 Bad sequence of commands");
                            break;
                        }

                        await writer.WriteLineAsync("354 End data with <CR><LF>.<CR><LF>");

                        var raw = await ReadDataAsync(reader, _maxMessageBytes, ct);
                        var msg = new EmailMessage
                        {
                            Helo = session.Helo,
                            MailFrom = session.MailFrom,
                            RcptTo = session.RcptTo.ToList(),
                            Raw = raw
                        };

                        ParseWithMimeKit(msg);

                        _store.Add(msg);

                        await _hub.Clients.All.SendAsync(
                            "email_received",
                            new
                            {
                                id = msg.Id,
                                receivedAtUtc = msg.ReceivedAtUtc,
                                mailFrom = msg.MailFrom,
                                rcptTo = msg.RcptTo.Select(r => r.Address),
                                subject = msg.Subject
                            },
                            ct);

                        await writer.WriteLineAsync("250 OK queued");
                        break;
                    }

                case "VRFY":
                case "EXPN":
                case "HELP":
                    await writer.WriteLineAsync("252 Cannot VRFY user");
                    break;

                case "STARTTLS":
                    await writer.WriteLineAsync("454 TLS not available");
                    break;

                case "AUTH":
                    {
                        // AUTH PLAIN <base64> | AUTH PLAIN (then challenge) | AUTH LOGIN (then user/pass)
                        if (TryHandleAuth(args, reader, writer, session))
                            break;

                        await writer.WriteLineAsync("501 Syntax: AUTH PLAIN|LOGIN");
                        break;
                    }


                default:
                    await writer.WriteLineAsync("502 Command not implemented");
                    break;
            }
        }
    }

    private static bool TryHandleAuth(
    string? args,
    StreamReader reader,
    StreamWriter writer,
    SmtpSession session)
    {
        if (string.IsNullOrWhiteSpace(args))
            return false;

        var parts = args.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var mech = parts[0].ToUpperInvariant();
        var initial = parts.Length > 1 ? parts[1] : null;

        if (mech == "PLAIN")
        {
            // AUTH PLAIN <b64>  OR  AUTH PLAIN (then 334)
            var b64 = initial;
            if (string.IsNullOrWhiteSpace(b64))
            {
                writer.WriteLine("334 "); // empty challenge
                b64 = reader.ReadLine();
            }

            if (string.IsNullOrWhiteSpace(b64))
            {
                writer.WriteLine("501 Invalid AUTH PLAIN");
                return true;
            }

            if (!TryDecodeBase64(b64, out var decoded))
            {
                writer.WriteLine("501 Invalid base64");
                return true;
            }

            // decoded format: [authzid]\0authcid\0passwd
            var fields = decoded.Split('\0');
            var user = fields.Length >= 2 ? fields[1] : "";
            // var pass = fields.Length >= 3 ? fields[2] : "";

            session.IsAuthenticated = true;
            session.AuthUser = string.IsNullOrWhiteSpace(user) ? "unknown" : user;

            writer.WriteLine("235 2.7.0 Authentication successful");
            return true;
        }

        if (mech == "LOGIN")
        {
            // AUTH LOGIN  OR  AUTH LOGIN <b64user>
            string? userB64 = initial;
            if (string.IsNullOrWhiteSpace(userB64))
            {
                writer.WriteLine("334 VXNlcm5hbWU6"); // "Username:"
                userB64 = reader.ReadLine();
            }

            if (!TryDecodeBase64(userB64 ?? "", out var user))
            {
                writer.WriteLine("501 Invalid base64 username");
                return true;
            }

            writer.WriteLine("334 UGFzc3dvcmQ6"); // "Password:"
            var passB64 = reader.ReadLine();

            // password can be ignored in fake mode
            _ = TryDecodeBase64(passB64 ?? "", out var _);

            session.IsAuthenticated = true;
            session.AuthUser = string.IsNullOrWhiteSpace(user) ? "unknown" : user;

            writer.WriteLine("235 2.7.0 Authentication successful");
            return true;
        }

        // unknown mechanism
        writer.WriteLine("504 Unrecognized authentication type");
        return true;
    }

    private static bool TryDecodeBase64(string b64, out string decoded)
    {
        decoded = "";
        try
        {
            var bytes = Convert.FromBase64String(b64.Trim());
            decoded = Encoding.UTF8.GetString(bytes);
            return true;
        }
        catch
        {
            return false;
        }
    }


    private static async Task WriteEhloAsync(StreamWriter writer, int maxMessageBytes)
    {
        await writer.WriteLineAsync($"250-{ServerName}");
        await writer.WriteLineAsync("250-PIPELINING");
        await writer.WriteLineAsync("250-8BITMIME");
        await writer.WriteLineAsync($"250-SIZE {maxMessageBytes}");
        await writer.WriteLineAsync("250-AUTH PLAIN LOGIN");
        await writer.WriteLineAsync("250 OK");
    }


    private static (string Command, string? Args) SplitCommand(string line)
    {
        var space = line.IndexOf(' ');
        if (space < 0) return (line.ToUpperInvariant(), null);

        var cmd = line[..space].Trim().ToUpperInvariant();
        var args = line[(space + 1)..].Trim();
        return (cmd, args.Length == 0 ? null : args);
    }

    private static bool TryParseMailFrom(string? args, out string from)
    {
        from = "";
        if (string.IsNullOrWhiteSpace(args)) return false;
        if (!args.StartsWith("FROM:", StringComparison.OrdinalIgnoreCase)) return false;
        from = ExtractPath(args["FROM:".Length..]);
        return !string.IsNullOrWhiteSpace(from);
    }

    private static bool TryParseRcptTo(string? args, out string to)
    {
        to = "";
        if (string.IsNullOrWhiteSpace(args)) return false;
        if (!args.StartsWith("TO:", StringComparison.OrdinalIgnoreCase)) return false;
        to = ExtractPath(args["TO:".Length..]);
        return !string.IsNullOrWhiteSpace(to);
    }

    private static string ExtractPath(string input)
    {
        var s = input.Trim();

        if (s.StartsWith('<'))
        {
            var end = s.IndexOf('>');
            if (end > 1)
                return s[1..end].Trim();
        }

        var space = s.IndexOf(' ');
        if (space >= 0) s = s[..space];

        return s.Trim();
    }

    private static async Task<string> ReadDataAsync(StreamReader reader, int maxBytes, CancellationToken ct)
    {
        var sb = new StringBuilder(capacity: Math.Min(maxBytes, 64 * 1024));
        var bytes = 0;

        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line is null) break;

            if (line == ".") break;

            if (line.StartsWith("..", StringComparison.Ordinal))
                line = line[1..];

            sb.Append(line).Append("\r\n");

            bytes += Encoding.UTF8.GetByteCount(line) + 2;
            if (bytes > maxBytes)
                break;
        }

        return sb.ToString();
    }

    private static void ParseWithMimeKit(EmailMessage msg)
    {
        try
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(msg.Raw));
            var mime = MimeMessage.Load(ms);

            msg.Subject = mime.Subject;
            msg.HeaderFrom = mime.From.ToString();
            msg.HeaderTo = mime.To.ToString();

            msg.TextBody = mime.TextBody;
            msg.HtmlBody = mime.HtmlBody;
        }
        catch
        {
            // raw already stored
        }
    }

    private sealed class SmtpSession
    {
        public string? Helo { get; set; }
        public string? MailFrom { get; set; }
        public List<EmailRecipient> RcptTo { get; } = new();
        public bool IsAuthenticated { get; set; }
        public string? AuthUser { get; set; }
        public void Reset()
        {
            Helo = null;
            MailFrom = null;
            RcptTo.Clear();
        }
    }
}
