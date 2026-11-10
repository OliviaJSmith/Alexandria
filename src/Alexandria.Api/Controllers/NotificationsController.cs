using Alexandria.Api.Dtos;
using Alexandria.Api.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Alexandria.Api.Controllers;

[Route("api/notifications")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class NotificationsController(INotificationService notificationService) : ApiControllerBase
{
    /// <summary>The VAPID public key for <c>pushManager.subscribe</c>. 503 when notifications aren't configured.</summary>
    [HttpGet("web-push-key")]
    [ProducesResponseType<WebPushKeyDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<WebPushKeyDto> GetWebPushKey() => ToActionResult(notificationService.GetPublicKey());

    /// <summary>Saves this browser's push subscription for the signed-in user.</summary>
    [HttpPost("subscriptions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult> Subscribe([FromBody] SubscribePushRequest request, CancellationToken cancellationToken)
    {
        var result = await ForCurrentUserAsync(userId => notificationService.SubscribeAsync(userId, request, cancellationToken), _ => NoContent());
        return result.Result!;
    }

    /// <summary>Removes this browser's push subscription.</summary>
    [HttpDelete("subscriptions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Unsubscribe([FromBody] UnsubscribePushRequest request, CancellationToken cancellationToken)
    {
        var result = await ForCurrentUserAsync(
            userId => notificationService.UnsubscribeAsync(userId, request.Endpoint, cancellationToken),
            _ => NoContent());
        return result.Result!;
    }
}
