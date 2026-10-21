using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Monolegal.Billing.Infrastructure.Mongo.Documents;

namespace Monolegal.Billing.Infrastructure.Mongo;

public sealed class MongoContext
{
    private readonly Lazy<IMongoDatabase> _database;

    public MongoContext(IOptions<MongoOptions> options)
    {
        _database = new Lazy<IMongoDatabase>(() =>
        {
            var value = options.Value;

            if (string.IsNullOrWhiteSpace(value.ConnectionString))
            {
                throw new InvalidOperationException(
                    "Mongo:ConnectionString is not configured. Set Mongo__ConnectionString or the MONGO_CONNECTION_STRING environment variable.");
            }

            if (string.IsNullOrWhiteSpace(value.DatabaseName))
            {
                throw new InvalidOperationException("Mongo:DatabaseName is not configured.");
            }

            return new MongoClient(value.ConnectionString).GetDatabase(value.DatabaseName);
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IMongoDatabase Database => _database.Value;

    internal IMongoCollection<CustomerDocument> Customers => Database.GetCollection<CustomerDocument>("clientes");

    internal IMongoCollection<InvoiceDocument> Invoices => Database.GetCollection<InvoiceDocument>("facturas");

    internal IMongoCollection<NotificationAttemptDocument> Attempts =>
        Database.GetCollection<NotificationAttemptDocument>("intentosNotificacion");
}
