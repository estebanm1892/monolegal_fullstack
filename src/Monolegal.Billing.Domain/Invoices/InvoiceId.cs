namespace Monolegal.Billing.Domain.Invoices;

public sealed record InvoiceId
{
    public InvoiceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Invoice id is required.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
