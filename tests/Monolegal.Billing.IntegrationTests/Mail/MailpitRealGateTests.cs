using System.Net;
using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Options;
using Monolegal.Billing.Application;
using Monolegal.Billing.Application.Email;
using Monolegal.Billing.Application.Reminders;
using Monolegal.Billing.Domain.Invoices;
using Monolegal.Billing.Infrastructure.Mail;
using Monolegal.Billing.Infrastructure.Mongo;
using Monolegal.Billing.Infrastructure.Mongo.Repositories;
using Monolegal.Billing.Infrastructure.Seed;

namespace Monolegal.Billing.IntegrationTests.Mail;

[Collection("mailpit")]
public class MailpitRealGateTests
{
    private const string MailpitBaseUrl = "http://localhost:8025";
    private const string GateDatabase = "monolegal_billing_gate";

    [Fact]
    public async Task RealBatchProducesFourMessagesAndFourTransitionsAgainstMailpitAndMongo()
    {
        Assert.True(await MailpitReachableAsync(), "Mailpit UI/API is not reachable on http://localhost:8025; real gate requires docker compose.");

        var connectionString = "mongodb://127.0.0.1:27017";
        var options = Options.Create(new MongoOptions
        {
            ConnectionString = connectionString,
            DatabaseName = GateDatabase,
        });
        var context = new MongoContext(options);

        var initializer = new MongoIndexInitializer(context);
        await initializer.EnsureAsync(CancellationToken.None);

        var seeder = new DevelopmentDataSeeder(context, initializer);
        await seeder.SeedAsync(reset: true, CancellationToken.None);

        var customers = new MongoCustomerRepository(context);
        var invoices = new MongoInvoiceRepository(context);
        var audit = new MongoNotificationAuditRepository(context);

        var emailSender = new MailKitEmailSender(Options.Create(new MailOptions
        {
            Host = "localhost",
            Port = 1025,
            From = "no-reply@example.test",
            FromName = "Monolegal Billing Demo",
            EnableTls = false,
            User = string.Empty,
            Password = string.Empty,
            TimeoutMilliseconds = 5000,
        }));

        var sut = new ProcessInvoiceReminders(
            invoices,
            customers,
            audit,
            emailSender,
            new FixedTimeSource(),
            new SequentialReminderIdGenerator());

        await ClearMailpitMessagesAsync();

        var result = await sut.ExecuteAsync(CancellationToken.None);

        var messages = await PollMailpitMessageCountAsync();
        Assert.Equal(4, messages);
        Assert.Equal(4, result.TotalCandidates);
        Assert.Equal(4, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Skipped);
        Assert.Equal(0, result.NeedsReview);

        var states = await InvoiceStatesAsync(context);
        Assert.Equal(InvoiceState.SegundoRecordatorio.Persisted, states["FAC-001"]);
        Assert.Equal(InvoiceState.Desactivado.Persisted, states["FAC-002"]);
        Assert.Equal(InvoiceState.SegundoRecordatorio.Persisted, states["FAC-003"]);
        Assert.Equal(InvoiceState.Desactivado.Persisted, states["FAC-005"]);
        Assert.Equal(InvoiceState.Pendiente.Persisted, states["FAC-004"]);
        Assert.Equal(InvoiceState.Desactivado.Persisted, states["FAC-006"]);

        var attempts = await AttemptStatusesAsync(context);
        Assert.All(attempts.Values, status => Assert.Equal("Completed", status));
    }

    private static async Task<bool> MailpitReachableAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var response = await client.GetAsync($"{MailpitBaseUrl}/api/v1/messages");
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch
        {
            return false;
        }
    }

    private static async Task ClearMailpitMessagesAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var response = await client.DeleteAsync($"{MailpitBaseUrl}/api/v1/messages");
        response.EnsureSuccessStatusCode();
    }

    private static async Task<int> PollMailpitMessageCountAsync()
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (DateTimeOffset.UtcNow < deadline)
        {
            using var response = await client.GetAsync($"{MailpitBaseUrl}/api/v1/messages");
            response.EnsureSuccessStatusCode();
            using var stream = await response.Content.ReadAsStreamAsync();
            var document = await JsonDocument.ParseAsync(stream);
            var total = document.RootElement.GetProperty("total").GetInt32();
            if (total >= 4)
            {
                return total;
            }
            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        return await GetMailpitMessageCountAsync(client);
    }

    private static async Task<int> GetMailpitMessageCountAsync(HttpClient client)
    {
        using var response = await client.GetAsync($"{MailpitBaseUrl}/api/v1/messages");
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync();
        var document = await JsonDocument.ParseAsync(stream);
        return document.RootElement.GetProperty("total").GetInt32();
    }

    private static async Task<Dictionary<string, string>> InvoiceStatesAsync(MongoContext context)
    {
        var states = new Dictionary<string, string>(StringComparer.Ordinal);
        var collection = context.Database.GetCollection<BsonDocument>("facturas");
        var cursor = await collection
            .Find(Builders<BsonDocument>.Filter.Empty)
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .ToListAsync(CancellationToken.None);
        foreach (var document in cursor)
        {
            states[document["_id"].AsString] = document["estado"].AsString;
        }
        return states;
    }

    private static async Task<Dictionary<string, string>> AttemptStatusesAsync(MongoContext context)
    {
        var statuses = new Dictionary<string, string>(StringComparer.Ordinal);
        var collection = context.Database.GetCollection<BsonDocument>("intentosNotificacion");
        var cursor = await collection
            .Find(Builders<BsonDocument>.Filter.Empty)
            .ToListAsync(CancellationToken.None);
        foreach (var document in cursor)
        {
            statuses[document["_id"].AsString] = document["status"].AsString;
        }
        return statuses;
    }

    private sealed class FixedTimeSource : ITimeSource
    {
        public DateTimeOffset UtcNow => new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    }

    private sealed class SequentialReminderIdGenerator : IReminderIdGenerator
    {
        private int _batch;
        private int _attempt;
        private int _claim;

        public string NewBatchId() => $"batch-{++_batch}";
        public string NewAttemptId() => $"attempt-{++_attempt}";
        public string NewClaimToken() => $"claim-{++_claim}";
    }
}
