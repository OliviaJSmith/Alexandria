using Alexandria.Api.Repositories;

namespace Alexandria.Api.Services;

public class UsernameGenerator(IUserRepository users, Random random) : IUsernameGenerator
{
    public const int MaxAttempts = 10;

    private static readonly string[] Adjectives =
    [
        "Ancient", "Arcane", "Avid", "Bold", "Bookish", "Classic", "Clever", "Cozy", "Curious", "Devoted",
        "Dreamy", "Dusty", "Enchanted", "Epic", "Fabled", "Fond", "Gentle", "Golden", "Happy", "Hidden",
        "Inky", "Jovial", "Keen", "Kindred", "Legendary", "Literary", "Midnight", "Mystic", "Noble", "Nostalgic",
        "Obscure", "Poetic", "Quaint", "Quiet", "Rare", "Rustic", "Silent", "Storied", "Timeless", "Twilight",
        "Untold", "Velvet", "Vintage", "Wandering", "Whimsical", "Wise", "Yellowed", "Zealous",
    ];

    private static readonly string[] Nouns =
    [
        "Alcove", "Almanac", "Anthology", "Archive", "Atlas", "Athenaeum", "Ballad", "Bookmark", "Canon", "Chapter",
        "Chronicle", "Codex", "Compendium", "Den", "Edition", "Epilogue", "Fable", "Folio", "Footnote", "Grimoire",
        "Haven", "Index", "Journal", "Legend", "Lexicon", "Loft", "Manuscript", "Memoir", "Nook", "Novel",
        "Opus", "Page", "Paperback", "Prologue", "Prose", "Quill", "Reader", "Saga", "Sanctum", "Scribe",
        "Scroll", "Shelf", "Sonnet", "Spine", "Story", "Tale", "Tome", "Tower", "Vault", "Verse", "Volume",
    ];

    public async Task<string> GenerateUniqueAsync(CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < MaxAttempts; attempt++)
        {
            var candidate = $"{Pick(Adjectives)}{Pick(Nouns)}{random.Next(1, 1000)}";
            if (!await users.NormalizedUserNameExistsAsync(Normalize(candidate), cancellationToken))
            {
                return candidate;
            }
        }

        // Extremely unlikely to be reached: fall back to a random hex suffix (at least 10 hex digits survive the trim).
        return $"{Pick(Adjectives)}{Pick(Nouns)}{Guid.NewGuid():N}"[..30];
    }

    public static string Normalize(string userName) => userName.ToUpperInvariant();

    private string Pick(string[] words) => words[random.Next(words.Length)];
}
