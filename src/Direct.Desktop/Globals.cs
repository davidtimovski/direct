using System.Globalization;

namespace Direct.Desktop;

public static class Globals
{
#if DEBUG
    public const string ServerUri = "http://localhost:5250";
#else
    public const string ServerUri = "https://direct.davidtimovski.com";
#endif

    public static CultureInfo Culture = new("en-US");
}
