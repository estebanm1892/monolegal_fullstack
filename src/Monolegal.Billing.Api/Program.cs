using Monolegal.Billing.Application;
using Monolegal.Billing.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ITimeSource, SystemTimeSource>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// No notification route is registered until the guarded workflow is implemented.
app.Run();

public partial class Program { }
