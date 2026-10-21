namespace Monolegal.Billing.Application.Invoices;

public static class ClaimPhases
{
    public const string Claimed = "Claimed";
    public const string Dispatching = "Dispatching";
}

public sealed record InvoiceClaimRequest(string Token, DateTimeOffset StartedAt, DateTimeOffset LockUntil);

public sealed record InvoiceClaim(string Token, string Phase, DateTimeOffset StartedAt, DateTimeOffset LockUntil);
