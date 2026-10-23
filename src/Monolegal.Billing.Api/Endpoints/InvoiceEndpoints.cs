using Monolegal.Billing.Application.Invoices;
using Monolegal.Billing.Api.Contracts;

namespace Monolegal.Billing.Api.Endpoints;

public static class InvoiceEndpoints
{
    public static void MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/invoices");

        group.MapGet("/", async (
            IInvoiceRepository repository,
            int page = 1,
            int pageSize = 20,
            string? customerId = null,
            string? status = null,
            CancellationToken ct = default) =>
        {
            if (page < 1)
            {
                return Results.Problem(
                    title: "Invalid pagination",
                    detail: "page must be at least 1.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            if (pageSize is < 1 or > InvoiceQuery.MaxPageSize)
            {
                return Results.Problem(
                    title: "Invalid pagination",
                    detail: $"pageSize must be between 1 and {InvoiceQuery.MaxPageSize}.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var query = new InvoiceQuery(page, pageSize, customerId, status);
            var result = await repository.ListAsync(query, ct);

            var dtos = result.Items.Select(i => new InvoiceDto(
                i.Id.Value,
                i.CustomerId.Value,
                i.Number,
                i.IssuedAt,
                i.DueAt,
                i.UpdatedAt,
                i.Amount,
                i.Currency,
                i.State.Persisted)).ToArray();

            return Results.Ok(new PagedInvoicesDto(
                dtos,
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages));
        })
        .WithName("GetInvoices")
        .WithTags("Facturas")
        .WithSummary("Consultar facturas")
        .WithDescription(
            "Consulta paginada de facturas. Los filtros de cliente y estado son opcionales y solo afectan esta consulta.")
        .Produces<PagedInvoicesDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/summary", async (
            IInvoiceRepository repository,
            CancellationToken ct = default) =>
        {
            var summary = await repository.GetSummaryAsync(ct);
            return Results.Ok(new InvoiceSummaryDto(
                summary.TotalInvoices,
                summary.CountByStatus,
                summary.CountByCustomer,
                summary.TotalAmountCop));
        })
        .WithName("GetInvoiceSummary")
        .WithTags("Facturas")
        .WithSummary("Consultar estadísticas generales")
        .WithDescription(
            "Devuelve el resumen global de todas las facturas por estado y cliente, además del valor total en COP. No aplica los filtros del dashboard.")
        .Produces<InvoiceSummaryDto>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
