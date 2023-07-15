using System;
using System.Security.Cryptography;

namespace Direct.Utilities;

/// <summary>
/// Derived from: https://stackoverflow.com/a/73126492/1200185
/// </summary>
internal static class CryptographyUtil
{
    private const int _saltSize = 16; // 128 bits
    private const int _keySize = 32; // 256 bits
    private const int _iterations = 50000;
    private const char segmentDelimiter = ':';

    private static readonly HashAlgorithmName _algorithm = HashAlgorithmName.SHA256;

    internal static string Hash(string input)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(_saltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            input,
            salt,
            _iterations,
            _algorithm,
            _keySize
        );

        return string.Join(
            segmentDelimiter,
            Convert.ToHexString(hash),
            Convert.ToHexString(salt),
            _iterations,
            _algorithm
        );
    }
}
