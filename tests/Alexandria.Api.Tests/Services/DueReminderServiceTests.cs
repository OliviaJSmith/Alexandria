using Alexandria.Api.Models;
using Alexandria.Api.Repositories;
using Alexandria.Api.Services.Notifications;
using Moq;

namespace Alexandria.Api.Tests.Services;

public class DueReminderServiceTests
{
    private readonly Mock<ILoanRepository> _loans = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly DueReminderService _service;

    public DueReminderServiceTests()
    {
        _service = new DueReminderService(_loans.Object, _notifications.Object, TestData.TimeProviderAt(TestData.Now).Object);
    }

    [Fact]
    public async Task Run_QueriesActiveLoansDueInTheNextTwoDays()
    {
        Setup();

        await _service.SendDueRemindersAsync();

        _loans.Verify(l => l.GetActiveDueBetweenNeedingReminderAsync(TestData.Now, TestData.Now.AddHours(48), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_DueInUnderADay_SendsTodayAndMarksBoth()
    {
        var loan = Loan(dueInHours: 10);
        Setup(loan);

        var result = await _service.SendDueRemindersAsync();

        Assert.Equal(new DueReminderRunDto(0, 1), result);
        _notifications.Verify(n => n.NotifyLoanDueAsync(loan, DueReminder.Today, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(TestData.Now, loan.DueTodayReminderSentAt);
        Assert.Equal(TestData.Now, loan.DueTomorrowReminderSentAt);
        _loans.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Run_DueInOneToTwoDays_SendsTomorrow()
    {
        var loan = Loan(dueInHours: 30);
        Setup(loan);

        var result = await _service.SendDueRemindersAsync();

        Assert.Equal(new DueReminderRunDto(1, 0), result);
        _notifications.Verify(n => n.NotifyLoanDueAsync(loan, DueReminder.Tomorrow, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(TestData.Now, loan.DueTomorrowReminderSentAt);
        Assert.Null(loan.DueTodayReminderSentAt);
    }

    [Fact]
    public async Task Run_TomorrowAlreadySent_DueTodayStillSent()
    {
        var loan = Loan(dueInHours: 5);
        loan.DueTomorrowReminderSentAt = TestData.Now.AddDays(-1);
        Setup(loan);

        await _service.SendDueRemindersAsync();

        _notifications.Verify(n => n.NotifyLoanDueAsync(loan, DueReminder.Today, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(TestData.Now.AddDays(-1), loan.DueTomorrowReminderSentAt);
    }

    [Fact]
    public async Task Run_RemindersAlreadySent_SendsNothingAndSkipsSave()
    {
        var today = Loan(dueInHours: 5);
        today.DueTodayReminderSentAt = TestData.Now.AddHours(-1);
        var tomorrow = Loan(dueInHours: 40);
        tomorrow.DueTomorrowReminderSentAt = TestData.Now.AddHours(-1);
        Setup(today, tomorrow);

        var result = await _service.SendDueRemindersAsync();

        Assert.Equal(new DueReminderRunDto(0, 0), result);
        _notifications.Verify(n => n.NotifyLoanDueAsync(It.IsAny<Loan>(), It.IsAny<DueReminder>(), It.IsAny<CancellationToken>()), Times.Never);
        _loans.Verify(l => l.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Run_ExactlyOneDayOut_CountsAsToday()
    {
        var loan = Loan(dueInHours: 24);
        Setup(loan);

        await _service.SendDueRemindersAsync();

        _notifications.Verify(n => n.NotifyLoanDueAsync(loan, DueReminder.Today, It.IsAny<CancellationToken>()), Times.Once);
    }

    private void Setup(params Loan[] loans) =>
        _loans.Setup(l => l.GetActiveDueBetweenNeedingReminderAsync(It.IsAny<DateTimeOffset>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(loans);

    private static Loan Loan(int dueInHours) => new()
    {
        Id = Guid.NewGuid(),
        LenderId = Guid.NewGuid(),
        BorrowerId = Guid.NewGuid(),
        Status = LoanStatus.Active,
        DueDate = TestData.Now.AddHours(dueInHours),
    };
}
