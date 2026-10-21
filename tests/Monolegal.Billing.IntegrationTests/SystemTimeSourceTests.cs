using Monolegal.Billing.Application;
using Monolegal.Billing.Infrastructure;

namespace Monolegal.Billing.IntegrationTests;

public class SystemTimeSourceTests
{
    [Fact]
    public void ProvidesTimeThroughApplicationPort()
    {
        var fixedTime = new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);
        ITimeSource clock = new SystemTimeSource(new FixedTimeProvider(fixedTime));

        Assert.Equal(fixedTime, clock.UtcNow);
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
