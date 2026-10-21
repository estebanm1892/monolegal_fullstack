namespace Monolegal.Billing.Infrastructure.Mongo;

public sealed class MongoOptions
{
    public const string SectionName = "Mongo";

    public string ConnectionString { get; set; } = string.Empty;

    public string DatabaseName { get; set; } = "monolegal_billing";
}
