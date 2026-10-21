namespace Monolegal.Billing.Infrastructure.Seed;

public sealed record SeedReport(int CustomersInserted, int InvoicesInserted, long CustomerCount, long InvoiceCount);
