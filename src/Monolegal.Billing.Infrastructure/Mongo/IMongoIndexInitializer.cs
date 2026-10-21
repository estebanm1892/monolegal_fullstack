namespace Monolegal.Billing.Infrastructure.Mongo;

public interface IMongoIndexInitializer
{
    Task EnsureAsync(CancellationToken cancellationToken);
}
