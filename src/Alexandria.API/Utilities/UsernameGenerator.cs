namespace Alexandria.API.Utilities;

/// <summary>
/// Generates random book-themed usernames for new users.
/// </summary>
public static class UsernameGenerator
{
    private static readonly string[] Adjectives =
    [
        "Avid", "Clever", "Curious", "Dreamy", "Epic", "Fond", "Gentle", "Happy",
        "Keen", "Literary", "Midnight", "Noble", "Quiet", "Rare", "Silent",
        "Timeless", "Vintage", "Wandering", "Wise", "Zealous", "Ancient", "Bold",
        "Cozy", "Dusty", "Enchanted", "Fabled", "Golden", "Hidden", "Inky",
        "Jovial", "Kindred", "Legendary", "Mystic", "Nostalgic", "Obscure",
        "Poetic", "Quaint", "Rustic", "Storied", "Twilight", "Untold", "Velvet",
        "Whimsical", "Yellowed", "Arcane", "Bookish", "Classic", "Devoted"
    ];

    private static readonly string[] BookNouns =
    [
        "Atlas", "Bookmark", "Chapter", "Codex", "Edition", "Fable", "Folio",
        "Grimoire", "Index", "Journal", "Lexicon", "Manuscript", "Novel",
        "Opus", "Page", "Paperback", "Prose", "Quill", "Reader", "Saga",
        "Scribe", "Scroll", "Shelf", "Spine", "Story", "Tale", "Tome", "Verse",
        "Volume", "Word", "Almanac", "Anthology", "Archive", "Ballad", "Canon",
        "Chronicle", "Compendium", "Digest", "Epilogue", "Excerpt", "Footnote",
        "Gazette", "Hardcover", "Inscription", "Legend", "Memoir", "Narrative",
        "Ode", "Pamphlet", "Preface", "Prologue", "Sonnet", "Synopsis", "Treatise"
    ];

    private static readonly string[] LibraryNouns =
    [
        "Alcove", "Annex", "Attic", "Binding", "Corner", "Desk", "Nook",
        "Parlor", "Stack", "Study", "Wing", "Aisle", "Loft", "Gallery",
        "Haven", "Hideaway", "Keep", "Lair", "Nest", "Retreat", "Sanctum",
        "Tower", "Vault", "Den", "Chamber", "Hall", "Library", "Athenaeum"
    ];

    private static readonly Random _random = new();

    /// <summary>
    /// Generates a random book-themed username.
    /// </summary>
    /// <returns>A username like "CuriousScribe42" or "MidnightTome7"</returns>
    public static string Generate()
    {
        var adjective = Adjectives[_random.Next(Adjectives.Length)];
        
        // Randomly choose between book nouns and library nouns
        var nouns = _random.Next(2) == 0 ? BookNouns : LibraryNouns;
        var noun = nouns[_random.Next(nouns.Length)];
        
        // Add a random number suffix (1-999) to help ensure uniqueness
        var number = _random.Next(1, 1000);
        
        return $"{adjective}{noun}{number}";
    }

    /// <summary>
    /// Generates a unique username by checking against existing usernames.
    /// </summary>
    /// <param name="isUserNameTaken">Function to check if a username is already taken.</param>
    /// <param name="maxAttempts">Maximum number of attempts to find a unique username.</param>
    /// <returns>A unique username.</returns>
    public static async Task<string> GenerateUniqueAsync(
        Func<string, Task<bool>> isUserNameTaken,
        int maxAttempts = 10)
    {
        for (var i = 0; i < maxAttempts; i++)
        {
            var username = Generate();
            if (!await isUserNameTaken(username))
            {
                return username;
            }
        }

        // If we couldn't find a unique one, add more randomness
        return $"{Generate()}{_random.Next(1000, 10000)}";
    }
}
