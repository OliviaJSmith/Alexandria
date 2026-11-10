using Alexandria.Api.Models;
using Alexandria.Api.Repositories;

namespace Alexandria.Api.Services.Notifications;

/// <summary>How many reminders one run sent.</summary>
public sealed record DueReminderRunDto(int DueTomorrow, int DueToday);

/// <summary>Sends borrowers "due tomorrow" and "due today" reminders. Called once a day by a scheduled job.</summary>
public interface IDueReminderService
{
    Task<DueReminderRunDto> SendDueRemindersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Due dates are exact moments (e.g. "14 days from when it was lent"), so "today" means due within the next
/// 24 hours and "tomorrow" means due in 24–48 hours. Run once a day, each loan gets each reminder once.
/// Loans already past due get nothing here.
/// </summary>
public class DueReminderService(
    ILoanRepository loans,
    INotificationService notifications,
    TimeProvider timeProvider) : IDueReminderService
{
    public static readonly TimeSpan Day = TimeSpan.FromHours(24);

    public async Task<DueReminderRunDto> SendDueRemindersAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var due = await loans.GetActiveDueBetweenNeedingReminderAsync(now, now + Day + Day, cancellationToken);

        int tomorrow = 0, today = 0;
        foreach (var loan in due)
        {
            if (loan.DueDate <= now + Day)
            {
                if (loan.DueTodayReminderSentAt is not null)
                {
                    continue;
                }

                await notifications.NotifyLoanDueAsync(loan, DueReminder.Today, cancellationToken);
                loan.DueTodayReminderSentAt = now;
                // Too late for "tomorrow" now; never send it after "today".
                loan.DueTomorrowReminderSentAt ??= now;
                today++;
            }
            else if (loan.DueTomorrowReminderSentAt is null)
            {
                await notifications.NotifyLoanDueAsync(loan, DueReminder.Tomorrow, cancellationToken);
                loan.DueTomorrowReminderSentAt = now;
                tomorrow++;
            }
        }

        if (tomorrow + today > 0)
        {
            await loans.SaveChangesAsync(cancellationToken);
        }

        return new DueReminderRunDto(tomorrow, today);
    }
}
