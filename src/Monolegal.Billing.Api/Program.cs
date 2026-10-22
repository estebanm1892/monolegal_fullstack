using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Monolegal.Billing.Api;
using Monolegal.Billing.Api.Endpoints;
using Monolegal.Billing.Application;
using Monolegal.Billing.Infrastructure;
using Monolegal.Billing.Infrastructure.Mail;
using Monolegal.Billing.Infrastructure.Seed;

var options = CommandLineOptions.Parse(args);

if (!options.IsValid)
{
    Console.Error.WriteLine(options.ValidationError);
    Environment.ExitCode = 1;
    return;
}

var builder = WebApplication.CreateBuilder(options.HostArguments);

const string LocalFrontendCorsPolicy = "LocalFrontend";

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITimeSource, SystemTimeSource>();
builder.Services.AddMonolegalInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddCors(cors => cors.AddPolicy(
    LocalFrontendCorsPolicy,
    policy => policy
        .WithOrigins("http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var app = builder.Build();

// Seed mode does not start the host, so force startup option validation before any data write.
_ = app.Services.GetRequiredService<IOptions<MailOptions>>().Value;

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.UseCors(LocalFrontendCorsPolicy);
    app.MapOpenApi();
}

app.MapCustomerEndpoints();
app.MapInvoiceEndpoints();
app.MapReminderEndpoints();
app.MapHealthEndpoint();

if (options.Seed)
{
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("The development seed can only run in the Development environment.");
        Environment.ExitCode = 1;
        return;
    }

    if (options.Reset)
    {
        Console.WriteLine("Reset requested: only the documented seed ids are deleted before seeding.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IDevelopmentDataSeeder>();
    var report = await seeder.SeedAsync(options.Reset, CancellationToken.None);

    Console.WriteLine(
        $"Seed completed: customersInserted={report.CustomersInserted}, invoicesInserted={report.InvoicesInserted}, customerCount={report.CustomerCount}, invoiceCount={report.InvoiceCount}");
    return;
}

app.Run();

public partial class Program { }
