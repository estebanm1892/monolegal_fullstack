using Monolegal.Billing.Application.Customers;
using Monolegal.Billing.Api.Contracts;

namespace Monolegal.Billing.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers");

        group.MapGet("/", async (ICustomerRepository repository, CancellationToken ct) =>
        {
            var customers = await repository.ListAsync(ct);
            var dtos = customers.Select(c => new CustomerDto(
                c.Id.Value,
                c.Name,
                c.Email.Value,
                c.Active)).ToArray();
            return Results.Ok(dtos);
        })
        .WithName("GetCustomers")
        .WithOpenApi();
    }
}
