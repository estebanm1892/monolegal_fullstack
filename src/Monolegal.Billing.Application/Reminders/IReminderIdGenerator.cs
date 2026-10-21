namespace Monolegal.Billing.Application.Reminders;

public interface IReminderIdGenerator
{
    string NewBatchId();

    string NewAttemptId();

    string NewClaimToken();
}

public sealed class GuidReminderIdGenerator : IReminderIdGenerator
{
    public string NewBatchId() => $"batch-{Guid.NewGuid():N}";

    public string NewAttemptId() => $"attempt-{Guid.NewGuid():N}";

    public string NewClaimToken() => $"claim-{Guid.NewGuid():N}";
}
