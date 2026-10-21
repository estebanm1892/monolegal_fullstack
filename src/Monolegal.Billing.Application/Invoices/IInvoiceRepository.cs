using Monolegal.Billing.Domain.Invoices;

namespace Monolegal.Billing.Application.Invoices;

public interface IInvoiceRepository
{
    Task<Invoice?> FindByIdAsync(InvoiceId id, CancellationToken cancellationToken);

    Task<PagedResult<Invoice>> ListAsync(InvoiceQuery query, CancellationToken cancellationToken);

    Task<InvoiceSummary> GetSummaryAsync(CancellationToken cancellationToken);

    /// <summary>Snapshot of invoices whose persisted state is currently eligible, ordered by id.</summary>
    Task<IReadOnlyList<InvoiceId>> FindEligibleIdsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Atomically claims one invoice. Returns null when the invoice is missing, its persisted state
    /// differs from <paramref name="expectedState"/>, or another active claim exists. Expired
    /// <c>Claimed</c> locks can be reclaimed; <c>Dispatching</c> locks never are.
    /// </summary>
    Task<InvoiceClaim?> TryClaimAsync(
        InvoiceId id,
        InvoiceState expectedState,
        InvoiceClaimRequest request,
        CancellationToken cancellationToken);

    /// <summary>Moves the active claim from <c>Claimed</c> to <c>Dispatching</c> before the SMTP call.</summary>
    Task<bool> TryBeginDispatchAsync(
        InvoiceId id,
        string token,
        DateTimeOffset lockUntil,
        CancellationToken cancellationToken);

    /// <summary>
    /// Compare-and-set: updates the persisted state only when id, previous state and token still match.
    /// Removes the claim on success.
    /// </summary>
    Task<bool> TryCompleteTransitionAsync(
        InvoiceId id,
        InvoiceState expectedState,
        string token,
        InvoiceState nextState,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken);

    Task<bool> ReleaseClaimAsync(InvoiceId id, string token, CancellationToken cancellationToken);
}
