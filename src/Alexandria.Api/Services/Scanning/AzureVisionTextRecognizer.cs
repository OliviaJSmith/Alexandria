using System.Net.Http.Headers;
using System.Text.Json;
using Alexandria.Api.Options;
using Microsoft.Extensions.Options;

namespace Alexandria.Api.Services.Scanning;

public interface ITextRecognizer
{
    bool IsConfigured { get; }

    /// <summary>Reads the text in an image, one recognized line per line; null when recognition fails.</summary>
    Task<string?> ReadTextAsync(byte[] image, CancellationToken cancellationToken = default);
}

/// <summary>Azure Vision Image Analysis 4.0 "read" feature over REST.</summary>
public class AzureVisionTextRecognizer(
    HttpClient httpClient,
    IOptions<AzureVisionOptions> options,
    ILogger<AzureVisionTextRecognizer> logger) : ITextRecognizer
{
    public const string AnalyzePath = "computervision/imageanalysis:analyze?api-version=2024-02-01&features=read";
    public const string KeyHeader = "Ocp-Apim-Subscription-Key";

    public bool IsConfigured => options.Value.IsConfigured;

    public async Task<string?> ReadTextAsync(byte[] image, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(EnsureTrailingSlash(settings.Endpoint!)), AnalyzePath));
        request.Headers.Add(KeyHeader, settings.ApiKey);
        request.Content = new ByteArrayContent(image);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Azure Vision returned status {StatusCode}", (int)response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            return string.Join('\n', ReadLines(document.RootElement));
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException
            || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            logger.LogWarning(ex, "Azure Vision request failed");
            return null;
        }
    }

    private static IEnumerable<string> ReadLines(JsonElement root)
    {
        if (!root.TryGetProperty("readResult", out var readResult)
            || !readResult.TryGetProperty("blocks", out var blocks)
            || blocks.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var block in blocks.EnumerateArray())
        {
            if (!block.TryGetProperty("lines", out var lines) || lines.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var line in lines.EnumerateArray())
            {
                if (line.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(text.GetString()))
                {
                    yield return text.GetString()!.Trim();
                }
            }
        }
    }

    private static string EnsureTrailingSlash(string url) => url.EndsWith('/') ? url : url + "/";
}
