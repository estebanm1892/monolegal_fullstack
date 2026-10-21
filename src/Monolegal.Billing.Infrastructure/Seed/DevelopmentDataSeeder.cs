using MongoDB.Driver;
using Monolegal.Billing.Infrastructure.Mongo;
using Monolegal.Billing.Infrastructure.Mongo.Documents;

namespace Monolegal.Billing.Infrastructure.Seed;

public interface IDevelopmentDataSeeder
{
    Task<SeedReport> SeedAsync(bool reset, CancellationToken cancellationToken);
}

public sealed class DevelopmentDataSeeder(MongoContext context, IMongoIndexInitializer indexInitializer)
    : IDevelopmentDataSeeder
{
    public async Task<SeedReport> SeedAsync(bool reset, CancellationToken cancellationToken)
    {
        await indexInitializer.EnsureAsync(cancellationToken);

        if (reset)
        {
            await ResetAsync(cancellationToken);
        }

        var customersInserted = 0;
        var invoicesInserted = 0;

        foreach (var customer in SeedData.Customers)
        {
            var result = await context.Customers.UpdateOneAsync(
                Builders<CustomerDocument>.Filter.Eq(document => document.Id, customer.Id.Value),
                Builders<CustomerDocument>.Update
                    .SetOnInsert(document => document.Nombre, customer.Name)
                    .SetOnInsert(document => document.Email, customer.Email.Value)
                    .SetOnInsert(document => document.Activo, customer.Active),
                new UpdateOptions { IsUpsert = true },
                cancellationToken);

            if (result.UpsertedId is not null)
            {
                customersInserted++;
            }
        }

        foreach (var invoice in SeedData.Invoices)
        {
            var result = await context.Invoices.UpdateOneAsync(
                Builders<InvoiceDocument>.Filter.Eq(document => document.Id, invoice.Id.Value),
                Builders<InvoiceDocument>.Update
                    .SetOnInsert(document => document.ClienteId, invoice.CustomerId.Value)
                    .SetOnInsert(document => document.Numero, invoice.Number)
                    .SetOnInsert(document => document.FechaEmision, MongoMapping.ToUtc(invoice.IssuedAt))
                    .SetOnInsert(document => document.FechaVencimiento, MongoMapping.ToUtc(invoice.DueAt))
                    .SetOnInsert(document => document.ActualizadoEn, MongoMapping.ToUtc(invoice.UpdatedAt))
                    .SetOnInsert(document => document.Valor, invoice.Amount)
                    .SetOnInsert(document => document.Moneda, invoice.Currency)
                    .SetOnInsert(document => document.Estado, invoice.State.Persisted),
                new UpdateOptions { IsUpsert = true },
                cancellationToken);

            if (result.UpsertedId is not null)
            {
                invoicesInserted++;
            }
        }

        var customerCount = await context.Customers.CountDocumentsAsync(
            FilterDefinition<CustomerDocument>.Empty,
            cancellationToken: cancellationToken);
        var invoiceCount = await context.Invoices.CountDocumentsAsync(
            FilterDefinition<InvoiceDocument>.Empty,
            cancellationToken: cancellationToken);

        return new SeedReport(customersInserted, invoicesInserted, customerCount, invoiceCount);
    }

    private async Task ResetAsync(CancellationToken cancellationToken)
    {
        var customerIds = SeedData.Customers.Select(customer => customer.Id.Value).ToArray();
        var invoiceIds = SeedData.Invoices.Select(invoice => invoice.Id.Value).ToArray();

        await context.Attempts.DeleteManyAsync(
            Builders<NotificationAttemptDocument>.Filter.In(document => document.InvoiceId, invoiceIds),
            cancellationToken);

        await context.Invoices.DeleteManyAsync(
            Builders<InvoiceDocument>.Filter.In(document => document.Id, invoiceIds),
            cancellationToken);

        await context.Customers.DeleteManyAsync(
            Builders<CustomerDocument>.Filter.In(document => document.Id, customerIds),
            cancellationToken);
    }
}
