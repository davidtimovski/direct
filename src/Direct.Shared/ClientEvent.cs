namespace Direct.Shared;

public static class ClientEvent
{
    public const string ConnectedContactsRetrieved = nameof(ConnectedContactsRetrieved);

    public const string ContactConnected = nameof(ContactConnected);
    public const string ContactDisconnected = nameof(ContactDisconnected);
    public const string ContactAdded = nameof(ContactAdded);
    public const string ContactRemoved = nameof(ContactRemoved);
    public const string ContactUpdatedProfileImage = nameof(ContactUpdatedProfileImage);

    public const string MessageSent = nameof(MessageSent);
    public const string MessageSendingFailed = nameof(MessageSendingFailed);
    public const string MessageUpdated = nameof(MessageUpdated);
    public const string MessageUpdatingFailed = nameof(MessageUpdatingFailed);

    /// <summary>
    /// The server has signaled for message upstream to start because a contact requested to pull messages.
    /// </summary>
    public const string StartMessageUpstream = nameof(StartMessageUpstream);

    /// <summary>
    /// The server has signaled for message downstream to start.
    /// </summary>
    public const string StartMessageDownstream = nameof(StartMessageDownstream);
}
