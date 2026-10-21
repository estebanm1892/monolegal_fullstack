using Monolegal.Billing.Domain.Customers;
using Monolegal.Billing.Domain.Invoices;

namespace Monolegal.Billing.Infrastructure.Seed;

internal static class SeedData
{
    private static readonly DateTimeOffset UpdatedAt = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    public static IReadOnlyList<Customer> Customers { get; } = new[]
    {
        new Customer(new CustomerId("cliente-a"), "Cliente A", EmailAddress.Parse("cliente-a@example.test"), true),
        new Customer(new CustomerId("cliente-b"), "Cliente B", EmailAddress.Parse("cliente-b@example.test"), true),
        new Customer(new CustomerId("cliente-c"), "Cliente C", EmailAddress.Parse("cliente-c@example.test"), true),
    };

    public static IReadOnlyList<Invoice> Invoices { get; } = new[]
    {
        Invoice("FAC-001", "cliente-a", 1, 10, 150000m, InvoiceState.PrimerRecordatorio),
        Invoice("FAC-002", "cliente-a", 2, 12, 85000m, InvoiceState.SegundoRecordatorio),
        Invoice("FAC-003", "cliente-b", 3, 14, 420000m, InvoiceState.PrimerRecordatorio),
        Invoice("FAC-004", "cliente-b", 4, 16, 99000m, InvoiceState.Pendiente),
        Invoice("FAC-005", "cliente-c", 5, 18, 230000m, InvoiceState.SegundoRecordatorio),
        Invoice("FAC-006", "cliente-c", 6, 20, 76000m, InvoiceState.Desactivado),
    };

    private static Invoice Invoice(
        string id,
        string customerId,
        int issuedDay,
        int dueDay,
        decimal amount,
        InvoiceState state) =>
        new(
            new InvoiceId(id),
            new CustomerId(customerId),
            id,
            new DateTimeOffset(2026, 9, issuedDay, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 9, dueDay, 0, 0, 0, TimeSpan.Zero),
            UpdatedAt,
            amount,
            state);
}
