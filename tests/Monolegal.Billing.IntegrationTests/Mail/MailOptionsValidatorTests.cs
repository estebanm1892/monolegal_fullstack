using Monolegal.Billing.Infrastructure.Mail;

namespace Monolegal.Billing.IntegrationTests.Mail;

public class MailOptionsValidatorTests
{
    private readonly MailOptionsValidator _validator = new();

    [Fact]
    public void AcceptsLocalMailpitWithoutTlsOrCredentials()
    {
        var result = _validator.Validate(null, ValidOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("mail.example.com")]
    [InlineData("0.0.0.0")]
    [InlineData("192.168.1.15")]
    public void RejectsNonLoopbackHosts(string host)
    {
        var options = ValidOptions();
        options.Host = host;

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("loopback", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void RejectsTlsCredentialsAndNonTestSender()
    {
        var options = ValidOptions();
        options.EnableTls = true;
        options.User = "real-user";
        options.Password = "real-password";
        options.From = "billing@example.com";

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("TLS", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Failures!, failure => failure.Contains("credentials", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Failures!, failure => failure.Contains("example.test", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(65536)]
    public void RejectsInvalidPort(int port)
    {
        var options = ValidOptions();
        options.Port = port;

        var result = _validator.Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("port", StringComparison.OrdinalIgnoreCase));
    }

    private static MailOptions ValidOptions() => new()
    {
        Host = "localhost",
        Port = 1025,
        From = "no-reply@example.test",
        EnableTls = false,
        User = string.Empty,
        Password = string.Empty,
    };
}
