using System;

namespace Direct.Desktop.Services;

public interface IEventService
{
    event EventHandler<ContactAddedEventArgs>? ContactAdded;

    void RaiseContactAdded(Guid userId, string nickname);
}

public class EventService : IEventService
{
    public event EventHandler<ContactAddedEventArgs>? ContactAdded;

    public void RaiseContactAdded(Guid userId, string nickname)
    {
        ContactAdded?.Invoke(this, new ContactAddedEventArgs(userId, nickname));
    }
}

public class ContactAddedEventArgs : EventArgs
{
    public ContactAddedEventArgs(Guid userId, string nickname)
    {
        UserId = userId;
        Nickname = nickname;
    }

    public Guid UserId { get; init; }
    public string Nickname { get; init; }
}
