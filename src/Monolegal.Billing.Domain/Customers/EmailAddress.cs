namespace Monolegal.Billing.Domain.Customers;

public sealed record EmailAddress
{
    private const int MaxLength = 254;
    private const int MaxLocalPartLength = 64;

    private EmailAddress(string value) => Value = value;

    public string Value { get; }

    public static bool TryParse(string? value, out EmailAddress? email)
    {
        email = null;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var candidate = value.Trim();
        if (candidate.Length > MaxLength || candidate.Any(char.IsWhiteSpace))
        {
            return false;
        }

        var at = candidate.IndexOf('@');
        if (at <= 0 || at != candidate.LastIndexOf('@') || at == candidate.Length - 1)
        {
            return false;
        }

        var local = candidate[..at];
        var domain = candidate[(at + 1)..];

        if (local.Length > MaxLocalPartLength || local.StartsWith('.') || local.EndsWith('.') || local.Contains(".."))
        {
            return false;
        }

        if (!domain.Contains('.') || domain.StartsWith('.') || domain.EndsWith('.') || domain.Contains(".."))
        {
            return false;
        }

        if (domain.Split('.').Any(label => label.StartsWith('-') || label.EndsWith('-')))
        {
            return false;
        }

        email = new EmailAddress(candidate);
        return true;
    }

    public static EmailAddress Parse(string value) =>
        TryParse(value, out var email)
            ? email!
            : throw new FormatException($"'{value}' is not a syntactically valid email address.");

    public override string ToString() => Value;
}
