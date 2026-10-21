using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Utils;
using Monolegal.Billing.Application.Email;

namespace Monolegal.Billing.Infrastructure.Mail;

public sealed class MailKitEmailSender(IOptions<MailOptions> options) : IEmailSender
{
    private readonly MailOptions _options = options.Value;

    public async Task<EmailSendResult> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        if (!MailOptionsValidator.IsTestAddress(message.RecipientAddress))
        {
            return EmailSendResult.KnownNotAccepted(
                "recipient_not_allowed",
                "The recipient must be a valid example.test address.");
        }

        var messageId = MimeUtils.GenerateMessageId("example.test");
        MimeMessage mimeMessage;

        try
        {
            mimeMessage = CreateMessage(message, messageId);
        }
        catch (Exception exception) when (exception is ArgumentException or FormatException)
        {
            return EmailSendResult.KnownNotAccepted(
                "message_validation_failed",
                "The email message could not be constructed safely.");
        }

        using var client = new SmtpClient
        {
            Timeout = _options.TimeoutMilliseconds,
        };

        var sendStarted = false;

        try
        {
            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                SecureSocketOptions.None,
                cancellationToken);

            sendStarted = true;
            await client.SendAsync(mimeMessage, cancellationToken);

            return EmailSendResult.Accepted(messageId);
        }
        catch (SmtpCommandException) when (sendStarted)
        {
            return EmailSendResult.KnownNotAccepted(
                "smtp_rejected",
                "The local SMTP server explicitly rejected the message.");
        }
        catch (Exception) when (sendStarted)
        {
            return EmailSendResult.AcceptanceUnknown(
                "smtp_acceptance_unknown",
                "SMTP started but no reliable acceptance result was received.");
        }
        catch (Exception)
        {
            return EmailSendResult.KnownNotAccepted(
                "smtp_unavailable",
                "The local SMTP server was unavailable before sending started.");
        }
        finally
        {
            if (client.IsConnected)
            {
                await DisconnectSafelyAsync(client);
            }
        }
    }

    private MimeMessage CreateMessage(EmailMessage message, string messageId)
    {
        var mimeMessage = new MimeMessage
        {
            MessageId = messageId,
            Subject = message.Subject,
            Body = new TextPart("plain") { Text = message.TextBody },
        };

        mimeMessage.From.Add(new MailboxAddress(_options.FromName, _options.From));
        mimeMessage.To.Add(new MailboxAddress(message.RecipientName, message.RecipientAddress));

        return mimeMessage;
    }

    private static async Task DisconnectSafelyAsync(SmtpClient client)
    {
        try
        {
            await client.DisconnectAsync(true, CancellationToken.None);
        }
        catch
        {
            // SMTP acceptance is already classified; disconnect failures must not alter it.
        }
    }
}
