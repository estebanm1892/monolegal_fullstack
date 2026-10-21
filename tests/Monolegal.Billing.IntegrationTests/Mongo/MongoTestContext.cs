using Microsoft.Extensions.Options;
using Monolegal.Billing.Infrastructure.Mongo;
using Monolegal.Billing.Infrastructure.Mongo.Repositories;
using Monolegal.Billing.Infrastructure.Seed;

namespace Monolegal.Billing.IntegrationTests.Mongo;

internal sealed class MongoTestContext : IAsyncDisposable
{
    private readonly string _databaseName;

    private MongoTestContext(string connectionString, string databaseName)
    {
        _databaseName = databaseName;
        Context = new MongoContext(Options.Create(new MongoOptions
        {
            ConnectionString = connectionString,
            DatabaseName = databaseName,
        }));

        Indexes = new MongoIndexInitializer(Context);
        Customers = new MongoCustomerRepository(Context);
        Invoices = new MongoInvoiceRepository(Context);
        Attempts = new MongoNotificationAuditRepository(Context);
        Seeder = new DevelopmentDataSeeder(Context, Indexes);
    }

    public MongoContext Context { get; }

    public MongoIndexInitializer Indexes { get; }

    public MongoCustomerRepository Customers { get; }

    public MongoInvoiceRepository Invoices { get; }

    public MongoNotificationAuditRepository Attempts { get; }

    public DevelopmentDataSeeder Seeder { get; }

    public static MongoTestContext Create(MongoFixture fixture) =>
        new(fixture.ConnectionString, $"monolegal_test_{Guid.NewGuid():N}");

    public async ValueTask DisposeAsync() =>
        await Context.Database.Client.DropDatabaseAsync(_databaseName);
}
