namespace Monolegal.Billing.Application.Invoices;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount)
{
    public long TotalPages => PageSize == 0 ? 0 : (TotalCount + PageSize - 1) / PageSize;
}
