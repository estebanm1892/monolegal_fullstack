using MongoDB.Driver;
using Monolegal.Billing.Application.Customers;
using Monolegal.Billing.Domain.Customers;
using Monolegal.Billing.Infrastructure.Mongo.Documents;

namespace Monolegal.Billing.Infrastructure.Mongo.Repositories;

public sealed class MongoCustomerRepository(MongoContext context) : ICustomerRepository
{
    public async Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken)
    {
        var documents = await context.Customers
            .Find(FilterDefinition<CustomerDocument>.Empty)
            .Sort(Builders<CustomerDocument>.Sort.Ascending(document => document.Id))
            .ToListAsync(cancellationToken);

        return documents.Select(MongoMapping.ToDomain).ToArray();
    }

    public async Task<Customer?> FindByIdAsync(CustomerId id, CancellationToken cancellationToken)
    {
        var document = await context.Customers
            .Find(Builders<CustomerDocument>.Filter.Eq(document => document.Id, id.Value))
            .FirstOrDefaultAsync(cancellationToken);

        return document is null ? null : MongoMapping.ToDomain(document);
    }
}
