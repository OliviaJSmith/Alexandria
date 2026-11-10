namespace Alexandria.Api.Options;

/// <summary>Scheduled jobs (called by a GitHub Actions cron). Off when no key is configured.</summary>
public class JobsOptions
{
    public const string SectionName = "Jobs";

    /// <summary>Header the scheduler sends the key in.</summary>
    public const string KeyHeader = "X-Job-Key";

    /// <summary>Keys shorter than this are treated as not configured.</summary>
    public const int MinimumKeyLength = 32;

    /// <summary>Shared secret the scheduler sends. Key Vault in Azure, user-secrets locally; never committed.</summary>
    public string? Key { get; set; }

    public bool IsConfigured => Key is { Length: >= MinimumKeyLength };
}
