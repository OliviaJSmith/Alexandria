namespace Alexandria.Api.Dtos;

/// <summary>
/// A book detected in a photo. <see cref="Match"/> is the best catalog match (its id is null when it comes
/// from Open Library / Google Books and isn't in Alexandria yet), or null when nothing matched.
/// </summary>
public sealed record ScannedBookDto(string DetectedTitle, string? DetectedAuthor, string? DetectedIsbn, BookDto? Match);

public sealed record ScanResultDto(IReadOnlyList<ScannedBookDto> Books, string? Message);
