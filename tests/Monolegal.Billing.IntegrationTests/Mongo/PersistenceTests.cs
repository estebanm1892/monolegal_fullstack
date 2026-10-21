using MongoDB.Bson;
using MongoDB.Driver;
using Monolegal.Billing.Application.Customers;
using Monolegal.Billing.Application.Invoices;
using Monolegal.Billing.Application.Notifications;
using Monolegal.Billing.Domain.Customers;
using Monolegal.Billing.Domain.Invoices;
using Monolegal.Billing.Infrastructure.Mongo;
using Xunit;

namespace Monolegal.Billing.IntegrationTests.Mongo;

[Collection("mongo")]
public class PersistenceTests(MongoFixture fixture)
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SeedInsertsThreeCustomersAndSixInvoicesIdempotently()
    {
        await using var db = MongoTestContext.Create(fixture);

        var first = await db.Seeder.SeedAsync(reset: false, CancellationToken.None);
        var second = await db.Seeder.SeedAsync(reset: false, CancellationToken.None);

        Assert.Equal(3, first.CustomersInserted);
        Assert.Equal(6, first.InvoicesInserted);
        Assert.Equal(0, second.CustomersInserted);
        Assert.Equal(0, second.InvoicesInserted);
        Assert.Equal(3L, second.CustomerCount);
        Assert.Equal(6L, second.InvoiceCount);
    }

    [Fact]
    public async Task SecondSeedDoesNotOverwriteProcessedState()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);

        var invoices = db.Context.Database.GetCollection<BsonDocument>("facturas");
        await invoices.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", "FAC-001"),
            Builders<BsonDocument>.Update.Set("estado", "desactivado"));

        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);

        var stored = await invoices.Find(Builders<BsonDocument>.Filter.Eq("_id", "FAC-001")).SingleAsync();
        Assert.Equal("desactivado", stored["estado"].AsString);
        Assert.Equal(6L, await invoices.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
    }

    [Fact]
    public async Task ResetRestoresSeedStatesAndRemovesOnlySeedDocuments()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);
        var invoices = db.Context.Database.GetCollection<BsonDocument>("facturas");
        await invoices.UpdateOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", "FAC-001"),
            Builders<BsonDocument>.Update.Set("estado", "desactivado"));
        await invoices.InsertOneAsync(new BsonDocument
        {
            { "_id", "FAC-999" },
            { "clienteId", "cliente-a" },
            { "numero", "FAC-999" },
            { "estado", "primerrecordatorio" },
        });

        var report = await db.Seeder.SeedAsync(reset: true, CancellationToken.None);

        Assert.Equal(3, report.CustomersInserted);
        Assert.Equal(6, report.InvoicesInserted);
        Assert.Equal(7L, await invoices.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty));
        var restored = await invoices.Find(Builders<BsonDocument>.Filter.Eq("_id", "FAC-001")).SingleAsync();
        Assert.Equal("primerrecordatorio", restored["estado"].AsString);
    }

    [Fact]
    public async Task IndexesMatchTheDeclaredContract()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Indexes.EnsureAsync(CancellationToken.None);

        var invoiceIndexes = await (await db.Context.Database.GetCollection<BsonDocument>("facturas")
            .Indexes.ListAsync()).ToListAsync();
        var customerIndexes = await (await db.Context.Database.GetCollection<BsonDocument>("clientes")
            .Indexes.ListAsync()).ToListAsync();
        var attemptIndexes = await (await db.Context.Database.GetCollection<BsonDocument>("intentosNotificacion")
            .Indexes.ListAsync()).ToListAsync();

        Assert.Contains(invoiceIndexes, index => index["name"] == "facturas_cliente_numero_unique" && index["unique"].AsBoolean);
        Assert.Contains(invoiceIndexes, index => index["name"] == "facturas_estado_lock");
        Assert.Contains(invoiceIndexes, index => index["name"] == "facturas_cliente_estado_id");
        Assert.Contains(customerIndexes, index => index["name"] == "clientes_email");
        Assert.Contains(attemptIndexes, index => index["name"] == "intentos_invoice_started");
        Assert.Contains(attemptIndexes, index => index["name"] == "intentos_batch");
    }

    [Fact]
    public async Task UniqueInvoiceNumberPerCustomerIsEnforced()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);

        var invoices = db.Context.Database.GetCollection<BsonDocument>("facturas");
        var duplicate = new BsonDocument
        {
            { "_id", "FAC-DUP" },
            { "clienteId", "cliente-a" },
            { "numero", "FAC-001" },
            { "estado", "pendiente" },
        };

        var exception = await Assert.ThrowsAsync<MongoWriteException>(() => invoices.InsertOneAsync(duplicate));
        Assert.Equal(11000, exception.WriteError.Code);
    }

    [Fact]
    public async Task ListFiltersByCustomerAndExactStatusAndPaginatesStably()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);

        var firstPage = await db.Invoices.ListAsync(new InvoiceQuery(page: 1, pageSize: 2), CancellationToken.None);
        var lastPage = await db.Invoices.ListAsync(new InvoiceQuery(page: 3, pageSize: 2), CancellationToken.None);
        var customerAFiltered = await db.Invoices.ListAsync(
            new InvoiceQuery(customerId: "cliente-a", status: "primerrecordatorio"),
            CancellationToken.None);
        var wrongCase = await db.Invoices.ListAsync(
            new InvoiceQuery(status: "PRIMERRECORDATORIO"),
            CancellationToken.None);
        var unknownStatus = await db.Invoices.ListAsync(
            new InvoiceQuery(status: "archivada"),
            CancellationToken.None);

        Assert.Equal(6L, firstPage.TotalCount);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(3L, firstPage.TotalPages);
        Assert.Equal(2, lastPage.Items.Count);
        var all = firstPage.Items.Concat(
                (await db.Invoices.ListAsync(new InvoiceQuery(page: 2, pageSize: 2), CancellationToken.None)).Items)
            .Concat(lastPage.Items)
            .ToArray();
        Assert.Equal(6, all.Select(invoice => invoice.Id.Value).Distinct().Count());
        Assert.Equal(
            all.OrderBy(invoice => invoice.DueAt).ThenBy(invoice => invoice.Id.Value, StringComparer.Ordinal)
                .Select(invoice => invoice.Id.Value),
            all.Select(invoice => invoice.Id.Value));
        Assert.Single(customerAFiltered.Items);
        Assert.Equal("FAC-001", customerAFiltered.Items[0].Id.Value);
        Assert.Empty(wrongCase.Items);
        Assert.Empty(unknownStatus.Items);
    }

    [Fact]
    public async Task QueryRejectsOutOfRangePaging()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InvoiceQuery(page: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InvoiceQuery(pageSize: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InvoiceQuery(pageSize: InvoiceQuery.MaxPageSize + 1));
    }

    [Fact]
    public async Task SummaryReportsGlobalTotalsByStatusAndCustomer()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);
        var all = await db.Invoices.ListAsync(new InvoiceQuery(pageSize: 100), CancellationToken.None);

        var summary = await db.Invoices.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(6L, summary.TotalInvoices);
        Assert.Equal(2L, summary.CountByStatus["primerrecordatorio"]);
        Assert.Equal(2L, summary.CountByStatus["segundorecordatorio"]);
        Assert.Equal(1L, summary.CountByStatus["pendiente"]);
        Assert.Equal(1L, summary.CountByStatus["desactivado"]);
        Assert.Equal(2L, summary.CountByCustomer["cliente-a"]);
        Assert.Equal(2L, summary.CountByCustomer["cliente-b"]);
        Assert.Equal(2L, summary.CountByCustomer["cliente-c"]);
        Assert.Equal(all.Items.Sum(invoice => invoice.Amount), summary.TotalAmountCop);
    }

    [Fact]
    public async Task ClaimIsExclusiveUnderConcurrency()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);
        var invoiceId = new InvoiceId("FAC-001");
        var state = InvoiceState.PrimerRecordatorio;

        var results = await Task.WhenAll(
            db.Invoices.TryClaimAsync(invoiceId, state, Request("token-a", Now), CancellationToken.None),
            db.Invoices.TryClaimAsync(invoiceId, state, Request("token-b", Now), CancellationToken.None));

        Assert.Single(results, claim => claim is not null);
        var stored = await db.Invoices.FindByIdAsync(invoiceId, CancellationToken.None);
        Assert.Equal(state, stored!.State);
    }

    [Fact]
    public async Task ExpiredClaimedLockCanBeReclaimedButDispatchingCannot()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);
        var invoiceId = new InvoiceId("FAC-001");
        var state = InvoiceState.PrimerRecordatorio;

        var first = await db.Invoices.TryClaimAsync(
            invoiceId, state, Request("token-1", Now, Now.AddMinutes(1)), CancellationToken.None);
        var whileActive = await db.Invoices.TryClaimAsync(
            invoiceId, state, Request("token-2", Now), CancellationToken.None);
        var afterExpiry = await db.Invoices.TryClaimAsync(
            invoiceId, state, Request("token-3", Now.AddMinutes(2), Now.AddMinutes(3)), CancellationToken.None);

        Assert.NotNull(first);
        Assert.Null(whileActive);
        Assert.Equal("token-3", afterExpiry!.Token);

        var dispatching = await db.Invoices.TryBeginDispatchAsync(
            invoiceId, "token-3", Now.AddMinutes(4), CancellationToken.None);
        var afterDispatching = await db.Invoices.TryClaimAsync(
            invoiceId, state, Request("token-4", Now.AddMinutes(10)), CancellationToken.None);

        Assert.True(dispatching);
        Assert.Null(afterDispatching);
    }

    [Fact]
    public async Task CasRequiresMatchingIdStateAndToken()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);
        var invoiceId = new InvoiceId("FAC-001");
        var previous = InvoiceState.PrimerRecordatorio;
        var next = InvoiceState.SegundoRecordatorio;
        await db.Invoices.TryClaimAsync(invoiceId, previous, Request("cas-token", Now), CancellationToken.None);

        var wrongState = await db.Invoices.TryCompleteTransitionAsync(
            invoiceId, InvoiceState.SegundoRecordatorio, "cas-token", InvoiceState.Desactivado, Now, CancellationToken.None);
        var wrongToken = await db.Invoices.TryCompleteTransitionAsync(
            invoiceId, previous, "other-token", next, Now, CancellationToken.None);
        var completed = await db.Invoices.TryCompleteTransitionAsync(
            invoiceId, previous, "cas-token", next, Now.AddMinutes(1), CancellationToken.None);
        var replayed = await db.Invoices.TryCompleteTransitionAsync(
            invoiceId, previous, "cas-token", next, Now.AddMinutes(2), CancellationToken.None);

        Assert.False(wrongState);
        Assert.False(wrongToken);
        Assert.True(completed);
        Assert.False(replayed);

        var stored = await db.Invoices.FindByIdAsync(invoiceId, CancellationToken.None);
        Assert.Equal(next, stored!.State);
        Assert.Equal(Now.AddMinutes(1), stored.UpdatedAt);
        var raw = await db.Context.Database.GetCollection<BsonDocument>("facturas")
            .Find(Builders<BsonDocument>.Filter.Eq("_id", "FAC-001")).SingleAsync();
        Assert.False(raw.Contains("processing"));
    }

    [Fact]
    public async Task ReleaseClaimOnlyClearsMatchingToken()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);
        var invoiceId = new InvoiceId("FAC-001");
        var state = InvoiceState.PrimerRecordatorio;
        await db.Invoices.TryClaimAsync(invoiceId, state, Request("release-token", Now), CancellationToken.None);

        var wrong = await db.Invoices.ReleaseClaimAsync(invoiceId, "other-token", CancellationToken.None);
        var released = await db.Invoices.ReleaseClaimAsync(invoiceId, "release-token", CancellationToken.None);
        var reclaim = await db.Invoices.TryClaimAsync(invoiceId, state, Request("new-token", Now), CancellationToken.None);

        Assert.False(wrong);
        Assert.True(released);
        Assert.Equal("new-token", reclaim!.Token);
    }

    [Fact]
    public async Task NotificationAttemptRoundTripsAndUpdatesStatus()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);
        var attempt = new NotificationAttempt(
            "attempt-1",
            "batch-1",
            new InvoiceId("FAC-001"),
            new CustomerId("cliente-a"),
            InvoiceState.PrimerRecordatorio,
            InvoiceState.SegundoRecordatorio,
            NotificationAttemptStatus.Dispatching,
            Now,
            Details: "smtp accepted by mailpit");

        await db.Attempts.AddAsync(attempt, CancellationToken.None);
        var stored = await db.Attempts.FindAsync("attempt-1", CancellationToken.None);
        var updated = await db.Attempts.UpdateAsync(
            attempt with { Status = NotificationAttemptStatus.Completed, FinishedAt = Now.AddMinutes(1) },
            CancellationToken.None);
        var final = await db.Attempts.FindAsync("attempt-1", CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(NotificationAttemptStatus.Dispatching, stored.Status);
        Assert.True(updated);
        Assert.Equal(NotificationAttemptStatus.Completed, final!.Status);
        Assert.Equal(Now.AddMinutes(1), final.FinishedAt);
        Assert.Equal("smtp accepted by mailpit", final.Details);
    }

    [Fact]
    public async Task CustomersAreListedAndFoundById()
    {
        await using var db = MongoTestContext.Create(fixture);
        await db.Seeder.SeedAsync(reset: false, CancellationToken.None);

        var customers = await db.Customers.ListAsync(CancellationToken.None);
        var found = await db.Customers.FindByIdAsync(new CustomerId("cliente-b"), CancellationToken.None);

        Assert.Equal(3, customers.Count);
        Assert.All(customers, customer => Assert.EndsWith("@example.test", customer.Email.Value));
        Assert.NotNull(found);
        Assert.Equal("cliente-b", found.Id.Value);
    }

    private static InvoiceClaimRequest Request(string token, DateTimeOffset startedAt, DateTimeOffset? lockUntil = null) =>
        new(token, startedAt, lockUntil ?? startedAt.AddMinutes(1));
}

