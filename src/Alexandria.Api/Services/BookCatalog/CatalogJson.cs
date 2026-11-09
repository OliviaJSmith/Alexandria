using System.Text.Json;
using System.Text.RegularExpressions;

namespace Alexandria.Api.Services.BookCatalog;

/// <summary>Helpers for reading the loosely-shaped JSON returned by external book catalogs.</summary>
internal static partial class CatalogJson
{
    public static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()!.Trim()
            : null;

    public static int? GetInt(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
        && element.TryGetProperty(property, out var value)
        && value.ValueKind == JsonValueKind.Number
        && value.TryGetInt32(out var number)
        && number > 0
            ? number
            : null;

    /// <summary>String items of an array property, or of an array of objects using <paramref name="itemProperty"/>.</summary>
    public static IReadOnlyList<string> GetStrings(JsonElement element, string property, string? itemProperty = null)
    {
        if (element.ValueKind != JsonValueKind.Object
            || !element.TryGetProperty(property, out var array)
            || array.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return array.EnumerateArray()
            .Select(item => itemProperty is null
                ? (item.ValueKind == JsonValueKind.String ? item.GetString() : null)
                : GetString(item, itemProperty))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim())
            .ToList();
    }

    public static string? JoinOrNull(IReadOnlyList<string> values, int max = 3) =>
        values.Count == 0 ? null : string.Join(", ", values.Distinct().Take(max));

    /// <summary>First four-digit year in a free-form date such as "2004", "2004-05-01" or "May 2004".</summary>
    public static int? ParseYear(string? date)
    {
        if (date is null)
        {
            return null;
        }

        var match = YearPattern().Match(date);
        return match.Success ? int.Parse(match.Value) : null;
    }

    /// <summary>First value that is a valid ISBN, normalized to ISBN-13.</summary>
    public static string? FirstValidIsbn(IEnumerable<string> candidates)
    {
        foreach (var candidate in candidates)
        {
            if (Isbn.TryNormalize(candidate, out var isbn13))
            {
                return isbn13;
            }
        }

        return null;
    }

    /// <summary>Upgrades http image links to https so the app never loads mixed content.</summary>
    public static string? HttpsUrl(string? url) =>
        url is null ? null
        : url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ? "https://" + url["http://".Length..]
        : url;

    [GeneratedRegex(@"\b\d{4}\b")]
    private static partial Regex YearPattern();
}
