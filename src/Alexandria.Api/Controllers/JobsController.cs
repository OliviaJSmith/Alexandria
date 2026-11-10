using System.Security.Cryptography;
using System.Text;
using Alexandria.Api.Options;
using Alexandria.Api.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Alexandria.Api.Controllers;

/// <summary>
/// Endpoints for the scheduler (a GitHub Actions cron), not for users. They take a shared key in the
/// <c>X-Job-Key</c> header instead of a user's sign-in token.
/// </summary>
[Route("api/jobs")]
[AllowAnonymous]
public class JobsController(IDueReminderService dueReminders, IOptions<JobsOptions> options) : ApiControllerBase
{
    /// <summary>Sends today's "due tomorrow" and "due today" reminders to borrowers.</summary>
    [HttpPost("due-reminders")]
    [ProducesResponseType<DueReminderRunDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<DueReminderRunDto>> SendDueReminders(
        [FromHeader(Name = JobsOptions.KeyHeader)] string? key,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.IsConfigured)
        {
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, detail: "Scheduled jobs aren't configured.");
        }

        if (!KeyMatches(key, settings.Key!))
        {
            return Problem(statusCode: StatusCodes.Status401Unauthorized, detail: "Missing or wrong job key.");
        }

        return Ok(await dueReminders.SendDueRemindersAsync(cancellationToken));
    }

    /// <summary>Constant-time comparison (of hashes, so lengths don't leak either).</summary>
    private static bool KeyMatches(string? provided, string expected) =>
        provided is not null
        && CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(provided)),
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)));
}
