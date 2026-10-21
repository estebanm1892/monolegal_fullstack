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
            var query = new InvoiceQuery(page, pageSize, customerId, status);
            var result = await repository.ListAsync(query, ct);

            var dtos = result.Items.Select(i => new InvoiceItemDto(new InvoiceDto(
                i.Id.Value,
                i.CustomerId.Value,
                i.Number,
                i.IssuedAt,
                i.DueAt,
                i.UpdatedAt,
                i.Amount,
                i.Currency,
                i.State.Persisted))).ToArray();

            return Results.Ok(new PagedInvoicesDto(
                dtos,
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages));
        })
        .WithName("GetInvoices")
        .WithOpenApi();

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
        .WithOpenApi();
    }
}
