namespace Monolegal.Billing.Application.Email;

public enum EmailSendStatus
{
    Accepted = 0,
    KnownNotAccepted = 1,
    AcceptanceUnknown = 2,
}

public sealed record EmailSendResult(
    EmailSendStatus Status,
    string? MessageId = null,
    string? FailureCode = null,
    string? Details = null)
{
    public static EmailSendResult Accepted(string messageId) =>
        new(EmailSendStatus.Accepted, messageId);

    public static EmailSendResult KnownNotAccepted(string failureCode, string details) =>
        new(EmailSendStatus.KnownNotAccepted, FailureCode: failureCode, Details: details);

    public static EmailSendResult AcceptanceUnknown(string failureCode, string details) =>
        new(EmailSendStatus.AcceptanceUnknown, FailureCode: failureCode, Details: details);
}
