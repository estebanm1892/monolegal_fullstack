using Microsoft.Extensions.Options;
using Monolegal.Billing.Application.Email;
using Monolegal.Billing.Infrastructure.Mail;

namespace Monolegal.Billing.IntegrationTests.Mail;

public class MailKitEmailSenderBoundaryTests
{
    [Theory]
    [InlineData("")]
    [InlineData("malformed")]
    [InlineData("person@example.com")]
    public async Task RejectsUnsafeRecipientWithTypedResult(string recipient)
    {
        var sender = new MailKitEmailSender(Options.Create(new MailOptions
        {
            Host = "192.0.2.1",
            Port = 1,
            TimeoutMilliseconds = 60_000,
        }));

        var result = await sender.SendAsync(
            new EmailMessage(recipient, "Person", "Subject", "Body"),
            CancellationToken.None);

        Assert.Equal(EmailSendStatus.KnownNotAccepted, result.Status);
        Assert.Equal("recipient_not_allowed", result.FailureCode);
    }
}
