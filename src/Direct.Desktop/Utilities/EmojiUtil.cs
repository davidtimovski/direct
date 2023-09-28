using System;
using System.Text;

namespace Direct.Desktop.Utilities;

internal static class EmojiUtil
{
    private static readonly Replacement[] replacements = new Replacement[]
    {
        new Replacement(":)", "🙂"),
        new Replacement(":(", "🙁"),
        new Replacement(";)", "😉"),
        new Replacement(":D", "😀"),
        new Replacement("XD", "😆"),
        new Replacement(":P", "😋"),
        new Replacement(";P", "😜"),
        new Replacement(":o", "😯"),
        new Replacement(":O", "😯"),
        new Replacement("8)", "😎"),
        new Replacement(":*", "😗"),
        new Replacement(";*", "😘"),
        new Replacement("<3", "❤️")
    };

    internal static string GenerateEmojis(string text)
    {
        var span = text.AsSpan();
        var builder = new StringBuilder();

        for (var i = 0; i < span.Length; i++)
        {
            var replaced = false;

            if (i < span.Length - 1)
            {
                foreach (var replacement in replacements)
                {
                    if (text[i].Equals(replacement.Match[0]) && text[i + 1].Equals(replacement.Match[1]))
                    {
                        builder.Append(replacement.Emoji);
                        i++;
                        replaced = true;
                        break;
                    }
                }
            }

            if (!replaced)
            {
                builder.Append(text[i]);
            }
        }

        return builder.ToString();
    }
}

internal readonly record struct Replacement(string Match, string Emoji);
