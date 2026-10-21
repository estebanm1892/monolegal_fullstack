using MongoDB.Driver;
using Monolegal.Billing.Infrastructure.Mongo.Documents;

namespace Monolegal.Billing.Infrastructure.Mongo;

public sealed class MongoIndexInitializer(MongoContext context) : IMongoIndexInitializer
{
    public async Task EnsureAsync(CancellationToken cancellationToken)
    {
        await context.Customers.Indexes.CreateManyAsync(
            new[]
            {
                new CreateIndexModel<CustomerDocument>(
                    Builders<CustomerDocument>.IndexKeys.Ascending(document => document.Email),
                    new CreateIndexOptions { Name = "clientes_email" }),
            },
            cancellationToken);

        await context.Invoices.Indexes.CreateManyAsync(
            new[]
            {
                new CreateIndexModel<InvoiceDocument>(
                    Builders<InvoiceDocument>.IndexKeys
                        .Ascending(document => document.ClienteId)
                        .Ascending(document => document.Numero),
                    new CreateIndexOptions { Name = "facturas_cliente_numero_unique", Unique = true }),
                new CreateIndexModel<InvoiceDocument>(
                    Builders<InvoiceDocument>.IndexKeys
                        .Ascending(document => document.Estado)
                        .Ascending(document => document.Processing!.LockUntil),
                    new CreateIndexOptions { Name = "facturas_estado_lock" }),
                new CreateIndexModel<InvoiceDocument>(
                    Builders<InvoiceDocument>.IndexKeys
                        .Ascending(document => document.ClienteId)
                        .Ascending(document => document.Estado)
                        .Ascending(document => document.Id),
                    new CreateIndexOptions { Name = "facturas_cliente_estado_id" }),
            },
            cancellationToken);

        await context.Attempts.Indexes.CreateManyAsync(
            new[]
            {
                new CreateIndexModel<NotificationAttemptDocument>(
                    Builders<NotificationAttemptDocument>.IndexKeys
                        .Ascending(document => document.InvoiceId)
                        .Ascending(document => document.StartedAt),
                    new CreateIndexOptions { Name = "intentos_invoice_started" }),
                new CreateIndexModel<NotificationAttemptDocument>(
                    Builders<NotificationAttemptDocument>.IndexKeys.Ascending(document => document.BatchId),
                    new CreateIndexOptions { Name = "intentos_batch" }),
            },
            cancellationToken);
    }
}
