﻿using System.Text.RegularExpressions;

namespace Direct.Shared;

public static partial class Validation
{
    private const int NicknameMinLength = 2;
    private const int NicknameMaxLength = 25;
    private const int PasswordHashMinLength = 20;
    private const int PasswordHashMaxLength = 100;

    public static string? ValidateNickname(string nickname)
    {
        if (!IsAlphanumeric(nickname))
        {
            return "Nicknames can contain only letters, numbers, and spaces.";
        }

        if (nickname.Trim().Length < NicknameMinLength || nickname.Trim().Length > NicknameMaxLength)
        {
            return $"Nicknames can be from {NicknameMinLength} to {NicknameMaxLength} characters long.";
        }

        return null;
    }

    public static bool PasswordHashIsValid(string passwordHash)
    {
        if (passwordHash.Trim().Length < PasswordHashMinLength || passwordHash.Trim().Length > PasswordHashMaxLength)
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
