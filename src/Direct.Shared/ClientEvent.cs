namespace Direct.Shared;

public static class ClientEvent
{
    public const string ConnectedContactsRetrieved = "ConnectedContactsRetrieved";

    public const string ContactConnected = "ContactConnected";
    public const string ContactDisconnected = "ContactDisconnected";
    public const string ContactAdded = "ContactAdded";
    public const string ContactRemoved = "ContactRemoved";

    public const string MessageSent = "MessageSent";
    public const string MessageSendingFailed = "MessageSendingFailed";
    public const string MessageUpdated = "MessageUpdated";
    public const string MessageUpdatingFailed = "MessageUpdatingFailed";

    /// <summary>
    /// The server has signaled for message upstream to start because a contact requested to pull messages.
    /// </summary>
    public const string StartMessageUpstream = "StartMessageUpstream";

    /// <summary>
    /// The server has signaled for message downstream to start.
    /// </summary>
    public const string StartMessageDownstream = "StartMessageDownstream";
}
