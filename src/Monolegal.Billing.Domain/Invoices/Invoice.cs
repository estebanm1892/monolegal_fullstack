using Monolegal.Billing.Domain.Customers;

namespace Monolegal.Billing.Domain.Invoices;

public sealed record Invoice
{
    public const string CopCurrency = "COP";

    public Invoice(
        InvoiceId id,
        CustomerId customerId,
        string number,
        DateTimeOffset issuedAt,
        DateTimeOffset dueAt,
        DateTimeOffset updatedAt,
        decimal amount,
        InvoiceState state)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        CustomerId = customerId ?? throw new ArgumentNullException(nameof(customerId));

        if (string.IsNullOrWhiteSpace(number))
        {
            throw new ArgumentException("Invoice number is required.", nameof(number));
        }

        Number = number.Trim();
        IssuedAt = RequireUtc(issuedAt, nameof(issuedAt));
        DueAt = RequireUtc(dueAt, nameof(dueAt));
        UpdatedAt = RequireUtc(updatedAt, nameof(updatedAt));
        Amount = amount;
        State = state ?? throw new ArgumentNullException(nameof(state));
    }

    public InvoiceId Id { get; }

    public CustomerId CustomerId { get; }

    public string Number { get; }

    public DateTimeOffset IssuedAt { get; }

    public DateTimeOffset DueAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public decimal Amount { get; }

    public string Currency => CopCurrency;

    public InvoiceState State { get; }

    private static DateTimeOffset RequireUtc(DateTimeOffset value, string parameterName) =>
        value.Offset == TimeSpan.Zero
            ? value
            : throw new ArgumentException("Invoice dates must be UTC.", parameterName);
}
