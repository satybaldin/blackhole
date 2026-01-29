using Blackhole.Server.Data;
using Blackhole.Server.Models;
using Microsoft.EntityFrameworkCore;

namespace Blackhole.Server.Storage;

public sealed class PostgresEmailStore : IEmailStore
{
    private readonly IDbContextFactory<BlackholeDbContext> _dbFactory;

    public PostgresEmailStore(IDbContextFactory<BlackholeDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public void Add(EmailMessage message)
    {
        using var db = _dbFactory.CreateDbContext();

        var entity = new EmailEntity
        {
            Id = message.Id,
            ReceivedAtUtc = message.ReceivedAtUtc,

            Helo = message.Helo,
            MailFrom = message.MailFrom,

            Subject = message.Subject,
            HeaderFrom = message.HeaderFrom,
            HeaderTo = message.HeaderTo,

            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,

            Raw = message.Raw,
            Recipients = message.RcptTo.Select(r => new EmailRecipientEntity { Address = r.Address }).ToList()
        };

        db.Emails.Add(entity);
        db.SaveChanges();
    }

    public IReadOnlyList<EmailMessage> List(int take = 50)
    {
        using var db = _dbFactory.CreateDbContext();

        var items = db.Emails
            .AsNoTracking()
            .Include(x => x.Recipients)
            .OrderByDescending(x => x.ReceivedAtUtc)
            .Take(Math.Clamp(take, 1, 200))
            .ToList();

        return items.Select(ToModel).ToList();
    }

    public EmailMessage? Get(Guid id)
    {
        using var db = _dbFactory.CreateDbContext();

        var entity = db.Emails
            .AsNoTracking()
            .Include(x => x.Recipients)
            .FirstOrDefault(x => x.Id == id);

        return entity is null ? null : ToModel(entity);
    }

    public void Clear()
    {
        using var db = _dbFactory.CreateDbContext();
        db.Database.ExecuteSqlRaw("TRUNCATE TABLE email_recipients RESTART IDENTITY CASCADE;");
        db.Database.ExecuteSqlRaw("TRUNCATE TABLE emails RESTART IDENTITY CASCADE;");
    }

    private static EmailMessage ToModel(EmailEntity e)
        => new EmailMessage
        {
            Id = e.Id,
            ReceivedAtUtc = e.ReceivedAtUtc,
            Helo = e.Helo,
            MailFrom = e.MailFrom,
            Subject = e.Subject,
            HeaderFrom = e.HeaderFrom,
            HeaderTo = e.HeaderTo,
            TextBody = e.TextBody,
            HtmlBody = e.HtmlBody,
            Raw = e.Raw,
            RcptTo = e.Recipients.Select(r => new EmailRecipient(r.Address)).ToList()
        };
}
