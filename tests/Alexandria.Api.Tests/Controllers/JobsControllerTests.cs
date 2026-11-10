using Alexandria.Api.Controllers;
using Alexandria.Api.Options;
using Alexandria.Api.Services.Notifications;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Alexandria.Api.Tests.Controllers;

public class JobsControllerTests
{
    private const string Key = "a-test-job-key-that-is-long-enough-123";

    private readonly Mock<IDueReminderService> _dueReminders = new();

    [Fact]
    public async Task DueReminders_RightKey_RunsAndReturnsCounts()
    {
        _dueReminders.Setup(d => d.SendDueRemindersAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new DueReminderRunDto(2, 1));

        var result = await Controller(Key).SendDueReminders(Key, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(new DueReminderRunDto(2, 1), ok.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong")]
    [InlineData(Key + "x")]
    public async Task DueReminders_WrongKey_ReturnsUnauthorizedWithoutRunning(string? provided)
    {
        var result = await Controller(Key).SendDueReminders(provided, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status401Unauthorized);
        _dueReminders.Verify(d => d.SendDueRemindersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("too-short")]
    public async Task DueReminders_NotConfigured_ReturnsServiceUnavailable(string? configuredKey)
    {
        var result = await Controller(configuredKey).SendDueReminders(configuredKey, CancellationToken.None);

        ControllerTestHelpers.AssertProblem(result.Result, StatusCodes.Status503ServiceUnavailable);
        _dueReminders.Verify(d => d.SendDueRemindersAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private JobsController Controller(string? configuredKey) =>
        new JobsController(_dueReminders.Object, Microsoft.Extensions.Options.Options.Create(new JobsOptions { Key = configuredKey })).WithContext();
}
