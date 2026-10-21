using Monolegal.Billing.Application;

namespace Monolegal.Billing.Infrastructure;

public sealed class SystemTimeSource(TimeProvider timeProvider) : ITimeSource
{
    public DateTimeOffset UtcNow => timeProvider.GetUtcNow();
}
