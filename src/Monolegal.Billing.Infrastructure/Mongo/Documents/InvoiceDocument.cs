using MongoDB.Bson.Serialization.Attributes;

namespace Monolegal.Billing.Infrastructure.Mongo.Documents;

internal sealed class InvoiceDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("clienteId")]
    public string ClienteId { get; set; } = string.Empty;

    [BsonElement("numero")]
    public string Numero { get; set; } = string.Empty;

    [BsonElement("fechaEmision")]
    public DateTime FechaEmision { get; set; }

    [BsonElement("fechaVencimiento")]
    public DateTime FechaVencimiento { get; set; }

    [BsonElement("actualizadoEn")]
    public DateTime ActualizadoEn { get; set; }

    [BsonElement("valor")]
    public decimal Valor { get; set; }

    [BsonElement("moneda")]
    public string Moneda { get; set; } = "COP";

    [BsonElement("estado")]
    public string Estado { get; set; } = string.Empty;

    [BsonElement("processing")]
    public ProcessingDocument? Processing { get; set; }
}

internal sealed class ProcessingDocument
{
    [BsonElement("token")]
    public string Token { get; set; } = string.Empty;

    [BsonElement("phase")]
    public string Phase { get; set; } = string.Empty;

    [BsonElement("lockUntil")]
    public DateTime LockUntil { get; set; }

    [BsonElement("startedAt")]
    public DateTime StartedAt { get; set; }
}
