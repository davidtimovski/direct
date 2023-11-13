using System;
using System.Collections.Generic;
using System.Linq;

namespace Direct.Desktop.Utilities;

public static class ProfileImageUtil
{
    private static readonly Random Random = new();

    private static readonly IReadOnlyList<string> ProfileImages = new List<string>
    {
        "Avestea",
        "Bodonia",
        "Cionus",
        "Criothea",
        "Deon",
        "Dothea",
        "Earthneron",
        "Haluna",
        "Herex",
        "Innorth",
        "Lunnora",
        "Meorus",
        "Naiwei",
        "Nouria",
        "Orilia",
        "Pheron",
        "Sacitera",
        "Theter",
        "Thiavis",
        "Ubos",
        "Velmillon",
        "Vupra",
        "Xeitis",
        "Zolara"
    };

    static ProfileImageUtil()
    {
        Lookup = ProfileImages.ToDictionary(x => x, x => $"Assets/Images/ProfileImages/{x}.png");
    }

    public static readonly IReadOnlyDictionary<string, string> Lookup;

    /// <summary>
    /// Returns the path of the image or a random one if the profile image is null.
    /// </summary>
    public static string GetSource(string? profileImage)
    {
        profileImage ??= GetRandom();

        return Lookup[profileImage];
    }

    public static string GetRandom()
    {
        var randomIndex = Random.Next(0, ProfileImages.Count);
        return ProfileImages[randomIndex];
    }
}
