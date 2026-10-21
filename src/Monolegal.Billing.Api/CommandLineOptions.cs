namespace Monolegal.Billing.Api;

public sealed record CommandLineOptions(bool Seed, bool Reset, string[] HostArguments)
{
    private const string SeedFlag = "--seed";
    private const string ResetFlag = "--reset";

    public bool IsValid => !Reset || Seed;

    public string? ValidationError => IsValid
        ? null
        : "--reset requires --seed. Use --seed --reset in Development; the API was not started.";

    public static CommandLineOptions Parse(string[] args)
    {
        var seed = args.Any(argument => IsFlag(argument, SeedFlag));
        var reset = args.Any(argument => IsFlag(argument, ResetFlag));
        var hostArguments = args
            .Where(argument => !IsFlag(argument, SeedFlag) && !IsFlag(argument, ResetFlag))
            .ToArray();

        return new CommandLineOptions(seed, reset, hostArguments);
    }

    private static bool IsFlag(string argument, string flag) =>
        string.Equals(argument, flag, StringComparison.OrdinalIgnoreCase);
}
