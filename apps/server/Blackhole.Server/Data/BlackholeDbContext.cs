using Microsoft.EntityFrameworkCore;

namespace Blackhole.Server.Data;



public sealed class BlackholeDbContext : DbContext
{
    public BlackholeDbContext(DbContextOptions<BlackholeDbContext> options) : base(options) { }

    public DbSet<EmailEntity> Emails => Set<EmailEntity>();
    public DbSet<EmailRecipientEntity> EmailRecipients => Set<EmailRecipientEntity>();

    public DbSet<WebhookEventEntity> Webhooks => Set<WebhookEventEntity>();
    public DbSet<WebhookHeaderEntity> WebhookHeaders => Set<WebhookHeaderEntity>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<EmailEntity>(e =>
        {
            e.ToTable("emails");
            e.HasKey(x => x.Id);
            e.Property(x => x.ReceivedAtUtc).IsRequired();
            e.Property(x => x.Raw).IsRequired();

            e.HasMany(x => x.Recipients)
                .WithOne(r => r.Email!)
                .HasForeignKey(r => r.EmailId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => x.ReceivedAtUtc);
            e.HasIndex(x => x.MailFrom);
            e.HasIndex(x => x.Subject);
        });

        b.Entity<EmailRecipientEntity>(r =>
        {
            r.ToTable("email_recipients");
            r.HasKey(x => x.Id);
            r.Property(x => x.Address).IsRequired();
            r.HasIndex(x => x.Address);
        });
        b.Entity<WebhookEventEntity>(e =>
{
    e.ToTable("webhook_events");
    e.HasKey(x => x.Id);

    e.Property(x => x.ReceivedAtUtc).IsRequired();
    e.Property(x => x.Name).IsRequired();

    e.HasMany(x => x.Headers)
        .WithOne(h => h.WebhookEvent!)
        .HasForeignKey(h => h.WebhookEventId)
        .OnDelete(DeleteBehavior.Cascade);

    e.HasIndex(x => x.ReceivedAtUtc);
    e.HasIndex(x => x.Name);
});

        b.Entity<WebhookHeaderEntity>(h =>
        {
            h.ToTable("webhook_headers");
            h.HasKey(x => x.Id);
            h.Property(x => x.Key).IsRequired();
        });

    }
}

public sealed class EmailEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }

    public string? Helo { get; set; }
    public string? MailFrom { get; set; }

    public string? Subject { get; set; }
    public string? HeaderFrom { get; set; }
    public string? HeaderTo { get; set; }

    public string? TextBody { get; set; }
    public string? HtmlBody { get; set; }

    public string Raw { get; set; } = "";

    public List<EmailRecipientEntity> Recipients { get; set; } = new();
}

public sealed class EmailRecipientEntity
{
    public long Id { get; set; }
    public Guid EmailId { get; set; }
    public EmailEntity? Email { get; set; }

    public string Address { get; set; } = "";
}


public sealed class WebhookEventEntity
{
    public Guid Id { get; set; }
    public DateTimeOffset ReceivedAtUtc { get; set; }

    public string Name { get; set; } = "";   // {name} из /hooks/{name}
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public string QueryString { get; set; } = "";

    public string ContentType { get; set; } = "";
    public string BodyText { get; set; } = "";  // MVP: храним как text

    public List<WebhookHeaderEntity> Headers { get; set; } = new();
}

public sealed class WebhookHeaderEntity
{
    public long Id { get; set; }
    public Guid WebhookEventId { get; set; }
    public WebhookEventEntity? WebhookEvent { get; set; }

    public string Key { get; set; } = "";
    public string Value { get; set; } = "";
}
