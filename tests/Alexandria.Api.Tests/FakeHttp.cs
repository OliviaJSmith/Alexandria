using System.Net;
using System.Text;
using Moq;
using Moq.Protected;

namespace Alexandria.Api.Tests;

/// <summary>An HttpClient whose handler is a Moq mock returning canned responses in order, recording each request.</summary>
internal sealed class FakeHttp
{
    private readonly Mock<HttpMessageHandler> _handler = new(MockBehavior.Strict);
    private readonly Queue<Func<HttpResponseMessage>> _responses = new();

    public FakeHttp()
    {
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((request, _) =>
            {
                Requests.Add(request);
                return Task.FromResult(_responses.Dequeue()());
            });
        Client = new HttpClient(_handler.Object);
    }

    public HttpClient Client { get; }

    public List<HttpRequestMessage> Requests { get; } = [];

    public FakeHttp Json(string json)
    {
        _responses.Enqueue(() => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
        return this;
    }

    public FakeHttp Status(HttpStatusCode status)
    {
        _responses.Enqueue(() => new HttpResponseMessage(status));
        return this;
    }

    public FakeHttp Throws(Exception exception)
    {
        _responses.Enqueue(() => throw exception);
        return this;
    }
}
