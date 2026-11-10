using System.Security.Claims;
using Alexandria.Api.Controllers;
using Alexandria.Api.Dtos;
using Alexandria.Api.Services;
using Alexandria.Api.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Moq;

namespace Alexandria.Api.Tests.Controllers;

public class NotificationsControllerTests
{
    private readonly Mock<INotificationService> _service = new();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly NotificationsController _controller;

    private static readonly SubscribePushRequest Subscription = new()
    {
        Endpoint = "https://fcm.googleapis.com/fcm/send/abc",
        P256dh = "key",
        Auth = "auth",
    };

    public NotificationsControllerTests()
    {
        _controller = new NotificationsController(_service.Object).WithContext(new Claim(JwtRegisteredClaimNames.Sub, _userId.ToString()));
    }

    [Fact]
    public void GetWebPushKey_Configured_ReturnsOk()
    {
        _service.Setup(s => s.GetPublicKey()).Returns(ServiceResult<WebPushKeyDto>.Success(new WebPushKeyDto("public-key")));

        var ok = Assert.IsType<OkObjectResult>(_controller.GetWebPushKey().Result);
        Assert.Equal("public-key", Assert.IsType<WebPushKeyDto>(ok.Value).PublicKey);
    }

    [Fact]
    public void GetWebPushKey_NotConfigured_ReturnsServiceUnavailable()
    {
        _service.Setup(s => s.GetPublicKey())
            .Returns(ServiceResult<WebPushKeyDto>.Failure(ServiceErrorType.Unavailable, NotificationService.NotConfiguredMessage));

        ControllerTestHelpers.AssertProblem(_controller.GetWebPushKey().Result, StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public async Task Subscribe_Valid_ReturnsNoContent()
    {
        _service.Setup(s => s.SubscribeAsync(_userId, Subscription, It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<bool>.Success(true));

        Assert.IsType<NoContentResult>(await _controller.Subscribe(Subscription, CancellationToken.None));
    }

    [Fact]
    public async Task Subscribe_Invalid_ReturnsBadRequest()
    {
        _service.Setup(s => s.SubscribeAsync(_userId, Subscription, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<bool>.Failure(ServiceErrorType.Validation, NotificationService.InvalidEndpointMessage));

        ControllerTestHelpers.AssertProblem(await _controller.Subscribe(Subscription, CancellationToken.None), StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task Unsubscribe_Own_ReturnsNoContent()
    {
        _service.Setup(s => s.UnsubscribeAsync(_userId, Subscription.Endpoint, It.IsAny<CancellationToken>())).ReturnsAsync(ServiceResult<bool>.Success(true));

        var result = await _controller.Unsubscribe(new UnsubscribePushRequest { Endpoint = Subscription.Endpoint }, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Unsubscribe_Unknown_ReturnsNotFound()
    {
        _service.Setup(s => s.UnsubscribeAsync(_userId, Subscription.Endpoint, It.IsAny<CancellationToken>()))
            .ReturnsAsync(ServiceResult<bool>.Failure(ServiceErrorType.NotFound, NotificationService.SubscriptionNotFoundMessage));

        var result = await _controller.Unsubscribe(new UnsubscribePushRequest { Endpoint = Subscription.Endpoint }, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result, StatusCodes.Status404NotFound);
    }
}
