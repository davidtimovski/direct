using System;

namespace Direct.Desktop.Services;

public interface IEventService
{
    event EventHandler<ContactAddedLocallyEventArgs>? ContactAddedLocally;
    event EventHandler<ContactEditedLocallyEventArgs>? ContactEditedLocally;

    void RaiseContactAdded(Guid userId, string nickname);
    void RaiseContactEdited(Guid userId, string nickname);
}

public class EventService : IEventService
{
    public event EventHandler<ContactAddedLocallyEventArgs>? ContactAddedLocally;
    public event EventHandler<ContactEditedLocallyEventArgs>? ContactEditedLocally;

    public void RaiseContactAdded(Guid userId, string nickname)
    {
        ContactAddedLocally?.Invoke(this, new ContactAddedLocallyEventArgs(userId, nickname));
    }

    public void RaiseContactEdited(Guid userId, string nickname)
    {
        ContactEditedLocally?.Invoke(this, new ContactEditedLocallyEventArgs(userId, nickname));
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

public class ContactEditedLocallyEventArgs : EventArgs
{
    public ContactEditedLocallyEventArgs(Guid userId, string nickname)
    {
        UserId = userId;
        Nickname = nickname;
    }

    public Guid UserId { get; init; }
    public string Nickname { get; init; }
}
