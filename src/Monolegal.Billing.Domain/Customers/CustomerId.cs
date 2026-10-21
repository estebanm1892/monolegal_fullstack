namespace Monolegal.Billing.Domain.Customers;

public sealed record CustomerId
{
    public CustomerId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Customer id is required.", nameof(value));
        }

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
