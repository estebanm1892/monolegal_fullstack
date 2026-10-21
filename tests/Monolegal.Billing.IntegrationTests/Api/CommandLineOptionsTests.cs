using Monolegal.Billing.Api;
using Xunit;

namespace Monolegal.Billing.IntegrationTests.Api;

public class CommandLineOptionsTests
{
    [Fact]
    public void ResetWithoutSeedIsInvalidAndDoesNotReachTheHost()
    {
        var options = CommandLineOptions.Parse(["--reset"]);

        Assert.False(options.IsValid);
        Assert.Contains("--seed", options.ValidationError);
        Assert.Empty(options.HostArguments);
    }

    [Fact]
    public void SeedAndResetAreConsumedAndOtherArgumentsReachTheHost()
    {
        var options = CommandLineOptions.Parse(["--seed", "--reset", "--urls", "http://127.0.0.1:5091"]);

        Assert.True(options.IsValid);
        Assert.True(options.Seed);
        Assert.True(options.Reset);
        Assert.Equal(["--urls", "http://127.0.0.1:5091"], options.HostArguments);
    }

    [Fact]
    public void NoFlagsIsAValidOrdinaryStartup()
    {
        var options = CommandLineOptions.Parse([]);

        Assert.True(options.IsValid);
        Assert.False(options.Seed);
        Assert.False(options.Reset);
        Assert.Empty(options.HostArguments);
    }

    [Fact]
    public void FlagsAreCaseInsensitive()
    {
        var options = CommandLineOptions.Parse(["--SEED", "--Reset"]);

        Assert.True(options.IsValid);
        Assert.True(options.Seed);
        Assert.True(options.Reset);
    }
}
