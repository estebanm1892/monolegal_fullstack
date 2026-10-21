namespace Monolegal.Billing.Application.Invoices;

public sealed record InvoiceQuery
{
    public const int MaxPageSize = 100;

    public InvoiceQuery(int page = 1, int pageSize = 20, string? customerId = null, string? status = null)
    {
        if (page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page), "Page must be at least 1.");
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(pageSize), $"Page size must be between 1 and {MaxPageSize}.");
        }

        Page = page;
        PageSize = pageSize;
        CustomerId = string.IsNullOrWhiteSpace(customerId) ? null : customerId;
        Status = string.IsNullOrWhiteSpace(status) ? null : status;
    }

    public int Page { get; }

    public int PageSize { get; }

    public string? CustomerId { get; }

    /// <summary>Exact persisted status text; never normalized.</summary>
    public string? Status { get; }
}
