using System.Net;
using System.Text;
using Alexandria.Api.Options;
using Alexandria.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;

namespace Alexandria.Api.Tests.Services;

public class GoogleTokenValidatorTests
{
    private const string AccessToken = "google-access-token";

    private readonly Mock<HttpMessageHandler> _handler = new(MockBehavior.Strict);
    private readonly List<(HttpRequestMessage Request, string? Body)> _requests = [];
    private readonly GoogleTokenValidator _validator;

    public GoogleTokenValidatorTests()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new GoogleOptions { ClientIds = [TestData.GoogleClientId] });
        _validator = new GoogleTokenValidator(new HttpClient(_handler.Object), options, NullLogger<GoogleTokenValidator>.Instance);
    }

    [Fact]
    public async Task Validate_TokenForOurClient_ReturnsGoogleUser()
    {
        Respond(TokenInfoJson(), UserInfoJson());

        var user = await _validator.ValidateAccessTokenAsync(AccessToken);

        Assert.NotNull(user);
        Assert.Equal("google-sub-1", user.SubjectId);
        Assert.Equal("reader@example.com", user.Email);
        Assert.Equal("Avid Reader", user.Name);
        Assert.Equal("https://example.com/me.png", user.PictureUrl);
    }

    [Fact]
    public async Task Validate_SendsTokenInRequestBodyAndAuthorizationHeader_NeverInTheUrl()
    {
        Respond(TokenInfoJson(), UserInfoJson());

        await _validator.ValidateAccessTokenAsync(AccessToken);

        var (tokenInfoRequest, tokenInfoBody) = _requests[0];
        Assert.Equal(HttpMethod.Post, tokenInfoRequest.Method);
        Assert.Equal(GoogleTokenValidator.TokenInfoUrl, tokenInfoRequest.RequestUri!.ToString());
        Assert.Equal($"access_token={AccessToken}", tokenInfoBody);

        var (userInfoRequest, _) = _requests[1];
        Assert.Equal(GoogleTokenValidator.UserInfoUrl, userInfoRequest.RequestUri!.ToString());
        Assert.Equal("Bearer", userInfoRequest.Headers.Authorization!.Scheme);
        Assert.Equal(AccessToken, userInfoRequest.Headers.Authorization.Parameter);
        Assert.DoesNotContain(_requests, r => r.Request.RequestUri!.Query.Contains(AccessToken));
    }

    [Fact]
    public async Task Validate_TokenIssuedToAnotherClient_ReturnsNull()
    {
        Respond(TokenInfoJson(aud: "someone-elses-client.apps.googleusercontent.com"));

        Assert.Null(await _validator.ValidateAccessTokenAsync(AccessToken));
        Assert.Single(_requests);
    }

    [Theory]
    [InlineData("\"false\"")]
    [InlineData("false")]
    [InlineData("null")]
    public async Task Validate_UnverifiedEmail_ReturnsNull(string emailVerifiedJson)
    {
        Respond(TokenInfoJson(emailVerifiedJson: emailVerifiedJson));

        Assert.Null(await _validator.ValidateAccessTokenAsync(AccessToken));
    }

    [Fact]
    public async Task Validate_EmailVerifiedAsJsonBoolean_IsAccepted()
    {
        Respond(TokenInfoJson(emailVerifiedJson: "true"), UserInfoJson());

        Assert.NotNull(await _validator.ValidateAccessTokenAsync(AccessToken));
    }

    [Fact]
    public async Task Validate_TokenInfoMissingEmail_ReturnsNull()
    {
        Respond("""{"aud":"test-client.apps.googleusercontent.com","sub":"google-sub-1","email_verified":"true"}""");

        Assert.Null(await _validator.ValidateAccessTokenAsync(AccessToken));
    }

    [Fact]
    public async Task Validate_GoogleRejectsToken_ReturnsNull()
    {
        RespondWith(new HttpResponseMessage(HttpStatusCode.BadRequest));

        Assert.Null(await _validator.ValidateAccessTokenAsync(AccessToken));
    }

    [Fact]
    public async Task Validate_UserInfoRequestFails_ReturnsNull()
    {
        RespondWith(Json(TokenInfoJson()), new HttpResponseMessage(HttpStatusCode.Unauthorized));

        Assert.Null(await _validator.ValidateAccessTokenAsync(AccessToken));
    }

    [Fact]
    public async Task Validate_UserInfoForDifferentAccount_ReturnsNull()
    {
        Respond(TokenInfoJson(), UserInfoJson(sub: "google-sub-2"));

        Assert.Null(await _validator.ValidateAccessTokenAsync(AccessToken));
    }

    [Fact]
    public async Task Validate_UserInfoWithoutName_FallsBackToEmail()
    {
        Respond(TokenInfoJson(), """{"sub":"google-sub-1"}""");

        var user = await _validator.ValidateAccessTokenAsync(AccessToken);

        Assert.Equal("reader@example.com", user!.Name);
        Assert.Null(user.PictureUrl);
    }

    [Fact]
    public async Task Validate_MalformedJson_ReturnsNull()
    {
        Respond("not json");

        Assert.Null(await _validator.ValidateAccessTokenAsync(AccessToken));
    }

    [Fact]
    public async Task Validate_NetworkFailure_ReturnsNull()
    {
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("network down"));

        Assert.Null(await _validator.ValidateAccessTokenAsync(AccessToken));
    }

    [Fact]
    public async Task Validate_CallerCancels_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        await Assert.ThrowsAsync<TaskCanceledException>(() => _validator.ValidateAccessTokenAsync(AccessToken, cts.Token));
    }

    private static string TokenInfoJson(string aud = TestData.GoogleClientId, string emailVerifiedJson = "\"true\"") =>
        $$"""{"aud":"{{aud}}","sub":"google-sub-1","email":"reader@example.com","email_verified":{{emailVerifiedJson}},"expires_in":"3599"}""";

    private static string UserInfoJson(string sub = "google-sub-1") =>
        $$"""{"sub":"{{sub}}","name":"Avid Reader","picture":"https://example.com/me.png","email":"reader@example.com"}""";

    private static HttpResponseMessage Json(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private void Respond(params string[] jsonResponses) => RespondWith(jsonResponses.Select(Json).ToArray());

    private void RespondWith(params HttpResponseMessage[] responses)
    {
        var queue = new Queue<HttpResponseMessage>(responses);
        _handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>(async (request, _) =>
            {
                var body = request.Content is null ? null : await request.Content.ReadAsStringAsync();
                _requests.Add((request, body));
                return queue.Dequeue();
            });
    }
}
