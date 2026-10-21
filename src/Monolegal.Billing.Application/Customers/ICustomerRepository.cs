using Monolegal.Billing.Domain.Customers;

namespace Monolegal.Billing.Application.Customers;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> ListAsync(CancellationToken cancellationToken);

    Task<Customer?> FindByIdAsync(CustomerId id, CancellationToken cancellationToken);
}
