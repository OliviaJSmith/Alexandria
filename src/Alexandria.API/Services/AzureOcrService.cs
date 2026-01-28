using System.Text;
using System.Text.RegularExpressions;
using Alexandria.API.DTOs;
using Alexandria.API.Utilities;
using Azure;
using Azure.AI.Vision.ImageAnalysis;

namespace Alexandria.API.Services;

/// <summary>
/// OCR service using Azure Computer Vision for text and barcode extraction.
/// </summary>
public partial class AzureOcrService(
    IConfiguration configuration,
    ILogger<AzureOcrService> logger) : IOcrService
{
    private readonly string _endpoint = configuration["AzureComputerVision:Endpoint"] ?? "";
    private readonly string _apiKey = configuration["AzureComputerVision:ApiKey"] ?? "";

    [GeneratedRegex(@"(?:ISBN[:\-\s]*)?(\d[\d\-\s]{8,16}[\dXx])", RegexOptions.IgnoreCase)]
    private static partial Regex IsbnPatternRegex();

    public async Task<OcrExtractionResult> ExtractSingleBookAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_apiKey))
        {
            logger.LogWarning("Azure Computer Vision is not configured. Returning empty result.");
            return new OcrExtractionResult { Confidence = 0 };
        }

        try
        {
            var client = CreateClient();
            var imageData = await ReadStreamToBinaryDataAsync(imageStream, cancellationToken);

            // For single books, use both Read (for text) and try to detect barcodes
            var result = await client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read,
                cancellationToken: cancellationToken);

            var extractedText = ExtractAllText(result.Value);
            var isbns = ExtractIsbns(extractedText);
            var titles = ExtractPotentialTitles(extractedText, isSingleBook: true);
            var confidence = CalculateConfidence(result.Value);

            logger.LogInformation(
                "Single book scan extracted {IsbnCount} ISBNs, {TitleCount} potential titles from {FileName}",
                isbns.Count, titles.Count, fileName);

            return new OcrExtractionResult
            {
                DetectedIsbns = isbns,
                DetectedTitles = titles,
                RawText = extractedText,
                Confidence = confidence
            };
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Azure Computer Vision request failed for {FileName}", fileName);
            return new OcrExtractionResult { Confidence = 0 };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing image {FileName}", fileName);
            return new OcrExtractionResult { Confidence = 0 };
        }
    }

    public async Task<OcrExtractionResult> ExtractBookshelfAsync(Stream imageStream, string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_endpoint) || string.IsNullOrEmpty(_apiKey))
        {
            logger.LogWarning("Azure Computer Vision is not configured. Returning empty result.");
            return new OcrExtractionResult { Confidence = 0 };
        }

        try
        {
            var client = CreateClient();
            var imageData = await ReadStreamToBinaryDataAsync(imageStream, cancellationToken);

            // For bookshelves, use Read API for dense text extraction
            var result = await client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read,
                cancellationToken: cancellationToken);

            var extractedText = ExtractAllText(result.Value);
            var isbns = ExtractIsbns(extractedText);
            var titles = ExtractPotentialTitles(extractedText, isSingleBook: false);
            var confidence = CalculateConfidence(result.Value);

            logger.LogInformation(
                "Bookshelf scan extracted {IsbnCount} ISBNs, {TitleCount} potential titles from {FileName}",
                isbns.Count, titles.Count, fileName);

            return new OcrExtractionResult
            {
                DetectedIsbns = isbns,
                DetectedTitles = titles,
                RawText = extractedText,
                Confidence = confidence
            };
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex, "Azure Computer Vision request failed for {FileName}", fileName);
            return new OcrExtractionResult { Confidence = 0 };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing bookshelf image {FileName}", fileName);
            return new OcrExtractionResult { Confidence = 0 };
        }
    }

    private ImageAnalysisClient CreateClient()
    {
        return new ImageAnalysisClient(
            new Uri(_endpoint),
            new AzureKeyCredential(_apiKey));
    }

    private static async Task<BinaryData> ReadStreamToBinaryDataAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);
        return BinaryData.FromBytes(memoryStream.ToArray());
    }

    private static string ExtractAllText(ImageAnalysisResult result)
    {
        if (result.Read?.Blocks is null)
            return string.Empty;

        var textBuilder = new StringBuilder();
        foreach (var block in result.Read.Blocks)
        {
            foreach (var line in block.Lines)
            {
                textBuilder.AppendLine(line.Text);
            }
        }

        return textBuilder.ToString();
    }

    private List<string> ExtractIsbns(string text)
    {
        var isbns = new List<string>();

        // First, try to find explicit ISBN patterns
        var matches = IsbnPatternRegex().Matches(text);
        foreach (Match match in matches)
        {
            var potential = match.Groups[1].Value;
            var normalized = IsbnHelper.NormalizeToIsbn13(potential);
            if (normalized is not null && !isbns.Contains(normalized))
            {
                isbns.Add(normalized);
            }
        }

        // Also try to extract from raw text using the helper
        var extracted = IsbnHelper.ExtractIsbnsFromText(text);
        foreach (var isbn in extracted)
        {
            if (!isbns.Contains(isbn))
                isbns.Add(isbn);
        }

        return isbns;
    }

    private static List<string> ExtractPotentialTitles(string text, bool isSingleBook)
    {
        var titles = new List<string>();
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .Where(l => l.Length >= 3 && l.Length <= 200) // Reasonable title length
            .Where(l => !IsLikelyNonTitle(l))
            .ToList();

        if (isSingleBook)
        {
            // For single book, prioritize longer, more prominent text
            // Often the title is one of the first lines or a prominent long line
            var candidates = lines
                .OrderByDescending(l => ScoreTitleCandidate(l))
                .Take(3)
                .ToList();

            titles.AddRange(candidates);
        }
        else
        {
            // For bookshelf, look for patterns that suggest book spines
            // Book spines often have Title on one line, Author on another
            foreach (var line in lines)
            {
                var score = ScoreTitleCandidate(line);
                if (score > 0.3)
                {
                    titles.Add(line);
                }
            }

            // Limit results for bookshelf to avoid too many false positives
            titles = titles.Take(50).ToList();
        }

        return titles.Distinct().ToList();
    }

    private static bool IsLikelyNonTitle(string text)
    {
        var lower = text.ToLowerInvariant();

        // Common non-title patterns
        var nonTitlePatterns = new[]
        {
            "isbn", "barcode", "price", "copyright", "published", "printed",
            "all rights reserved", "edition", "www.", "http", ".com", ".org",
            "chapter", "page", "index", "contents", "acknowledgments"
        };

        if (nonTitlePatterns.Any(p => lower.Contains(p)))
            return true;

        // Pure numbers (likely prices, page numbers, etc.)
        if (Regex.IsMatch(text, @"^[\d\.\$\€\£,\s]+$"))
            return true;

        // Very short all-caps codes
        if (text.Length <= 5 && text == text.ToUpperInvariant() && !text.Any(char.IsLower))
            return true;

        return false;
    }

    private static double ScoreTitleCandidate(string text)
    {
        var score = 0.5; // Base score

        // Longer text (within reason) is more likely a title
        if (text.Length >= 10 && text.Length <= 100)
            score += 0.2;

        // Title case or mixed case is common for titles
        if (char.IsUpper(text[0]) && text.Any(char.IsLower))
            score += 0.1;

        // Contains letters (not just numbers/symbols)
        if (text.Any(char.IsLetter))
            score += 0.1;

        // Penalize text that's all uppercase (often headers, not titles)
        if (text == text.ToUpperInvariant() && text.Length > 10)
            score -= 0.1;

        // Penalize if contains too many numbers
        var digitRatio = (double)text.Count(char.IsDigit) / text.Length;
        if (digitRatio > 0.3)
            score -= 0.2;

        return Math.Max(0, Math.Min(1, score));
    }

    private static double CalculateConfidence(ImageAnalysisResult result)
    {
        if (result.Read?.Blocks is null || result.Read.Blocks.Count == 0)
            return 0;

        // Calculate average confidence from word confidences
        var confidences = result.Read.Blocks
            .SelectMany(b => b.Lines)
            .SelectMany(l => l.Words)
            .Select(w => w.Confidence)
            .ToList();

        if (confidences.Count == 0)
            return 0;

        return confidences.Average();
    }
}
