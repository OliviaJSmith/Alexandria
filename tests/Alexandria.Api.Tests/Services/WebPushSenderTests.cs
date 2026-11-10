using System.Net;
using System.Security.Cryptography;
using Alexandria.Api.Dtos;
using Alexandria.Api.Models;
using Alexandria.Api.Options;
using Alexandria.Api.Services.Notifications;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using PushSubscription = Alexandria.Api.Models.PushSubscription;

namespace Alexandria.Api.Tests.Services;

public class WebPushSenderTests
{
    private static readonly (string PublicKey, string PrivateKey) Vapid = GenerateVapidKeys();
    private static readonly PushMessage Message = new("Borrow request", "Pat wants to borrow Dune.", "/loans");

    [Fact]
    public async Task Send_Accepted_ReturnsSentWithEncryptedVapidRequest()
    {
        var http = new FakeHttp().Status(HttpStatusCode.Created);

        var result = await Sender(http).SendAsync(BrowserSubscription(), Message);

        Assert.Equal(PushSendResult.Sent, result);
        var request = Assert.Single(http.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://fcm.googleapis.com/fcm/send/abc123", request.RequestUri!.ToString());
        Assert.StartsWith("vapid t=", request.Headers.Authorization!.ToString());
        Assert.Contains($"k={Vapid.PublicKey}", request.Headers.Authorization!.ToString());
        Assert.Equal(WebPushSender.TimeToLiveSeconds.ToString(), Assert.Single(request.Headers.GetValues("TTL")));
        Assert.Equal("high", Assert.Single(request.Headers.GetValues("Urgency")));
        Assert.Equal("aes128gcm", Assert.Single(request.Content!.Headers.ContentEncoding));
        // The body is encrypted, so the plain message must not appear in it.
        Assert.DoesNotContain("Dune", http.RequestBodies[0] ?? string.Empty);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    public async Task Send_SubscriptionGone_ReturnsGone(HttpStatusCode status)
    {
        var http = new FakeHttp().Status(status);

        Assert.Equal(PushSendResult.Gone, await Sender(http).SendAsync(BrowserSubscription(), Message));
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Send_OtherError_ReturnsFailed(HttpStatusCode status)
    {
        var http = new FakeHttp().Status(status);

        Assert.Equal(PushSendResult.Failed, await Sender(http).SendAsync(BrowserSubscription(), Message));
    }

    [Fact]
    public async Task Send_NotConfigured_ReturnsFailedWithoutCalling()
    {
        var http = new FakeHttp();
        var sender = new WebPushSender(http.Client, Microsoft.Extensions.Options.Options.Create(new WebPushOptions()), NullLogger<WebPushSender>.Instance);

        Assert.False(sender.IsConfigured);
        Assert.Equal(PushSendResult.Failed, await sender.SendAsync(BrowserSubscription(), Message));
        Assert.Empty(http.Requests);
    }

    [Fact]
    public void PublicKey_ComesFromOptions()
    {
        var sender = Sender(new FakeHttp());

        Assert.True(sender.IsConfigured);
        Assert.Equal(Vapid.PublicKey, sender.PublicKey);
    }

    private static WebPushSender Sender(FakeHttp http) => new(
        http.Client,
        Microsoft.Extensions.Options.Options.Create(new WebPushOptions
        {
            Subject = "https://alexandria.example",
            PublicKey = Vapid.PublicKey,
            PrivateKey = Vapid.PrivateKey,
        }),
        NullLogger<WebPushSender>.Instance);

    /// <summary>A VAPID key pair as base64url raw keys: 65-byte uncompressed public point, 32-byte private scalar.</summary>
    private static (string PublicKey, string PrivateKey) GenerateVapidKeys()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var parameters = key.ExportParameters(true);
        return (WebEncoders.Base64UrlEncode(Uncompressed(parameters.Q)), WebEncoders.Base64UrlEncode(parameters.D!));
    }

    private static byte[] Uncompressed(ECPoint point)
    {
        var bytes = new byte[65];
        bytes[0] = 0x04;
        point.X!.CopyTo(bytes, 1);
        point.Y!.CopyTo(bytes, 33);
        return bytes;
    }

    /// <summary>A subscription with real browser-style keys, so the package can encrypt for it.</summary>
    private static PushSubscription BrowserSubscription()
    {
        using var browserKey = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        var uncompressed = Uncompressed(browserKey.ExportParameters(false).Q);
        return new PushSubscription
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Endpoint = "https://fcm.googleapis.com/fcm/send/abc123",
            P256dh = WebEncoders.Base64UrlEncode(uncompressed),
            Auth = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(16)),
        };
    }
}
