using Monolegal.Billing.Application.Customers;
using Monolegal.Billing.Application.Email;
using Monolegal.Billing.Application.Invoices;
using Monolegal.Billing.Application.Notifications;
using Monolegal.Billing.Domain.Invoices;
using Monolegal.Billing.Domain.Reminders;

namespace Monolegal.Billing.Application.Reminders;

public sealed class ProcessInvoiceReminders(
    IInvoiceRepository invoices,
    ICustomerRepository customers,
    INotificationAuditRepository audit,
    IEmailSender emailSender,
    ITimeSource timeSource,
    IReminderIdGenerator idGenerator)
{
    private static readonly TimeSpan ClaimDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan DispatchDuration = TimeSpan.FromMinutes(30);

    public async Task<ReminderBatchResult> ExecuteAsync(CancellationToken cancellationToken)
    {
        var batchId = idGenerator.NewBatchId();
        var snapshot = await invoices.FindEligibleIdsAsync(cancellationToken);
        var candidates = snapshot
            .DistinctBy(id => id.Value, StringComparer.Ordinal)
            .ToArray();
        var results = new List<ReminderInvoiceResult>(candidates.Length);

        foreach (var invoiceId in candidates)
        {
            try
            {
                results.Add(await ProcessOneAsync(batchId, invoiceId, cancellationToken));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                results.Add(new ReminderInvoiceResult(
                    invoiceId,
                    ReminderProcessingStatus.NeedsReview,
                    FailureCode: "unhandled_invoice_error",
                    Details: "The invoice outcome could not be determined safely."));
            }
        }

        return new ReminderBatchResult(batchId, candidates.Length, results);
    }

    private async Task<ReminderInvoiceResult> ProcessOneAsync(
        string batchId,
        InvoiceId invoiceId,
        CancellationToken cancellationToken)
    {
        Invoice? invoice;

        try
        {
            invoice = await invoices.FindByIdAsync(invoiceId, cancellationToken);
        }
        catch
        {
            return Failed(invoiceId, null, "invoice_read_failed", "The invoice could not be loaded.");
        }

        if (invoice is null)
        {
            return Skipped(invoiceId, "invoice_not_found", "The snapshot invoice no longer exists.");
        }

        var decision = ReminderTransitionPolicy.Default.Evaluate(invoice.State);
        if (!decision.ShouldNotify || decision.NextState is null)
        {
            return Skipped(invoiceId, "state_not_eligible", "The invoice is no longer eligible.");
        }

        var startedAt = timeSource.UtcNow;
        var token = idGenerator.NewClaimToken();
        InvoiceClaim? claim;

        try
        {
            claim = await invoices.TryClaimAsync(
                invoice.Id,
                invoice.State,
                new InvoiceClaimRequest(token, startedAt, startedAt.Add(ClaimDuration)),
                cancellationToken);
        }
        catch
        {
            return Failed(invoiceId, null, "claim_failed", "The invoice claim could not be acquired.");
        }

        if (claim is null)
        {
            return Skipped(invoiceId, "claim_unavailable", "Another process owns or changed the invoice.");
        }

        var attempt = new NotificationAttempt(
            idGenerator.NewAttemptId(),
            batchId,
            invoice.Id,
            invoice.CustomerId,
            invoice.State,
            decision.NextState,
            NotificationAttemptStatus.Claimed,
            startedAt);

        EmailMessage message;

        try
        {
            var customer = await customers.FindByIdAsync(invoice.CustomerId, cancellationToken);
            if (customer is null)
            {
                return await FailBeforeSendAsync(
                    attempt,
                    token,
                    false,
                    "customer_not_found",
                    "The invoice customer does not exist.",
                    cancellationToken);
            }

            message = ReminderEmailFactory.Create(customer, invoice, decision.NextState);
        }
        catch
        {
            return await FailBeforeSendAsync(
                attempt,
                token,
                false,
                "customer_or_email_invalid",
                "The customer relationship or email is invalid.",
                cancellationToken);
        }

        if (!await TryAddAttemptAsync(attempt, cancellationToken))
        {
            await TryReleaseAsync(attempt.InvoiceId, token, cancellationToken);
            return Failed(
                invoiceId,
                attempt.AttemptId,
                "audit_create_failed",
                "The send attempt could not be created; SMTP was not called.");
        }

        var dispatchStartedAt = timeSource.UtcNow;
        var dispatching = attempt with
        {
            Status = NotificationAttemptStatus.Dispatching,
            DispatchStartedAt = dispatchStartedAt,
        };

        if (!await TryUpdateAttemptAsync(dispatching, cancellationToken))
        {
            return await FailBeforeSendAsync(
                attempt,
                token,
                true,
                "audit_dispatch_failed",
                "Dispatching could not be persisted; SMTP was not called.",
                cancellationToken);
        }

        bool dispatchClaimed;

        try
        {
            dispatchClaimed = await invoices.TryBeginDispatchAsync(
                invoice.Id,
                token,
                dispatchStartedAt.Add(DispatchDuration),
                cancellationToken);
        }
        catch
        {
            dispatchClaimed = false;
        }

        if (!dispatchClaimed)
        {
            return await FailBeforeSendAsync(
                dispatching,
                token,
                true,
                "dispatch_claim_failed",
                "The claim could not enter Dispatching; SMTP was not called.",
                cancellationToken);
        }

        EmailSendResult sendResult;

        try
        {
            sendResult = await emailSender.SendAsync(message, cancellationToken);
        }
        catch
        {
            return await MarkNeedsReviewAsync(
                dispatching,
                "email_sender_threw",
                "SMTP started but the sender did not return a typed result.",
                cancellationToken);
        }

        if (sendResult.Status == EmailSendStatus.KnownNotAccepted)
        {
            return await FailBeforeSendAsync(
                dispatching,
                token,
                true,
                sendResult.FailureCode ?? "smtp_not_accepted",
                sendResult.Details ?? "The local SMTP server did not accept the message.",
                cancellationToken);
        }

        if (sendResult.Status == EmailSendStatus.AcceptanceUnknown)
        {
            return await MarkNeedsReviewAsync(
                dispatching,
                sendResult.FailureCode ?? "smtp_acceptance_unknown",
                sendResult.Details ?? "SMTP acceptance could not be determined.",
                cancellationToken);
        }

        var acceptedAt = timeSource.UtcNow;
        var accepted = dispatching with
        {
            Status = NotificationAttemptStatus.SmtpAccepted,
            SmtpAcceptedAt = acceptedAt,
            MessageId = sendResult.MessageId,
        };

        if (!await TryUpdateAttemptAsync(accepted, cancellationToken))
        {
            return await MarkNeedsReviewAsync(
                accepted,
                "smtp_acceptance_audit_failed",
                "SMTP accepted the message but acceptance could not be persisted.",
                cancellationToken);
        }

        bool transitioned;

        try
        {
            transitioned = await invoices.TryCompleteTransitionAsync(
                invoice.Id,
                invoice.State,
                token,
                decision.NextState,
                acceptedAt,
                cancellationToken);
        }
        catch
        {
            transitioned = false;
        }

        if (!transitioned)
        {
            return await MarkNeedsReviewAsync(
                accepted,
                "invoice_transition_failed",
                "SMTP accepted the message but the invoice transition was not confirmed.",
                cancellationToken);
        }

        var completed = accepted with
        {
            Status = NotificationAttemptStatus.Completed,
            FinishedAt = timeSource.UtcNow,
        };

        if (!await TryUpdateAttemptAsync(completed, cancellationToken))
        {
            return await MarkNeedsReviewAsync(
                accepted,
                "completion_audit_failed",
                "The invoice transitioned but completion could not be persisted.",
                cancellationToken);
        }

        return new ReminderInvoiceResult(
            invoice.Id,
            ReminderProcessingStatus.Succeeded,
            attempt.AttemptId);
    }

    private async Task<ReminderInvoiceResult> FailBeforeSendAsync(
        NotificationAttempt attempt,
        string token,
        bool attemptExists,
        string failureCode,
        string details,
        CancellationToken cancellationToken)
    {
        var failed = attempt with
        {
            Status = NotificationAttemptStatus.Failed,
            FinishedAt = timeSource.UtcNow,
            FailureCode = failureCode,
            Details = details,
        };

        if (attemptExists)
        {
            await TryUpdateAttemptAsync(failed, cancellationToken);
        }
        else
        {
            await TryAddAttemptAsync(failed, cancellationToken);
        }

        await TryReleaseAsync(attempt.InvoiceId, token, cancellationToken);

        return Failed(attempt.InvoiceId, attempt.AttemptId, failureCode, details);
    }

    private async Task<ReminderInvoiceResult> MarkNeedsReviewAsync(
        NotificationAttempt attempt,
        string failureCode,
        string details,
        CancellationToken cancellationToken)
    {
        var needsReview = attempt with
        {
            Status = NotificationAttemptStatus.NeedsReview,
            FinishedAt = timeSource.UtcNow,
            FailureCode = failureCode,
            Details = details,
        };

        await TryUpdateAttemptAsync(needsReview, cancellationToken);

        return new ReminderInvoiceResult(
            attempt.InvoiceId,
            ReminderProcessingStatus.NeedsReview,
            attempt.AttemptId,
            failureCode,
            details);
    }

    private async Task<bool> TryAddAttemptAsync(NotificationAttempt attempt, CancellationToken cancellationToken)
    {
        try
        {
            await audit.AddAsync(attempt, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> TryUpdateAttemptAsync(NotificationAttempt attempt, CancellationToken cancellationToken)
    {
        try
        {
            return await audit.UpdateAsync(attempt, cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    private async Task TryReleaseAsync(
        InvoiceId invoiceId,
        string? token,
        CancellationToken cancellationToken)
    {
        if (token is null)
        {
            return;
        }

        try
        {
            await invoices.ReleaseClaimAsync(invoiceId, token, cancellationToken);
        }
        catch
        {
            // A pre-SMTP failure remains known, even if claim cleanup must wait for expiry.
        }
    }

    private static ReminderInvoiceResult Failed(
        InvoiceId invoiceId,
        string? attemptId,
        string failureCode,
        string details) =>
        new(invoiceId, ReminderProcessingStatus.Failed, attemptId, failureCode, details);

    private static ReminderInvoiceResult Skipped(
        InvoiceId invoiceId,
        string failureCode,
        string details) =>
        new(invoiceId, ReminderProcessingStatus.Skipped, FailureCode: failureCode, Details: details);
}
