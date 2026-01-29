using System.Collections.Concurrent;
using Blackhole.Server.Models;

namespace Blackhole.Server.Storage;

public sealed class InMemoryEmailStore : IEmailStore
{
    private readonly ConcurrentQueue<EmailMessage> _queue = new();
    private readonly ConcurrentDictionary<Guid, EmailMessage> _byId = new();

    private readonly int _maxItems;

    public InMemoryEmailStore(int maxItems = 500)
    {
        _maxItems = Math.Max(10, maxItems);
    }

    public void Add(EmailMessage message)
    {
        _byId[message.Id] = message;
        _queue.Enqueue(message);

        // best-effort trimming
        while (_queue.Count > _maxItems && _queue.TryDequeue(out var removed))
        {
            _byId.TryRemove(removed.Id, out _);
        }
    }

    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
        _byId.Clear();
    }


    public IReadOnlyList<EmailMessage> List(int take = 50)
        => _queue.Reverse().Take(Math.Clamp(take, 1, 200)).ToList();

    public EmailMessage? Get(Guid id)
        => _byId.TryGetValue(id, out var msg) ? msg : null;
}
