using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace Monolegal.Billing.Infrastructure.Mail;

public sealed class MailOptionsValidator : IValidateOptions<MailOptions>
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "localhost",
        "127.0.0.1",
        "::1",
    };

    public ValidateOptionsResult Validate(string? name, MailOptions options)
    {
        var failures = new List<string>();

        if (!AllowedHosts.Contains(options.Host))
        {
            failures.Add("Mail:Host must be an explicit loopback host for local Mailpit.");
        }

        if (options.Port is < 1 or > 65535)
        {
            failures.Add("Mail:Port must be between 1 and 65535.");
        }

        if (options.EnableTls)
        {
            failures.Add("Mail:EnableTls must be false for the local Mailpit demo.");
        }

        if (!string.IsNullOrWhiteSpace(options.User) || !string.IsNullOrWhiteSpace(options.Password))
        {
            failures.Add("SMTP credentials are not allowed for the local Mailpit demo.");
        }

        if (!IsTestAddress(options.From))
        {
            failures.Add("Mail:From must be a valid example.test address.");
        }

        if (string.IsNullOrWhiteSpace(options.FromName))
        {
            failures.Add("Mail:FromName is required.");
        }

        if (options.TimeoutMilliseconds <= 0)
        {
            failures.Add("Mail:TimeoutMilliseconds must be greater than zero.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    internal static bool IsTestAddress(string value)
    {
        try
        {
            var address = new MailAddress(value);
            return address.Address.EndsWith("@example.test", StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
