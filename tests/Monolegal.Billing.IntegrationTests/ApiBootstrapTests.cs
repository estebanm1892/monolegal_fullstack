using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Monolegal.Billing.Application;
using Monolegal.Billing.Infrastructure;

namespace Monolegal.Billing.IntegrationTests;

public class ApiBootstrapTests
{
    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task OpenApiIsOnlyAvailableInDevelopment(string environment, HttpStatusCode expected)
    {
        await using var factory = CreateFactory(environment);
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(expected, response.StatusCode);
        if (response.IsSuccessStatusCode)
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            Assert.True(document.RootElement.TryGetProperty("openapi", out _));
            Assert.True(document.RootElement.GetProperty("paths").EnumerateObject().Any());
        }
    }

    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.NotFound)]
    public async Task SwaggerUiIsOnlyAvailableInDevelopment(string environment, HttpStatusCode expected)
    {
        await using var factory = CreateFactory(environment);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        using var response = await client.GetAsync("/swagger/index.html");

        Assert.Equal(expected, response.StatusCode);
        if (response.IsSuccessStatusCode)
        {
            var html = await response.Content.ReadAsStringAsync();
            Assert.Contains("Monolegal Billing API", html, StringComparison.Ordinal);

            using var initializerResponse = await client.GetAsync("/swagger/index.js");
            initializerResponse.EnsureSuccessStatusCode();
            var initializer = await initializerResponse.Content.ReadAsStringAsync();
            Assert.Contains("/openapi/v1.json", initializer, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task OpenApiDocumentsSpanishOperationsParametersResponsesAndExamples()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/openapi/v1.json");
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal("Monolegal Billing API", root.GetProperty("info").GetProperty("title").GetString());

        AssertOperation(root, "/api/customers", "get", "Clientes", "Consultar clientes");
        AssertOperation(
            root,
            "/api/invoices/summary",
            "get",
            "Facturas",
            "Consultar estadísticas generales");
        AssertOperation(
            root,
            "/api/reminders/runs/{batchId}",
            "get",
            "Recordatorios",
            "Consultar auditoría de una ejecución");
        AssertOperation(
            root,
            "/health",
            "get",
            "Salud del servicio",
            "Consultar el estado del servicio");

        var invoiceOperation = root.GetProperty("paths")
            .GetProperty("/api/invoices")
            .GetProperty("get");
        Assert.Equal("Facturas", invoiceOperation.GetProperty("tags")[0].GetString());
        Assert.Equal("Consultar facturas", invoiceOperation.GetProperty("summary").GetString());
        var pageParameter = invoiceOperation.GetProperty("parameters")
            .EnumerateArray()
            .Single(parameter => parameter.GetProperty("name").GetString() == "page");
        Assert.Contains("Página", pageParameter.GetProperty("description").GetString());
        Assert.True(invoiceOperation.GetProperty("responses").TryGetProperty("400", out _));
        Assert.True(ResponseHasExample(invoiceOperation, "200"));
        Assert.Equal(
            "Correcto",
            invoiceOperation.GetProperty("responses").GetProperty("200").GetProperty("description").GetString());
        Assert.Equal(
            "Solicitud incorrecta",
            invoiceOperation.GetProperty("responses").GetProperty("400").GetProperty("description").GetString());
        var invoiceExample = invoiceOperation.GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example");
        Assert.Equal(
            "2026-10-01T00:00:00+00:00",
            invoiceExample.GetProperty("items")[0].GetProperty("updatedAt").GetString());

        var processOperation = root.GetProperty("paths")
            .GetProperty("/api/reminders/process")
            .GetProperty("post");
        Assert.Equal("Recordatorios", processOperation.GetProperty("tags")[0].GetString());
        Assert.Contains("todas las facturas elegibles", processOperation.GetProperty("description").GetString());
        Assert.Contains("ADVERTENCIA", processOperation.GetProperty("description").GetString());
        Assert.True(processOperation.GetProperty("responses").TryGetProperty("403", out _));
        Assert.True(ResponseHasExample(processOperation, "200"));
        var processExample = processOperation.GetProperty("responses")
            .GetProperty("200")
            .GetProperty("content")
            .GetProperty("application/json")
            .GetProperty("example");
        Assert.Matches(
            "^batch-[0-9a-f]{32}$",
            processExample.GetProperty("batchId").GetString()!);

        var runOperation = root.GetProperty("paths")
            .GetProperty("/api/reminders/runs/{batchId}")
            .GetProperty("get");
        Assert.True(runOperation.GetProperty("responses").TryGetProperty("404", out _));
        Assert.True(ResponseHasExample(runOperation, "200"));
        Assert.True(ResponseHasExample(runOperation, "404"));

        var healthOperation = root.GetProperty("paths").GetProperty("/health").GetProperty("get");
        Assert.Equal("Salud del servicio", healthOperation.GetProperty("tags")[0].GetString());
        Assert.Contains("proceso de la API", healthOperation.GetProperty("description").GetString());
    }

    [Theory]
    [InlineData("Development", HttpStatusCode.OK)]
    [InlineData("Production", HttpStatusCode.Forbidden)]
    public async Task BootstrapReminderProcessingIsDevelopmentOnly(string environment, HttpStatusCode expected)
    {
        await using var factory = CreateFactory(environment);
        using var client = factory.CreateClient();

        using var response = await client.PostAsync("/api/reminders/process", null);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task CompositionResolvesApplicationTimePortWithRegisteredProvider()
    {
        var provider = new FixedTimeProvider();
        await using var factory = CreateFactory("Development").WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.AddSingleton<TimeProvider>(provider)));

        var clock = factory.Services.GetRequiredService<ITimeSource>();

        Assert.IsType<SystemTimeSource>(clock);
        Assert.Equal(provider.GetUtcNow(), clock.UtcNow);
        Assert.Same(clock, factory.Services.GetRequiredService<ITimeSource>());
    }

    [Fact]
    public async Task CorsPreflightAllowsTheLocalAngularOrigin()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/reminders/process");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out var origins));
        Assert.Equal("http://localhost:4200", Assert.Single(origins));
    }

    [Fact]
    public async Task CorsPreflightDoesNotAllowUnknownOrigins()
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/reminders/process");
        request.Headers.Add("Origin", "http://not-the-local-frontend.test");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task CorsIsNotEnabledOutsideDevelopment()
    {
        await using var factory = CreateFactory("Production");
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/reminders/process");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Theory]
    [InlineData("page=0&pageSize=10")]
    [InlineData("page=1&pageSize=0")]
    [InlineData("page=1&pageSize=101")]
    public async Task InvalidPaginationReturnsProblemDetailsBadRequest(string query)
    {
        await using var factory = CreateFactory("Development");
        using var client = factory.CreateClient();

        using var response = await client.GetAsync($"/api/invoices?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static WebApplicationFactory<Program> CreateFactory(string environment) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment(environment));

    private static bool ResponseHasExample(JsonElement operation, string statusCode) =>
        operation.GetProperty("responses")
            .GetProperty(statusCode)
            .GetProperty("content")
            .EnumerateObject()
            .Any(content => content.Value.TryGetProperty("example", out _));

    private static void AssertOperation(
        JsonElement root,
        string path,
        string method,
        string tag,
        string summary)
    {
        var operation = root.GetProperty("paths").GetProperty(path).GetProperty(method);
        Assert.Equal(tag, operation.GetProperty("tags")[0].GetString());
        Assert.Equal(summary, operation.GetProperty("summary").GetString());
        Assert.True(ResponseHasExample(operation, "200"));
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    }
}
