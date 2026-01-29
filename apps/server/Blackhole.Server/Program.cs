using Blackhole.Server.Data;
using Blackhole.Server.Realtime;
using Blackhole.Server.Smtp;
using Blackhole.Server.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.SignalR;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Blackhole Server",
        Version = "v1"
    });
});

// CORS (Next.js dev обычно 3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("dev", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://localhost:8080")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


// SignalR
builder.Services.AddSignalR();

// DB
var cs = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrWhiteSpace(cs))
{
    builder.Services.AddDbContextFactory<BlackholeDbContext>(o => o.UseNpgsql(cs));
    builder.Services.AddSingleton<IEmailStore, PostgresEmailStore>();
}
else
{
    // fallback, если ты пока не хочешь БД
    builder.Services.AddSingleton<IEmailStore>(_ => new InMemoryEmailStore(maxItems: 500));
}

// SMTP listener
builder.Services.AddHostedService<SmtpListenerService>();

var app = builder.Build();

app.UseRouting();
app.UseCors("dev");

app.MapHub<EventsHub>("/hubs/events");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api/health", () => Results.Ok(new
{
    ok = true,
    service = "blackhole-server",
    utc = DateTimeOffset.UtcNow
}));

app.MapGet("/api/emails", (IEmailStore store, int? take) =>
{
    var items = store.List(take ?? 50)
        .Select(x => new
        {
            x.Id,
            x.ReceivedAtUtc,
            x.Helo,
            x.MailFrom,
            RcptTo = x.RcptTo.Select(r => r.Address),
            x.Subject,
            From = x.HeaderFrom,
            To = x.HeaderTo
        });

    return Results.Ok(items);
});

app.MapGet("/api/emails/{id:guid}", (IEmailStore store, Guid id) =>
{
    var msg = store.Get(id);
    return msg is null ? Results.NotFound() : Results.Ok(msg);
});

app.MapGet("/api/emails/{id:guid}/raw", (IEmailStore store, Guid id) =>
{
    var msg = store.Get(id);
    return msg is null ? Results.NotFound() : Results.Text(msg.Raw, "text/plain; charset=utf-8");
});

app.MapDelete("/api/emails", (IEmailStore store) =>
{
    store.Clear();
    return Results.NoContent();
});

app.MapMethods("/hooks/{name}", new[] { "GET", "POST", "PUT", "PATCH", "DELETE" },
async (HttpRequest req, string name, IDbContextFactory<BlackholeDbContext> dbFactory, IHubContext<EventsHub> hub) =>
{
    using var db = dbFactory.CreateDbContext();

    // read body as text (MVP)
    req.EnableBuffering();
    string bodyText = "";
    using (var reader = new StreamReader(req.Body, Encoding.UTF8, leaveOpen: true))
    {
        bodyText = await reader.ReadToEndAsync();
        req.Body.Position = 0;
    }

    var ev = new WebhookEventEntity
    {
        Id = Guid.NewGuid(),
        ReceivedAtUtc = DateTimeOffset.UtcNow,
        Name = name,
        Method = req.Method,
        Path = req.Path,
        QueryString = req.QueryString.HasValue ? req.QueryString.Value! : "",
        ContentType = req.ContentType ?? "",
        BodyText = bodyText,
        Headers = req.Headers.Select(h => new WebhookHeaderEntity
        {
            Key = h.Key,
            Value = string.Join(", ", h.Value.ToArray())
        }).ToList()
    };

    db.Webhooks.Add(ev);
    await db.SaveChangesAsync();

    await hub.Clients.All.SendAsync("webhook_received", new
    {
        id = ev.Id,
        receivedAtUtc = ev.ReceivedAtUtc,
        name = ev.Name,
        method = ev.Method
    });

    return Results.Ok(new { ok = true, id = ev.Id });
});

app.MapGet("/api/webhooks", async (IDbContextFactory<BlackholeDbContext> dbFactory, string? name, int? take) =>
{
    using var db = dbFactory.CreateDbContext();

    var q = db.Webhooks.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(name))
        q = q.Where(x => x.Name == name);

    var items = await q.OrderByDescending(x => x.ReceivedAtUtc)
        .Take(Math.Clamp(take ?? 50, 1, 200))
        .Select(x => new
        {
            x.Id,
            x.ReceivedAtUtc,
            x.Name,
            x.Method,
            x.Path,
            x.ContentType
        })
        .ToListAsync();

    return Results.Ok(items);
});

app.MapGet("/api/webhooks/{id:guid}", async (IDbContextFactory<BlackholeDbContext> dbFactory, Guid id) =>
{
    using var db = dbFactory.CreateDbContext();

    var ev = await db.Webhooks.AsNoTracking()
        .Include(x => x.Headers)
        .FirstOrDefaultAsync(x => x.Id == id);

    return ev is null ? Results.NotFound() : Results.Ok(ev);
});


app.Run();
