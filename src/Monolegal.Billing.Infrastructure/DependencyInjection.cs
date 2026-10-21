using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Monolegal.Billing.Application.Customers;
using Monolegal.Billing.Application.Invoices;
using Monolegal.Billing.Application.Notifications;
using Monolegal.Billing.Infrastructure.Mongo;
using Monolegal.Billing.Infrastructure.Mongo.Repositories;
using Monolegal.Billing.Infrastructure.Seed;

namespace Monolegal.Billing.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMonolegalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .Configure(options =>
            {
                var connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");
                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    options.ConnectionString = connectionString;
                }

                var databaseName = Environment.GetEnvironmentVariable("MONGO_DATABASE");
                if (!string.IsNullOrWhiteSpace(databaseName))
                {
                    options.DatabaseName = databaseName;
                }
            });

        services.AddSingleton<MongoContext>();
        services.AddSingleton<ICustomerRepository, MongoCustomerRepository>();
        services.AddSingleton<IInvoiceRepository, MongoInvoiceRepository>();
        services.AddSingleton<INotificationAuditRepository, MongoNotificationAuditRepository>();
        services.AddSingleton<IMongoIndexInitializer, MongoIndexInitializer>();
        services.AddSingleton<IDevelopmentDataSeeder, DevelopmentDataSeeder>();

        return services;
    }
}
