namespace Monolegal.Billing.Infrastructure.Mail;

public sealed class MailOptions
{
    public const string SectionName = "Mail";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    public string From { get; set; } = "no-reply@example.test";

    public string FromName { get; set; } = "Monolegal Billing Demo";

    public bool EnableTls { get; set; }

    public string User { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public int TimeoutMilliseconds { get; set; } = 10_000;
}
