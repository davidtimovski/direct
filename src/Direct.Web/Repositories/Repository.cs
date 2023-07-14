using Direct.Web.Repositories.Models;
using Marten;

namespace Direct.Web.Repositories;

public interface IRepository
{
    Task<User?> GetUserAsync(string passwordHash);
    Task<User> CreateUserAsync(string passwordHash);
    Task<IReadOnlyList<Message>> GetAllAsync(Guid userId);
    Task<Message> CreateMessageAsync(Guid senderId, Guid recipientId, string text);
    Task<Message> UpdateMessageAsync(Guid id, Guid senderId, string text);
}

public class Repository : IRepository
{
    private readonly IDocumentStore _store;

    public Repository(IDocumentStore store)
    {
        _store = store;
    }

    public async Task<User?> GetUserAsync(string passwordHash)
    {
        using var session = _store.LightweightSession();

        return await session.Query<User>().Where(x => x.PasswordHash == passwordHash).FirstOrDefaultAsync();
    }

    public async Task<User> CreateUserAsync(string passwordHash)
    {
        var user = new User { PasswordHash = passwordHash, CreatedDate = DateTime.UtcNow };

        using var session = _store.LightweightSession();
        session.Store(user);

        await session.SaveChangesAsync();

        return user;
    }

    public async Task<IReadOnlyList<Message>> GetAllAsync(Guid userId)
    {
        using var session = _store.LightweightSession();

        return await session.Query<Message>().Where(x => x.SenderId == userId || x.RecipientId == userId).ToListAsync();
    }

    public async Task<Message> CreateMessageAsync(Guid senderId, Guid recipientId, string text)
    {
        var message = new Message { SenderId = senderId, RecipientId = recipientId, Text = text, SentAt = DateTime.UtcNow };

        using var session = _store.LightweightSession();
        session.Store(message);

        await session.SaveChangesAsync();

        return message;
    }

    public async Task<Message> UpdateMessageAsync(Guid id, Guid senderId, string text)
    {
        using var session = _store.LightweightSession();

        var message = await session.Query<Message>().Where(x => x.Id == id && x.SenderId == senderId).FirstOrDefaultAsync();
        if (message is null)
        {
            throw new InvalidOperationException();
        }

        message.Text = text;
        message.EditedAt = DateTime.UtcNow;
        session.Store(message);

        await session.SaveChangesAsync();

        return message;
    }
}
