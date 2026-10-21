namespace Monolegal.Billing.Application;

// Composition example; business time-dependent rules will use this port in later phases.
public interface ITimeSource
{
    DateTimeOffset UtcNow { get; }
}
