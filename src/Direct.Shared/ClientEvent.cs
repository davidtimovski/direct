namespace Direct.Shared;

public static class ClientEvent
{
    public const string Connected = "Connected";

    public const string ContactConnected = "ContactConnected";
    public const string ContactDisconnected = "ContactDisconnected";
    public const string ContactAdded = "ContactAdded";
    public const string ContactRemoved = "ContactRemoved";

    public const string MessageSent = "MessageSent";
    public const string MessageSendingFailed = "MessageSendingFailed";
    public const string MessageUpdated = "MessageUpdated";
    public const string MessageUpdatingFailed = "MessageUpdatingFailed";
}
