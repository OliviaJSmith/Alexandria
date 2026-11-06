namespace Alexandria.Api.Services;

public interface IUsernameGenerator
{
    /// <summary>Generates a book-themed username (e.g. "CuriousScribe42") not used by any other account.</summary>
    Task<string> GenerateUniqueAsync(CancellationToken cancellationToken = default);
}
