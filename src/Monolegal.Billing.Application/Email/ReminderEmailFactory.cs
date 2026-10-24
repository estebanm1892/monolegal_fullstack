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

        var (subject, nextStep) = (invoice.State.Kind, nextState.Kind) switch
        {
            (InvoiceStateKind.PrimerRecordatorio, InvoiceStateKind.SegundoRecordatorio) =>
                ($"Segundo recordatorio de pago – Factura {invoice.Number}", "factura quedará en segundo recordatorio"),
            (InvoiceStateKind.SegundoRecordatorio, InvoiceStateKind.Desactivado) =>
                ($"Aviso de desactivación – Factura {invoice.Number}", "factura quedará desactivada"),
            _ => throw new InvalidOperationException("The invoice state transition does not produce a reminder email."),
        };

        var amount = invoice.Amount.ToString("#,0.############################", CultureInfo.GetCultureInfo("es-CO"));
        var body = $"""
            Cordial saludo, {customer.Name}:

            Le informamos sobre la factura {invoice.Number}.
            Valor: {amount} pesos colombianos ({invoice.Currency}).

            Si se confirma el procesamiento de este aviso, la {nextStep}.
            """;

        return new EmailMessage(customer.Email.Value, customer.Name, subject, body);
    }
}
