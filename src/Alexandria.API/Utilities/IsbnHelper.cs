using System.Text.RegularExpressions;

namespace Alexandria.API.Utilities;

/// <summary>
/// Helper class for ISBN validation and normalization.
/// All ISBNs are normalized to ISBN-13 format for consistency.
/// </summary>
public static partial class IsbnHelper
{
    [GeneratedRegex(@"[\s\-]")]
    private static partial Regex WhitespaceAndDashRegex();

    [GeneratedRegex(@"^\d{9}[\dXx]$")]
    private static partial Regex Isbn10Regex();

    [GeneratedRegex(@"^\d{13}$")]
    private static partial Regex Isbn13Regex();

    /// <summary>
    /// Cleans an ISBN by removing spaces and dashes.
    /// </summary>
    public static string Clean(string isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
            return string.Empty;

        return WhitespaceAndDashRegex().Replace(isbn.Trim(), "");
    }

    /// <summary>
    /// Validates if a string is a valid ISBN-10.
    /// </summary>
    public static bool IsValidIsbn10(string isbn)
    {
        var cleaned = Clean(isbn);
        if (!Isbn10Regex().IsMatch(cleaned))
            return false;

        return CalculateIsbn10CheckDigit(cleaned[..9]) == char.ToUpper(cleaned[9]);
    }

    /// <summary>
    /// Validates if a string is a valid ISBN-13.
    /// </summary>
    public static bool IsValidIsbn13(string isbn)
    {
        var cleaned = Clean(isbn);
        if (!Isbn13Regex().IsMatch(cleaned))
            return false;

        return CalculateIsbn13CheckDigit(cleaned[..12]) == cleaned[12];
    }

    /// <summary>
    /// Validates if a string is any valid ISBN format.
    /// </summary>
    public static bool IsValid(string isbn)
    {
        return IsValidIsbn10(isbn) || IsValidIsbn13(isbn);
    }

    /// <summary>
    /// Normalizes any valid ISBN to ISBN-13 format.
    /// Returns null if the input is not a valid ISBN.
    /// </summary>
    public static string? NormalizeToIsbn13(string isbn)
    {
        var cleaned = Clean(isbn);

        if (IsValidIsbn13(cleaned))
            return cleaned;

        if (IsValidIsbn10(cleaned))
            return ConvertIsbn10ToIsbn13(cleaned);

        return null;
    }

    /// <summary>
    /// Converts a valid ISBN-10 to ISBN-13.
    /// </summary>
    public static string ConvertIsbn10ToIsbn13(string isbn10)
    {
        var cleaned = Clean(isbn10);
        if (cleaned.Length != 10)
            throw new ArgumentException("ISBN-10 must be exactly 10 characters", nameof(isbn10));

        // ISBN-13 = "978" + first 9 digits of ISBN-10 + new check digit
        var isbn13Base = "978" + cleaned[..9];
        var checkDigit = CalculateIsbn13CheckDigit(isbn13Base);

        return isbn13Base + checkDigit;
    }

    /// <summary>
    /// Calculates the check digit for ISBN-10.
    /// </summary>
    private static char CalculateIsbn10CheckDigit(string first9Digits)
    {
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            sum += (10 - i) * (first9Digits[i] - '0');
        }

        var checkValue = (11 - (sum % 11)) % 11;
        return checkValue == 10 ? 'X' : (char)('0' + checkValue);
    }

    /// <summary>
    /// Calculates the check digit for ISBN-13.
    /// </summary>
    private static char CalculateIsbn13CheckDigit(string first12Digits)
    {
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            var digit = first12Digits[i] - '0';
            sum += (i % 2 == 0) ? digit : digit * 3;
        }

        var checkValue = (10 - (sum % 10)) % 10;
        return (char)('0' + checkValue);
    }

    /// <summary>
    /// Attempts to extract ISBNs from text (useful for OCR results).
    /// Returns all found ISBNs normalized to ISBN-13 format.
    /// </summary>
    public static IEnumerable<string> ExtractIsbnsFromText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            yield break;

        // Pattern to find potential ISBN-like sequences
        var potentialIsbns = Regex.Matches(text, @"\b(?:\d[\d\-\s]{8,16}\d[Xx]?)\b");

        foreach (Match match in potentialIsbns)
        {
            var potential = match.Value;
            var normalized = NormalizeToIsbn13(potential);
            if (normalized is not null)
                yield return normalized;
        }
    }
}
