using Alexandria.Api.Services;

namespace Alexandria.Api.Tests.Services;

public class IsbnTests
{
    [Theory]
    [InlineData("9780547928227", "9780547928227")]
    [InlineData("978-0-547-92822-7", "9780547928227")]
    [InlineData(" 978 0547 928227 ", "9780547928227")]
    [InlineData("054792822X", "9780547928227")]
    [InlineData("0-8044-2957-X", "9780804429573")]
    [InlineData("080442957x", "9780804429573")]
    [InlineData("0306406152", "9780306406157")]
    public void TryNormalize_ValidIsbn_ReturnsIsbn13(string input, string expected)
    {
        Assert.True(Isbn.TryNormalize(input, out var isbn13));
        Assert.Equal(expected, isbn13);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("9780547928228")] // bad ISBN-13 check digit
    [InlineData("0547928221")] // bad ISBN-10 check digit
    [InlineData("12345")]
    [InlineData("X780547928227")]
    [InlineData("05479X8220")] // X only allowed as the ISBN-10 check digit
    public void TryNormalize_InvalidIsbn_ReturnsFalse(string? input)
    {
        Assert.False(Isbn.TryNormalize(input, out var isbn13));
        Assert.Equal(string.Empty, isbn13);
    }

    [Fact]
    public void ExtractFromText_FindsDistinctValidIsbnsInFreeText()
    {
        var text = "THE HOBBIT\nISBN 978-0-547-92822-7 $14.99\nalso 054792822X and 9780306406157\nbarcode 9780547928228 phone 555 123 4567";

        Assert.Equal(["9780547928227", "9780306406157"], Isbn.ExtractFromText(text));
    }

    [Fact]
    public void ExtractFromText_NoIsbn_ReturnsEmpty()
    {
        Assert.Empty(Isbn.ExtractFromText("Pride and Prejudice, Penguin Classics"));
    }

    [Fact]
    public void Clean_KeepsDigitsAndUppercasesX()
    {
        Assert.Equal("080442957X", Isbn.Clean("isbn: 0-8044-2957-x"));
    }

    [Fact]
    public void ConvertIsbn10ToIsbn13_ComputesCheckDigit()
    {
        Assert.Equal("9780306406157", Isbn.ConvertIsbn10ToIsbn13("0306406152"));
    }
}
