namespace Alexandria.Api.Services.Scanning;

/// <summary>Checks an upload's leading bytes, so the file type isn't taken on the client's word.</summary>
public static class ImageFormat
{
    public static bool IsSupported(ReadOnlySpan<byte> data) =>
        IsJpeg(data) || IsPng(data) || IsGif(data) || IsBmp(data) || IsWebp(data);

    private static bool IsJpeg(ReadOnlySpan<byte> d) => d.Length >= 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF;

    private static bool IsPng(ReadOnlySpan<byte> d) => d.StartsWith((ReadOnlySpan<byte>)[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);

    private static bool IsGif(ReadOnlySpan<byte> d) => d.StartsWith("GIF8"u8);

    private static bool IsBmp(ReadOnlySpan<byte> d) => d.StartsWith("BM"u8);

    private static bool IsWebp(ReadOnlySpan<byte> d) => d.Length >= 12 && d.StartsWith("RIFF"u8) && d[8..12].SequenceEqual("WEBP"u8);
}
