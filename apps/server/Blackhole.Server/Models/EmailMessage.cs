namespace Blackhole.Server.Models;

public sealed record EmailRecipient(string Address);

public sealed class EmailMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public DateTimeOffset ReceivedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public string? Helo { get; init; }
    public string? MailFrom { get; init; }
    public List<EmailRecipient> RcptTo { get; init; } = new();

    public string Raw { get; init; } = "";

    public string? Subject { get; set; }
    public string? HeaderFrom { get; set; }
    public string? HeaderTo { get; set; }

    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }
}
