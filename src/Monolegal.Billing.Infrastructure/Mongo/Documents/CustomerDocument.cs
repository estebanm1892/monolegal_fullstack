using MongoDB.Bson.Serialization.Attributes;

namespace Monolegal.Billing.Infrastructure.Mongo.Documents;

internal sealed class CustomerDocument
{
    [BsonId]
    public string Id { get; set; } = string.Empty;

    [BsonElement("nombre")]
    public string Nombre { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    [BsonElement("activo")]
    public bool Activo { get; set; }
}
