namespace Monolegal.Billing.Api.Contracts;

public sealed record CustomerDto(
    string Id,
    string Name,
    string Email,
    bool Active);

public sealed record InvoiceDto(
    string Id,
    string CustomerId,
    string Number,
    DateTimeOffset IssuedAt,
    DateTimeOffset DueAt,
    DateTimeOffset UpdatedAt,
    decimal Amount,
    string Currency,
    string State);

public sealed record InvoiceSummaryDto(
    long TotalInvoices,
    IReadOnlyDictionary<string, long> CountByStatus,
    IReadOnlyDictionary<string, long> CountByCustomer,
    decimal TotalAmountCop);

public sealed record PagedInvoicesDto(
    IReadOnlyList<InvoiceDto> Items,
    int Page,
    int PageSize,
    long TotalCount,
    long TotalPages);

public sealed record ReminderInvoiceResultDto(
    string InvoiceId,
    string Status,
    string? AttemptId,
    string? FailureCode,
    string? Details);

public sealed record ReminderProcessResponseDto(
    string BatchId,
    int TotalCandidates,
    int Succeeded,
    int Failed,
    int Skipped,
    int NeedsReview,
    IReadOnlyList<ReminderInvoiceResultDto> Results);

public sealed record ReminderRunAttemptDto(
    string AttemptId,
    string InvoiceId,
    string PreviousState,
    string NextState,
    int Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? SmtpAcceptedAt,
    DateTimeOffset? FinishedAt,
    string? MessageId,
    string? FailureCode);

public sealed record HealthResponseDto(string Status);
