using Monolegal.Billing.Domain.Invoices;

namespace Monolegal.Billing.Application.Reminders;

public enum ReminderProcessingStatus
{
    Succeeded = 0,
    Failed = 1,
    Skipped = 2,
    NeedsReview = 3,
}

public sealed record ReminderInvoiceResult(
    InvoiceId InvoiceId,
    ReminderProcessingStatus Status,
    string? AttemptId = null,
    string? FailureCode = null,
    string? Details = null);

public sealed record ReminderBatchResult(
    string BatchId,
    int TotalCandidates,
    IReadOnlyList<ReminderInvoiceResult> Results)
{
    public int Succeeded => Count(ReminderProcessingStatus.Succeeded);

    public int Failed => Count(ReminderProcessingStatus.Failed);

    public int Skipped => Count(ReminderProcessingStatus.Skipped);

    public int NeedsReview => Count(ReminderProcessingStatus.NeedsReview);

    private int Count(ReminderProcessingStatus status) => Results.Count(result => result.Status == status);
}
