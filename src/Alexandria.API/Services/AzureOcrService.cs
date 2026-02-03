using Alexandria.API.DTOs;
using Alexandria.API.Utilities;
using Azure;
using Azure.AI.OpenAI;
using Azure.AI.Vision.ImageAnalysis;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text;
using System.Text.RegularExpressions;

namespace Alexandria.API.Services;

/// <summary>
/// OCR service using Azure Computer Vision for text and barcode extraction.
/// </summary>
public partial class AzureOcrService(
    IOptions<AzureOpenAiOptions> options,
    ILogger<AzureOcrService> logger
) : IOcrService
{
    private readonly AzureOpenAiOptions _options = options.Value;

    [GeneratedRegex(@"(?:ISBN[:\-\s]*)?(\d[\d\-\s]{8,16}[\dXx])", RegexOptions.IgnoreCase)]
    private static partial Regex IsbnPatternRegex();

    public async Task<OcrExtractionResult> ExtractSingleBookAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default
    )
    {
        if (!_options.IsConfigured)
        {
            logger.LogWarning("Azure Computer Vision is not configured. Returning empty result.");
            return new OcrExtractionResult { Confidence = 0 };
        }

        try
        {
            var client = CreateClient();
            var imageData = await ReadStreamToBinaryDataAsync(imageStream, cancellationToken);

            // For single books, use both Read (for text) and try to detect barcodes
            var analysisResult = await client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read,
                cancellationToken: cancellationToken
            );

            var extractedText = ExtractAllText(analysisResult.Value);
            var isbns = ExtractIsbns(extractedText);
            var titles = ExtractPotentialTitles(extractedText, isSingleBook: true);
            var confidence = CalculateConfidence(analysisResult.Value);

            logger.LogInformation(
                "Single book scan extracted {IsbnCount} ISBNs, {TitleCount} potential titles from {FileName}",
                isbns.Count,
                titles.Count,
                fileName
            );

            var result = new OcrExtractionResult
            {
                DetectedIsbns = isbns,
                DetectedTitles = titles,
                RawText = extractedText,
                Confidence = confidence,
            };

            // Enhance with AI if deployment name is configured
            if (_options.IsAiEnhancementEnabled)
            {
                result = await EnhanceWithAiAsync(result, isSingleBook: true, cancellationToken);
            }

            return result;
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

    public async Task<OcrExtractionResult> ExtractBookshelfAsync(
        Stream imageStream,
        string fileName,
        CancellationToken cancellationToken = default
    )
    {
        if (!_options.IsConfigured)
        {
            logger.LogWarning("Azure Computer Vision is not configured. Returning empty result.");
            return new OcrExtractionResult { Confidence = 0 };
        }

        try
        {
            var client = CreateClient();
            var imageData = await ReadStreamToBinaryDataAsync(imageStream, cancellationToken);

            // For bookshelves, use Read API for dense text extraction
            var analysisResult = await client.AnalyzeAsync(
                imageData,
                VisualFeatures.Read,
                cancellationToken: cancellationToken
            );

            var extractedText = ExtractAllText(analysisResult.Value);
            var isbns = ExtractIsbns(extractedText);
            var titles = ExtractPotentialTitles(extractedText, isSingleBook: false);
            var confidence = CalculateConfidence(analysisResult.Value);

            logger.LogInformation(
                "Bookshelf scan extracted {IsbnCount} ISBNs, {TitleCount} potential titles from {FileName}",
                isbns.Count,
                titles.Count,
                fileName
            );

            var result = new OcrExtractionResult
            {
                DetectedIsbns = isbns,
                DetectedTitles = titles,
                RawText = extractedText,
                Confidence = confidence,
            };

            // Enhance with AI if deployment name is configured
            if (_options.IsAiEnhancementEnabled)
            {
                result = await EnhanceWithAiAsync(result, isSingleBook: false, cancellationToken);
            }

            return result;
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
            new Uri(_options.Endpoint),
            new AzureKeyCredential(_options.ApiKey)
        );
    }

    private static async Task<BinaryData> ReadStreamToBinaryDataAsync(
        Stream stream,
        CancellationToken cancellationToken
    )
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
            .Where(l => l.Length >= 2 && l.Length <= 200) // Reasonable text length
            .Where(l => !IsLikelyNonTitle(l))
            .ToList();

        if (isSingleBook)
        {
            // For single book covers, text is often split across multiple lines
            // Strategy: combine lines to form potential title+author combinations

            // First, add combinations of adjacent lines (common for multi-line titles)
            for (int i = 0; i < lines.Count; i++)
            {
                // Single line as candidate
                var singleLine = lines[i];
                if (ScoreTitleCandidate(singleLine) > 0.3)
                {
                    titles.Add(singleLine);
                }

                // Combine 2 adjacent lines (e.g., "The" + "Hobbit" = "The Hobbit")
                if (i + 1 < lines.Count)
                {
                    var twoLines = $"{lines[i]} {lines[i + 1]}";
                    if (ScoreTitleCandidate(twoLines) > 0.3)
                    {
                        titles.Add(twoLines);
                    }
                }

                // Combine 3 adjacent lines (for longer titles)
                if (i + 2 < lines.Count)
                {
                    var threeLines = $"{lines[i]} {lines[i + 1]} {lines[i + 2]}";
                    if (ScoreTitleCandidate(threeLines) > 0.3)
                    {
                        titles.Add(threeLines);
                    }
                }
            }

            // Also add the full combined text as a search candidate (useful for covers)
            var fullText = string.Join(" ", lines);
            if (fullText.Length >= 3 && fullText.Length <= 200)
            {
                titles.Add(fullText);
            }

            // Sort by score and take the best candidates
            titles = titles
                .Distinct()
                .OrderByDescending(t => ScoreTitleCandidate(t))
                .Take(5)
                .ToList();
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
            "isbn",
            "barcode",
            "price",
            "copyright",
            "published",
            "printed",
            "all rights reserved",
            "edition",
            "www.",
            "http",
            ".com",
            ".org",
            "chapter",
            "page",
            "index",
            "contents",
            "acknowledgments",
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
        var confidences = result
            .Read.Blocks.SelectMany(b => b.Lines)
            .SelectMany(l => l.Words)
            .Select(w => w.Confidence)
            .ToList();

        if (confidences.Count == 0)
            return 0;

        return confidences.Average();
    }

    private AzureOpenAIClient CreateOpenAIClient()
    {
        return new AzureOpenAIClient(
            new Uri(_options.Endpoint),
            new AzureKeyCredential(_options.ApiKey)
        );
    }

    private async Task<OcrExtractionResult> EnhanceWithAiAsync(
        OcrExtractionResult ocrResult,
        bool isSingleBook,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var client = CreateOpenAIClient();
            var chatClient = client.GetChatClient(_options.DeploymentName);

            var systemPrompt = isSingleBook
                ? """
                You are an expert at analyzing OCR text from book covers. 
                Your task is to extract and refine book information from raw OCR text.
                Focus on identifying:
                1. ISBN numbers (10 or 13 digits, may include hyphens)
                2. Book title (main title, typically the largest text)
                3. Author name

                Return a JSON object with:
                - "isbns": array of ISBN strings (normalized to ISBN-13 format)
                - "titles": array of possible book titles (best candidate first)

                Be conservative - only return high-confidence matches.
                """
                : """
                You are an expert at analyzing OCR text from bookshelf images.
                Your task is to extract book titles and ISBNs from spines of multiple books.

                Return a JSON object with:
                - "isbns": array of ISBN strings found (normalized to ISBN-13 format)
                - "titles": array of book titles visible on spines

                Focus on text that appears to be book titles, not publisher logos or decorative text.
                """;

            var userPrompt =
                $"""
                Raw OCR Text:
                {ocrResult.RawText}

                Previously detected ISBNs: {string.Join(", ", ocrResult.DetectedIsbns)}
                Previously detected titles: {string.Join(", ", ocrResult.DetectedTitles)}

                Please analyze this text and extract refined book information.
                Return only valid JSON.
                """;

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userPrompt),
            };

            var response = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);

            var content = response.Value.Content[0].Text;

            logger.LogInformation("AI enhancement response: {Response}", content);

            // Parse the AI response and merge with existing results
            var enhancedResult = ParseAiResponse(content, ocrResult);

            return enhancedResult;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Failed to enhance OCR results with AI, returning original results"
            );
            return ocrResult;
        }
    }

    private OcrExtractionResult ParseAiResponse(string aiResponse, OcrExtractionResult originalResult)
    {
        try
        {
            // Try to parse JSON response
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = System.Text.Json.JsonDocument.Parse(jsonContent);
                var root = parsed.RootElement;

                // Start with AI-enhanced results (prioritized)
                var enhancedIsbns = new List<string>();
                var enhancedTitles = new List<string>();

                // Extract ISBNs from AI response first (highest priority)
                if (root.TryGetProperty("isbns", out var isbnsElement) && isbnsElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var isbn in isbnsElement.EnumerateArray())
                    {
                        var isbnValue = isbn.GetString();
                        if (!string.IsNullOrEmpty(isbnValue))
                        {
                            enhancedIsbns.Add(isbnValue);
                        }
                    }
                }

                // Add original ISBNs that aren't already in AI results
                foreach (var originalIsbn in originalResult.DetectedIsbns)
                {
                    if (!enhancedIsbns.Contains(originalIsbn))
                    {
                        enhancedIsbns.Add(originalIsbn);
                    }
                }

                // Extract titles from AI response first (highest priority)
                if (root.TryGetProperty("titles", out var titlesElement) && titlesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var title in titlesElement.EnumerateArray())
                    {
                        var titleValue = title.GetString();
                        if (!string.IsNullOrEmpty(titleValue))
                        {
                            enhancedTitles.Add(titleValue);
                        }
                    }
                }

                // Add original titles that aren't already in AI results
                foreach (var originalTitle in originalResult.DetectedTitles)
                {
                    if (!enhancedTitles.Contains(originalTitle, StringComparer.OrdinalIgnoreCase))
                    {
                        enhancedTitles.Add(originalTitle);
                    }
                }

                return new OcrExtractionResult
                {
                    DetectedIsbns = enhancedIsbns.Distinct().ToList(),
                    DetectedTitles = enhancedTitles.Distinct().ToList(),
                    RawText = originalResult.RawText,
                    Confidence = originalResult.Confidence,
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to parse AI response, using original results");
        }

        return originalResult;
    }
}
