namespace Monolegal.Billing.Application.Invoices;

/// <summary>
/// Global invoice totals. Status keys are the exact persisted strings, including unknown ones.
/// </summary>
public sealed record InvoiceSummary(
    long TotalInvoices,
    IReadOnlyDictionary<string, long> CountByStatus,
    IReadOnlyDictionary<string, long> CountByCustomer,
    decimal TotalAmountCop);
