namespace Alexandria.Api.Options;

public class GoogleBooksOptions
{
    public const string SectionName = "GoogleBooks";

    /// <summary>Optional Google Books API key; without one, requests use Google's low anonymous quota.</summary>
    public string? ApiKey { get; set; }
}
