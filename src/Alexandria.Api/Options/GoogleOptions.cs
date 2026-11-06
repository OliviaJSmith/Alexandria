using System.ComponentModel.DataAnnotations;

namespace Alexandria.Api.Options;

public class GoogleOptions
{
    public const string SectionName = "Google";

    /// <summary>
    /// OAuth client IDs (web, Android, iOS) whose access tokens Alexandria accepts.
    /// A token issued to any other client is rejected.
    /// </summary>
    [MinLength(1)]
    public string[] ClientIds { get; set; } = [];
}
