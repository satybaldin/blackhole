using Blackhole.Server.Models;

namespace Blackhole.Server.Storage;

public interface IEmailStore
{
    void Add(EmailMessage message);
    IReadOnlyList<EmailMessage> List(int take = 50);
    EmailMessage? Get(Guid id);
    void Clear();
}
