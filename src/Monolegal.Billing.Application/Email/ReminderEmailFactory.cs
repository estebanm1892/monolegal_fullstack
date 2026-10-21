using System.Globalization;
using Monolegal.Billing.Domain.Customers;
using Monolegal.Billing.Domain.Invoices;

namespace Monolegal.Billing.Application.Email;

public static class ReminderEmailFactory
{
    public static EmailMessage Create(Customer customer, Invoice invoice, InvoiceState nextState)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(nextState);

        if (customer.Id != invoice.CustomerId)
        {
            throw new InvalidOperationException("The invoice does not belong to the supplied customer.");
        }

        var subject = (invoice.State.Kind, nextState.Kind) switch
        {
            (InvoiceStateKind.PrimerRecordatorio, InvoiceStateKind.SegundoRecordatorio) =>
                $"Invoice {invoice.Number}: second reminder",
            (InvoiceStateKind.SegundoRecordatorio, InvoiceStateKind.Desactivado) =>
                $"Invoice {invoice.Number}: final reminder",
            _ => throw new InvalidOperationException("The invoice state transition does not produce a reminder email."),
        };

        var amount = invoice.Amount.ToString(CultureInfo.InvariantCulture);
        var body = $"""
            Hello {customer.Name},

            Invoice: {invoice.Number}
            Amount: {amount} {invoice.Currency}
            Reminder transition: {invoice.State.Persisted} -> {nextState.Persisted}

            This message was captured by the local Mailpit demo server.
            """;

        return new EmailMessage(customer.Email.Value, customer.Name, subject, body);
    }
}
