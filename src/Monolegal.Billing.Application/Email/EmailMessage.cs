namespace Monolegal.Billing.Application.Email;

public sealed record EmailMessage(
    string RecipientAddress,
    string RecipientName,
    string Subject,
    string TextBody);
