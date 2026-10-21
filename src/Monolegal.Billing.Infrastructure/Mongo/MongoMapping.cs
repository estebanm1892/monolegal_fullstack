using Monolegal.Billing.Domain.Customers;
using Monolegal.Billing.Infrastructure.Mongo.Documents;

namespace Monolegal.Billing.Infrastructure.Mongo;

internal static class MongoMapping
{
    public static DateTime ToUtc(DateTimeOffset value) => DateTime.SpecifyKind(value.UtcDateTime, DateTimeKind.Utc);

    public static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    public static Customer ToDomain(CustomerDocument document) =>
        new(
            new CustomerId(document.Id),
            document.Nombre,
            EmailAddress.Parse(document.Email),
            document.Activo);

    public static CustomerDocument ToDocument(Customer customer) =>
        new()
        {
            Id = customer.Id.Value,
            Nombre = customer.Name,
            Email = customer.Email.Value,
            Activo = customer.Active,
        };

    public static Domain.Invoices.Invoice ToDomain(InvoiceDocument document) =>
        new(
            new Domain.Invoices.InvoiceId(document.Id),
            new CustomerId(document.ClienteId),
            document.Numero,
            ToOffset(document.FechaEmision),
            ToOffset(document.FechaVencimiento),
            ToOffset(document.ActualizadoEn),
            document.Valor,
            Domain.Invoices.InvoiceState.FromPersisted(document.Estado));
}
