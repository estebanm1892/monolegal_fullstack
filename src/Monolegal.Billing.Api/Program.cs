using Monolegal.Billing.Application;
using Monolegal.Billing.Infrastructure;
using Monolegal.Billing.Infrastructure.Seed;

var seedRequested = args.Any(argument => string.Equals(argument, "--seed", StringComparison.OrdinalIgnoreCase));
var resetRequested = args.Any(argument => string.Equals(argument, "--reset", StringComparison.OrdinalIgnoreCase));
var hostArguments = args
    .Where(argument => !string.Equals(argument, "--seed", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(argument, "--reset", StringComparison.OrdinalIgnoreCase))
    .ToArray();

var builder = WebApplication.CreateBuilder(hostArguments);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITimeSource, SystemTimeSource>();
builder.Services.AddMonolegalInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (seedRequested)
{
    if (!app.Environment.IsDevelopment())
    {
        Console.Error.WriteLine("The development seed can only run in the Development environment.");
        Environment.ExitCode = 1;
        return;
    }

    if (resetRequested)
    {
        Console.WriteLine("Reset requested: only the documented seed ids are deleted before seeding.");
    }

    await using var scope = app.Services.CreateAsyncScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IDevelopmentDataSeeder>();
    var report = await seeder.SeedAsync(resetRequested, CancellationToken.None);

    Console.WriteLine(
        $"Seed completed: customersInserted={report.CustomersInserted}, invoicesInserted={report.InvoicesInserted}, customerCount={report.CustomerCount}, invoiceCount={report.InvoiceCount}");
    return;
}

// No notification route is registered until the guarded workflow is implemented.
app.Run();

public partial class Program { }
