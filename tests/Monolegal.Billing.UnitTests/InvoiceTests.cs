using Monolegal.Billing.Domain.Customers;
using Monolegal.Billing.Domain.Invoices;

namespace Monolegal.Billing.UnitTests;

public class InvoiceTests
{
    private static readonly DateTimeOffset Issued = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Due = new(2026, 10, 31, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Updated = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreatesInvoiceWithRequiredData()
    {
        var invoice = CreateInvoice();

        Assert.Equal("FAC-001", invoice.Id.Value);
        Assert.Equal("cliente-a", invoice.CustomerId.Value);
        Assert.Equal("FAC-001", invoice.Number);
        Assert.Equal(150000m, invoice.Amount);
        Assert.Equal("COP", invoice.Currency);
        Assert.Equal(InvoiceState.PrimerRecordatorio, invoice.State);
        Assert.Equal(Issued, invoice.IssuedAt);
        Assert.Equal(Due, invoice.DueAt);
        Assert.Equal(Updated, invoice.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsEmptyInvoiceId(string? id)
    {
        Assert.Throws<ArgumentException>(() => new InvoiceId(id!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RejectsEmptyNumber(string? number)
    {
        Assert.Throws<ArgumentException>(() => CreateInvoice(number: number));
    }

    [Theory]
    [InlineData("2026-10-01T00:00:00-05:00")]
    [InlineData("2026-10-01T05:00:00+02:00")]
    public void RejectsNonUtcDates(string issuedAt)
    {
        Assert.Throws<ArgumentException>(() => CreateInvoice(issuedAt: DateTimeOffset.Parse(issuedAt)));
    }

    [Fact]
    public void PreservesUnknownStateVerbatim()
    {
        var invoice = CreateInvoice(state: InvoiceState.FromPersisted("archivada"));

        Assert.Equal(InvoiceStateKind.Unknown, invoice.State.Kind);
        Assert.Equal("archivada", invoice.State.Persisted);
    }

    private static Invoice CreateInvoice(
        string? number = "FAC-001",
        DateTimeOffset? issuedAt = null,
        InvoiceState? state = null) =>
        new(
            new InvoiceId("FAC-001"),
            new CustomerId("cliente-a"),
            number!,
            issuedAt ?? Issued,
            Due,
            Updated,
            150000m,
            state ?? InvoiceState.PrimerRecordatorio);
}
