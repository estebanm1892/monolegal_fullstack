namespace Monolegal.Billing.Domain.Customers;

public sealed record Customer
{
    public Customer(CustomerId id, string name, EmailAddress email, bool active)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Customer name is required.", nameof(name));
        }

        Name = name.Trim();
        Email = email ?? throw new ArgumentNullException(nameof(email));
        Active = active;
    }

    public CustomerId Id { get; }

    public string Name { get; }

    public EmailAddress Email { get; }

    /// <summary>Sample flag from the specification; no exclusion rule is derived from it.</summary>
    public bool Active { get; }
}
