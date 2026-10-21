using MongoDB.Bson;
using MongoDB.Driver;
using Monolegal.Billing.Application.Invoices;
using Monolegal.Billing.Domain.Invoices;
using Monolegal.Billing.Infrastructure.Mongo.Documents;

namespace Monolegal.Billing.Infrastructure.Mongo.Repositories;

public sealed class MongoInvoiceRepository(MongoContext context) : IInvoiceRepository
{
    private IMongoCollection<InvoiceDocument> Invoices => context.Invoices;

    public async Task<Invoice?> FindByIdAsync(InvoiceId id, CancellationToken cancellationToken)
    {
        var document = await Invoices
            .Find(Builders<InvoiceDocument>.Filter.Eq(document => document.Id, id.Value))
            .FirstOrDefaultAsync(cancellationToken);

        return document is null ? null : MongoMapping.ToDomain(document);
    }

    public async Task<PagedResult<Invoice>> ListAsync(InvoiceQuery query, CancellationToken cancellationToken)
    {
        var filter = Builders<InvoiceDocument>.Filter.Empty;

        if (query.CustomerId is not null)
        {
            filter &= Builders<InvoiceDocument>.Filter.Eq(document => document.ClienteId, query.CustomerId);
        }

        if (query.Status is not null)
        {
            filter &= Builders<InvoiceDocument>.Filter.Eq(document => document.Estado, query.Status);
        }

        var totalCount = await Invoices.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var documents = await Invoices
            .Find(filter)
            .Sort(Builders<InvoiceDocument>.Sort
                .Ascending(document => document.FechaVencimiento)
                .Ascending(document => document.Id))
            .Skip((query.Page - 1) * query.PageSize)
            .Limit(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Invoice>(
            documents.Select(MongoMapping.ToDomain).ToArray(),
            query.Page,
            query.PageSize,
            totalCount);
    }

    public async Task<InvoiceSummary> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var byStatus = await GroupCountAsync("$estado", cancellationToken);
        var byCustomer = await GroupCountAsync("$clienteId", cancellationToken);

        var totals = await Invoices
            .Aggregate()
            .AppendStage<BsonDocument>(new BsonDocument("$group", new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "count", new BsonDocument("$sum", 1) },
                { "amount", new BsonDocument("$sum", "$valor") },
            }))
            .ToListAsync(cancellationToken);

        var totalInvoices = 0L;
        var totalAmount = 0m;

        if (totals.Count > 0)
        {
            totalInvoices = totals[0]["count"].ToInt64();
            if (totals[0].TryGetValue("amount", out var amount) && amount is BsonDecimal128 decimal128)
            {
                totalAmount = decimal128.ToDecimal();
            }
        }

        return new InvoiceSummary(totalInvoices, byStatus, byCustomer, totalAmount);
    }

    public async Task<IReadOnlyList<InvoiceId>> FindEligibleIdsAsync(CancellationToken cancellationToken)
    {
        var eligibleStates = new[]
        {
            InvoiceState.PrimerRecordatorio.Persisted,
            InvoiceState.SegundoRecordatorio.Persisted,
        };

        var ids = await Invoices
            .Find(Builders<InvoiceDocument>.Filter.In(document => document.Estado, eligibleStates))
            .Sort(Builders<InvoiceDocument>.Sort.Ascending(document => document.Id))
            .Project(document => document.Id)
            .ToListAsync(cancellationToken);

        return ids.Select(id => new InvoiceId(id)).ToArray();
    }

    public async Task<InvoiceClaim?> TryClaimAsync(
        InvoiceId id,
        InvoiceState expectedState,
        InvoiceClaimRequest request,
        CancellationToken cancellationToken)
    {
        var startedAt = MongoMapping.ToUtc(request.StartedAt);
        var lockUntil = MongoMapping.ToUtc(request.LockUntil);

        var filter = Builders<InvoiceDocument>.Filter.Eq(document => document.Id, id.Value)
            & Builders<InvoiceDocument>.Filter.Eq(document => document.Estado, expectedState.Persisted)
            & Builders<InvoiceDocument>.Filter.Or(
                Builders<InvoiceDocument>.Filter.Eq(document => document.Processing, null),
                Builders<InvoiceDocument>.Filter.And(
                    Builders<InvoiceDocument>.Filter.Eq(
                        document => document.Processing!.Phase,
                        ClaimPhases.Claimed),
                    Builders<InvoiceDocument>.Filter.Lt(
                        document => document.Processing!.LockUntil,
                        startedAt)));

        var update = Builders<InvoiceDocument>.Update.Set(document => document.Processing, new ProcessingDocument
        {
            Token = request.Token,
            Phase = ClaimPhases.Claimed,
            StartedAt = startedAt,
            LockUntil = lockUntil,
        });

        var document = await Invoices.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<InvoiceDocument> { ReturnDocument = ReturnDocument.After },
            cancellationToken);

        if (document?.Processing is null)
        {
            return null;
        }

        return new InvoiceClaim(
            document.Processing.Token,
            document.Processing.Phase,
            MongoMapping.ToOffset(document.Processing.StartedAt),
            MongoMapping.ToOffset(document.Processing.LockUntil));
    }

    public async Task<bool> TryBeginDispatchAsync(
        InvoiceId id,
        string token,
        DateTimeOffset lockUntil,
        CancellationToken cancellationToken)
    {
        var filter = Builders<InvoiceDocument>.Filter.Eq(document => document.Id, id.Value)
            & Builders<InvoiceDocument>.Filter.Eq(document => document.Processing!.Token, token)
            & Builders<InvoiceDocument>.Filter.Eq(document => document.Processing!.Phase, ClaimPhases.Claimed);

        var update = Builders<InvoiceDocument>.Update
            .Set(document => document.Processing!.Phase, ClaimPhases.Dispatching)
            .Set(document => document.Processing!.LockUntil, MongoMapping.ToUtc(lockUntil));

        var result = await Invoices.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> TryCompleteTransitionAsync(
        InvoiceId id,
        InvoiceState expectedState,
        string token,
        InvoiceState nextState,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        var filter = Builders<InvoiceDocument>.Filter.Eq(document => document.Id, id.Value)
            & Builders<InvoiceDocument>.Filter.Eq(document => document.Estado, expectedState.Persisted)
            & Builders<InvoiceDocument>.Filter.Eq(document => document.Processing!.Token, token)
            & Builders<InvoiceDocument>.Filter.Eq(document => document.Processing!.Phase, ClaimPhases.Dispatching);

        var update = Builders<InvoiceDocument>.Update
            .Set(document => document.Estado, nextState.Persisted)
            .Set(document => document.ActualizadoEn, MongoMapping.ToUtc(updatedAt))
            .Unset(document => document.Processing);

        var result = await Invoices.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    public async Task<bool> ReleaseClaimAsync(InvoiceId id, string token, CancellationToken cancellationToken)
    {
        var filter = Builders<InvoiceDocument>.Filter.Eq(document => document.Id, id.Value)
            & Builders<InvoiceDocument>.Filter.Eq(document => document.Processing!.Token, token);

        var update = Builders<InvoiceDocument>.Update.Unset(document => document.Processing);
        var result = await Invoices.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount == 1;
    }

    private async Task<IReadOnlyDictionary<string, long>> GroupCountAsync(
        string field,
        CancellationToken cancellationToken)
    {
        var groups = await Invoices
            .Aggregate()
            .AppendStage<BsonDocument>(new BsonDocument("$group", new BsonDocument
            {
                { "_id", field },
                { "count", new BsonDocument("$sum", 1) },
            }))
            .ToListAsync(cancellationToken);

        var counts = new Dictionary<string, long>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            counts[group["_id"].AsString] = group["count"].ToInt64();
        }

        return counts
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }
}
