using System;

namespace Direct.Desktop.Services;

public interface IEventService
{
    event EventHandler<ContactAddedLocallyEventArgs>? ContactAddedLocally;

    void RaiseContactAdded(Guid userId, string nickname);
}

public class EventService : IEventService
{
    public event EventHandler<ContactAddedLocallyEventArgs>? ContactAddedLocally;

    public void RaiseContactAdded(Guid userId, string nickname)
    {
        ContactAddedLocally?.Invoke(this, new ContactAddedLocallyEventArgs(userId, nickname));
    }
}

public class ContactAddedLocallyEventArgs : EventArgs
{
    public ContactAddedLocallyEventArgs(Guid userId, string nickname)
    {
        UserId = userId;
        Nickname = nickname;
    }

    public Guid UserId { get; init; }
    public string Nickname { get; init; }
}
