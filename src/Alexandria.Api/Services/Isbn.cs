using System.Text;

namespace Alexandria.Api.Services;

/// <summary>ISBN validation and normalization. Alexandria stores every ISBN as ISBN-13 without hyphens.</summary>
public static class Isbn
{
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
