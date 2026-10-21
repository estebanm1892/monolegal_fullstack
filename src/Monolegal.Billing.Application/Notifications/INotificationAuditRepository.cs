namespace Monolegal.Billing.Application.Notifications;

public interface INotificationAuditRepository
{
    Task AddAsync(NotificationAttempt attempt, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(NotificationAttempt attempt, CancellationToken cancellationToken);

    Task<NotificationAttempt?> FindAsync(string attemptId, CancellationToken cancellationToken);
}
