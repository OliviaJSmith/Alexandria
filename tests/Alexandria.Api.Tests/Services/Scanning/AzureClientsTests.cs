using System.Net;
using System.Text.Json;
using Alexandria.Api.Options;
using Alexandria.Api.Services.Scanning;
using Microsoft.Extensions.Logging.Abstractions;

namespace Alexandria.Api.Tests.Services.Scanning;

public class AzureVisionTextRecognizerTests
{
    private readonly FakeHttp _http = new();

    [Fact]
    public async Task ReadText_JoinsLinesFromAllBlocks_AndSendsKeyHeaderAndBytes()
    {
        _http.Json("""
        {"readResult":{"blocks":[
          {"lines":[{"text":"THE HOBBIT"},{"text":"  "},{"text":"J.R.R. Tolkien"}]},
          {"lines":[{"text":"ISBN 978-0-547-92822-7"}]},
          {"noLines":true}
        ]}}
        """);

        var text = await Recognizer().ReadTextAsync([1, 2, 3]);

        Assert.Equal("THE HOBBIT\nJ.R.R. Tolkien\nISBN 978-0-547-92822-7", text);
        var request = _http.Requests[0];
        Assert.Equal($"https://vision.example/{AzureVisionTextRecognizer.AnalyzePath}", request.RequestUri!.ToString());
        Assert.Equal("vision-key", Assert.Single(request.Headers.GetValues(AzureVisionTextRecognizer.KeyHeader)));
        Assert.Equal("application/octet-stream", request.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task ReadText_NoReadResult_ReturnsEmptyString()
    {
        _http.Json("""{"modelVersion":"2023-10-01"}""");

        Assert.Equal(string.Empty, await Recognizer().ReadTextAsync([1]));
    }

    [Fact]
    public async Task ReadText_HttpError_ReturnsNull()
    {
        _http.Status(HttpStatusCode.TooManyRequests);

        Assert.Null(await Recognizer().ReadTextAsync([1]));
    }

    [Fact]
    public async Task ReadText_NetworkFailure_ReturnsNull()
    {
        _http.Throws(new HttpRequestException("down"));

        Assert.Null(await Recognizer().ReadTextAsync([1]));
    }

    [Theory]
    [InlineData(null, "key", false)]
    [InlineData("https://vision.example", null, false)]
    [InlineData("https://vision.example", "key", true)]
    public void IsConfigured_RequiresEndpointAndKey(string? endpoint, string? key, bool expected)
    {
        Assert.Equal(expected, Recognizer(endpoint, key).IsConfigured);
    }

    private AzureVisionTextRecognizer Recognizer(string? endpoint = "https://vision.example", string? key = "vision-key") => new(
        _http.Client,
        Microsoft.Extensions.Options.Options.Create(new AzureVisionOptions { Endpoint = endpoint, ApiKey = key }),
        NullLogger<AzureVisionTextRecognizer>.Instance);
}

public class AzureOpenAIBookTextInterpreterTests
{
    private readonly FakeHttp _http = new();

    [Fact]
    public async Task Interpret_ParsesBooksFromJsonContent_AndSendsSchemaRequest()
    {
        RespondWithContent("""{"books":[{"title":"Dune","author":"Frank Herbert","isbn":null},{"title":" ","author":null,"isbn":null},{"title":"Kindred","author":null,"isbn":"9780807083697"}]}""");

        var books = await Interpreter().InterpretAsync("DUNE HERBERT KINDRED", ScanMode.Shelf);

        Assert.Equal(
            [new BookCandidate("Dune", "Frank Herbert", null), new BookCandidate("Kindred", null, "9780807083697")],
            books);

        var request = _http.Requests[0];
        Assert.Equal($"https://openai.example/{AzureOpenAIBookTextInterpreter.ChatPath}", request.RequestUri!.ToString());
        Assert.Equal("openai-key", Assert.Single(request.Headers.GetValues(AzureOpenAIBookTextInterpreter.KeyHeader)));
        using var body = JsonDocument.Parse(_http.RequestBodies[0]!);
        Assert.Equal("gpt-5-mini", body.RootElement.GetProperty("model").GetString());
        Assert.Equal("json_schema", body.RootElement.GetProperty("response_format").GetProperty("type").GetString());
        Assert.Contains("bookshelf", body.RootElement.GetProperty("messages")[0].GetProperty("content").GetString());
        Assert.Contains("DUNE HERBERT KINDRED", body.RootElement.GetProperty("messages")[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task Interpret_SingleMode_UsesSingleBookInstructions()
    {
        RespondWithContent("""{"books":[]}""");

        await Interpreter().InterpretAsync("text", ScanMode.Single);

        using var body = JsonDocument.Parse(_http.RequestBodies[0]!);
        Assert.Contains("ONE book", body.RootElement.GetProperty("messages")[0].GetProperty("content").GetString());
    }

    [Fact]
    public async Task Interpret_CapsNumberOfBooks()
    {
        var many = string.Join(',', Enumerable.Range(1, 40).Select(i => $$"""{"title":"Book {{i}}","author":null,"isbn":null}"""));
        RespondWithContent($$"""{"books":[{{many}}]}""");

        var books = await Interpreter().InterpretAsync("text", ScanMode.Shelf);

        Assert.Equal(AzureOpenAIBookTextInterpreter.MaxBooks, books.Count);
    }

    [Theory]
    [InlineData("""{"choices":[]}""")]
    [InlineData("""{"choices":[{"message":{"content":"not json"}}]}""")]
    [InlineData("""{"choices":[{"message":{"content":"{\"nope\":1}"}}]}""")]
    [InlineData("""{"choices":[{"message":{"content":null}}]}""")]
    public async Task Interpret_UnexpectedResponse_ReturnsEmpty(string json)
    {
        _http.Json(json);

        Assert.Empty(await Interpreter().InterpretAsync("text", ScanMode.Shelf));
    }

    [Fact]
    public async Task Interpret_HttpError_ReturnsEmpty()
    {
        _http.Status(HttpStatusCode.BadRequest);

        Assert.Empty(await Interpreter().InterpretAsync("text", ScanMode.Shelf));
    }

    [Theory]
    [InlineData(null, "gpt", "key", false)]
    [InlineData("https://openai.example", null, "key", false)]
    [InlineData("https://openai.example", "gpt", null, false)]
    [InlineData("https://openai.example/", "gpt", "key", true)]
    public void IsConfigured_RequiresEndpointDeploymentAndKey(string? endpoint, string? deployment, string? key, bool expected)
    {
        Assert.Equal(expected, Interpreter(endpoint, deployment, key).IsConfigured);
    }

    private void RespondWithContent(string content) =>
        _http.Json(JsonSerializer.Serialize(new { choices = new[] { new { message = new { content } } } }));

    private AzureOpenAIBookTextInterpreter Interpreter(string? endpoint = "https://openai.example", string? deployment = "gpt-5-mini", string? key = "openai-key") => new(
        _http.Client,
        Microsoft.Extensions.Options.Options.Create(new AzureOpenAIOptions { Endpoint = endpoint, Deployment = deployment, ApiKey = key }),
        NullLogger<AzureOpenAIBookTextInterpreter>.Instance);
}

public class ImageFormatTests
{
    [Theory]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, true)] // JPEG
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0 }, true)] // PNG
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, true)] // GIF89a
    [InlineData(new byte[] { 0x42, 0x4D, 0, 0 }, true)] // BMP
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x45, 0x42, 0x50 }, true)] // WEBP
    [InlineData(new byte[] { 0x52, 0x49, 0x46, 0x46, 0, 0, 0, 0, 0x57, 0x41, 0x56, 0x45 }, false)] // WAV
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46 }, false)] // PDF
    [InlineData(new byte[] { 0xFF }, false)]
    public void IsSupported_ChecksMagicBytes(byte[] data, bool expected)
    {
        Assert.Equal(expected, ImageFormat.IsSupported(data));
    }
}
