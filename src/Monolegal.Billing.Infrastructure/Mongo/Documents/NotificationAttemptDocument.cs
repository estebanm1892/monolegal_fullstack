using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Monolegal.Billing.Application.Notifications;

namespace Monolegal.Billing.Infrastructure.Mongo.Documents;

internal sealed class NotificationAttemptDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("batchId")]
    public string BatchId { get; set; } = string.Empty;

    [BsonElement("invoiceId")]
    public string InvoiceId { get; set; } = string.Empty;

    [BsonElement("customerId")]
    public string CustomerId { get; set; } = string.Empty;

    [BsonElement("previousState")]
    public string PreviousState { get; set; } = string.Empty;

    [BsonElement("nextState")]
    public string NextState { get; set; } = string.Empty;

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public NotificationAttemptStatus Status { get; set; }

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; }

    [BsonElement("dispatchStartedAt")]
    public DateTime? DispatchStartedAt { get; set; }

    [BsonElement("smtpAcceptedAt")]
    public DateTime? SmtpAcceptedAt { get; set; }

    [BsonElement("finishedAt")]
    public DateTime? FinishedAt { get; set; }

    [BsonElement("messageId")]
    public string? MessageId { get; set; }

    [BsonElement("failureCode")]
    public string? FailureCode { get; set; }

    [BsonElement("details")]
    public string? Details { get; set; }
}
