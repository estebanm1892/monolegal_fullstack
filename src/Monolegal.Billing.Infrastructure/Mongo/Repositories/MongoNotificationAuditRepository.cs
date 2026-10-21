using MongoDB.Driver;
using Monolegal.Billing.Application.Notifications;
using Monolegal.Billing.Domain.Invoices;
using Monolegal.Billing.Infrastructure.Mongo.Documents;

namespace Monolegal.Billing.Infrastructure.Mongo.Repositories;

public sealed class MongoNotificationAuditRepository(MongoContext context) : INotificationAuditRepository
{
    public Task AddAsync(NotificationAttempt attempt, CancellationToken cancellationToken) =>
        context.Attempts.InsertOneAsync(ToDocument(attempt), cancellationToken: cancellationToken);

    public async Task<bool> UpdateAsync(NotificationAttempt attempt, CancellationToken cancellationToken)
    {
        var result = await context.Attempts.ReplaceOneAsync(
            Builders<NotificationAttemptDocument>.Filter.Eq(document => document.Id, attempt.AttemptId),
            ToDocument(attempt),
            cancellationToken: cancellationToken);

        return result.MatchedCount == 1;
    }

    public async Task<NotificationAttempt?> FindAsync(string attemptId, CancellationToken cancellationToken)
    {
        var document = await context.Attempts
            .Find(Builders<NotificationAttemptDocument>.Filter.Eq(document => document.Id, attemptId))
            .FirstOrDefaultAsync(cancellationToken);

        return document is null ? null : ToDomain(document);
    }

    public async Task<IReadOnlyList<NotificationAttempt>> FindByBatchIdAsync(
        string batchId,
        CancellationToken cancellationToken)
    {
        var documents = await context.Attempts
            .Find(Builders<NotificationAttemptDocument>.Filter.Eq(document => document.BatchId, batchId))
            .Sort(Builders<NotificationAttemptDocument>.Sort.Ascending(document => document.StartedAt))
            .ToListAsync(cancellationToken);

        return documents.Select(ToDomain).ToArray();
    }

    private static NotificationAttemptDocument ToDocument(NotificationAttempt attempt) =>
        new()
        {
            Id = attempt.AttemptId,
            BatchId = attempt.BatchId,
            InvoiceId = attempt.InvoiceId.Value,
            CustomerId = attempt.CustomerId.Value,
            PreviousState = attempt.PreviousState.Persisted,
            NextState = attempt.NextState.Persisted,
            Status = attempt.Status,
            StartedAt = MongoMapping.ToUtc(attempt.StartedAt),
            DispatchStartedAt = attempt.DispatchStartedAt is { } dispatchStartedAt
                ? MongoMapping.ToUtc(dispatchStartedAt)
                : null,
            SmtpAcceptedAt = attempt.SmtpAcceptedAt is { } smtpAcceptedAt
                ? MongoMapping.ToUtc(smtpAcceptedAt)
                : null,
            FinishedAt = attempt.FinishedAt is { } finishedAt
                ? MongoMapping.ToUtc(finishedAt)
                : null,
            MessageId = attempt.MessageId,
            FailureCode = attempt.FailureCode,
            Details = attempt.Details,
        };

    private static NotificationAttempt ToDomain(NotificationAttemptDocument document) =>
        new(
            document.Id,
            document.BatchId,
            new InvoiceId(document.InvoiceId),
            new Domain.Customers.CustomerId(document.CustomerId),
            InvoiceState.FromPersisted(document.PreviousState),
            InvoiceState.FromPersisted(document.NextState),
            document.Status,
            MongoMapping.ToOffset(document.StartedAt),
            document.DispatchStartedAt is { } dispatchStartedAt ? MongoMapping.ToOffset(dispatchStartedAt) : null,
            document.SmtpAcceptedAt is { } smtpAcceptedAt ? MongoMapping.ToOffset(smtpAcceptedAt) : null,
            document.FinishedAt is { } finishedAt ? MongoMapping.ToOffset(finishedAt) : null,
            document.MessageId,
            document.FailureCode,
            document.Details);
}
