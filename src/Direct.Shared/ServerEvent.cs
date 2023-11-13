namespace Direct.Shared;

public static class ServerEvent
{
    public const string UserJoin = nameof(UserJoin);

    public const string AddContact = nameof(AddContact);
    public const string RemoveContact = nameof(RemoveContact);

    public const string SendMessage = nameof(SendMessage);
    public const string UpdateMessage = nameof(UpdateMessage);

    public const string RequestMessagePull = nameof(RequestMessagePull);
    public const string MessageUpstream = nameof(MessageUpstream);
    public const string MessageDownstream = nameof(MessageDownstream);

    public const string UpdateProfileImage = nameof(UpdateProfileImage);
}
