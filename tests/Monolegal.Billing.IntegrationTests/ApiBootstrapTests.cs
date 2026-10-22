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

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
    }
}
