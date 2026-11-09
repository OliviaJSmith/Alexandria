using System.Text;
using System.Text.RegularExpressions;

namespace Alexandria.Api.Services;

/// <summary>ISBN validation and normalization. Alexandria stores every ISBN as ISBN-13 without hyphens.</summary>
public static partial class Isbn
{
    /// <summary>Valid ISBNs found anywhere in free text (e.g. text read from a book cover), as distinct ISBN-13s.</summary>
    public static IReadOnlyList<string> ExtractFromText(string text)
    {
        var found = new List<string>();
        foreach (Match match in IsbnLikePattern().Matches(text))
        {
            if (TryNormalize(match.Value, out var isbn13) && !found.Contains(isbn13))
            {
                found.Add(isbn13);
            }
        }

        return found;
    }

    // 10 or 13 digits (last may be X for ISBN-10), optionally separated by single hyphens or spaces.
    [GeneratedRegex(@"(?<![\dXx])(?:\d[-\s]?){9,12}[\dXx](?![\dXx])")]
    private static partial Regex IsbnLikePattern();

    /// <summary>Normalizes an ISBN-10 or ISBN-13 (hyphens and spaces allowed) to ISBN-13; false if it is not a valid ISBN.</summary>
    public static bool TryNormalize(string? input, out string isbn13)
    {
        isbn13 = string.Empty;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        var cleaned = Clean(input);
        if (IsValidIsbn13(cleaned))
        {
            isbn13 = cleaned;
            return true;
        }

        if (IsValidIsbn10(cleaned))
        {
            isbn13 = ConvertIsbn10ToIsbn13(cleaned);
            return true;
        }

        return false;
    }

    /// <summary>Removes everything except digits and a trailing X.</summary>
    public static string Clean(string input)
    {
        var builder = new StringBuilder(input.Length);
        foreach (var c in input)
        {
            if (char.IsAsciiDigit(c))
            {
                builder.Append(c);
            }
            else if (c is 'x' or 'X')
            {
                builder.Append('X');
            }
        }

        return builder.ToString();
    }

    public static bool IsValidIsbn10(string isbn)
    {
        if (isbn.Length != 10)
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 10; i++)
        {
            int value;
            if (char.IsAsciiDigit(isbn[i]))
            {
                value = isbn[i] - '0';
            }
            else if (i == 9 && isbn[i] == 'X')
            {
                value = 10;
            }
            else
            {
                return false;
            }

            sum += value * (10 - i);
        }

        return sum % 11 == 0;
    }

    public static bool IsValidIsbn13(string isbn)
    {
        if (isbn.Length != 13 || !isbn.All(char.IsAsciiDigit))
        {
            return false;
        }

        var sum = 0;
        for (var i = 0; i < 13; i++)
        {
            sum += (isbn[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        return sum % 10 == 0;
    }

    /// <summary>Converts a valid ISBN-10 to its ISBN-13 (978 prefix) form.</summary>
    public static string ConvertIsbn10ToIsbn13(string isbn10)
    {
        var core = "978" + isbn10[..9];
        var sum = 0;
        for (var i = 0; i < 12; i++)
        {
            sum += (core[i] - '0') * (i % 2 == 0 ? 1 : 3);
        }

        var check = (10 - (sum % 10)) % 10;
        return core + check;
    }
}
