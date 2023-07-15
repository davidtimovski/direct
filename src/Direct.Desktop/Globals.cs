namespace Direct;

public static class Globals
{
#if DEBUG
    public const string ServerUri = "http://localhost:5250";
#else
    public const string ServerUri = "https://direct.davidtimovski.com";
#endif
}
