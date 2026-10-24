using Monolegal.Billing.Application.Email;
using Monolegal.Billing.Domain.Customers;
using Monolegal.Billing.Domain.Invoices;

namespace Monolegal.Billing.UnitTests.Email;

public class ReminderEmailFactoryTests
{
    private static readonly Customer Customer = new(
        new CustomerId("customer-a"),
        "Customer A",
        EmailAddress.Parse("customer-a@example.test"),
        true);

    [Fact]
    public void FirstReminderBuildsSecondReminderMessageWithInvoiceData()
    {
        var invoice = CreateInvoice(InvoiceState.PrimerRecordatorio);

        var message = ReminderEmailFactory.Create(Customer, invoice, InvoiceState.SegundoRecordatorio);

        Assert.Equal("customer-a@example.test", message.RecipientAddress);
        Assert.Equal("Customer A", message.RecipientName);
        Assert.Equal("Segundo recordatorio de pago – Factura FAC-100", message.Subject);
        Assert.Contains("Customer A", message.TextBody);
        Assert.Contains("FAC-100", message.TextBody);
        Assert.Contains("150.000 pesos colombianos (COP)", message.TextBody);
        Assert.Contains("factura quedará en segundo recordatorio", message.TextBody);
        Assert.DoesNotContain("primerrecordatorio", message.TextBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("segundorecordatorio", message.TextBody, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SecondReminderBuildsDistinctFinalReminderMessage()
    {
        var invoice = CreateInvoice(InvoiceState.SegundoRecordatorio);

        var message = ReminderEmailFactory.Create(Customer, invoice, InvoiceState.Desactivado);

        Assert.Equal("Aviso de desactivación – Factura FAC-100", message.Subject);
        Assert.Contains("Customer A", message.TextBody);
        Assert.Contains("FAC-100", message.TextBody);
        Assert.Contains("150.000 pesos colombianos (COP)", message.TextBody);
        Assert.Contains("factura quedará desactivada", message.TextBody);
        Assert.DoesNotContain("segundorecordatorio", message.TextBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("cuenta", message.TextBody, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Segundo recordatorio de pago", message.Subject);
    }

    [Fact]
    public void PreservesExactFractionalAmount()
    {
        var invoice = new Invoice(
            new InvoiceId("FAC-101"),
            Customer.Id,
            "FAC-101",
            new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero),
            123.456m,
            InvoiceState.PrimerRecordatorio);

        var message = ReminderEmailFactory.Create(Customer, invoice, InvoiceState.SegundoRecordatorio);

        Assert.Contains("123,456 pesos colombianos (COP)", message.TextBody);
    }

    private static Invoice CreateInvoice(InvoiceState state) => new(
        new InvoiceId("FAC-100"),
        Customer.Id,
        "FAC-100",
        new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 10, 15, 0, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero),
        150000m,
        state);
}
