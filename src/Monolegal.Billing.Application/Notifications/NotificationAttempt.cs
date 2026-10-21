using Monolegal.Billing.Domain.Customers;
using Monolegal.Billing.Domain.Invoices;

namespace Monolegal.Billing.Application.Notifications;

public enum NotificationAttemptStatus
{
    Claimed = 0,
    Dispatching = 1,
    SmtpAccepted = 2,
    Completed = 3,
    Failed = 4,
    NeedsReview = 5,
}

public sealed record NotificationAttempt(
    string AttemptId,
    string BatchId,
    InvoiceId InvoiceId,
    CustomerId CustomerId,
    InvoiceState PreviousState,
    InvoiceState NextState,
    NotificationAttemptStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? DispatchStartedAt = null,
    DateTimeOffset? SmtpAcceptedAt = null,
    DateTimeOffset? FinishedAt = null,
    string? MessageId = null,
    string? FailureCode = null);
