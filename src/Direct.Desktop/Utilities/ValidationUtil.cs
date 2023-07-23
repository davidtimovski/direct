using System;
using System.Text.RegularExpressions;

namespace Direct.Desktop.Utilities;

public static partial class ValidationUtil
{
    private const int NicknameMinLength = 2;
    private const int NicknameMaxLength = 25;

    public static bool UserIdIsValid(string userId)
    {
        return userId.Trim().Length == 36 && Guid.TryParse(userId, out Guid _);
    }

    public static bool NicknameIsValid(string nickname)
    {
        if (!IsAlphanumeric(nickname))
        {
            return false;
        }

        if (nickname.Trim().Length < NicknameMinLength || nickname.Trim().Length > NicknameMaxLength)
        {
            return false;
        }

        return true;
    }

    private static bool IsAlphanumeric(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        text = text.Trim();

        return NicknameRegex().IsMatch(text);
    }

    [GeneratedRegex("^[a-zA-Z0-9\\s]*$")]
    private static partial Regex NicknameRegex();
}
