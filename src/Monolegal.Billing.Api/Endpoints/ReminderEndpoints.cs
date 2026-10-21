using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Monolegal.Billing.Application.Email;
using Monolegal.Billing.Application.Notifications;
using Monolegal.Billing.Application.Reminders;
using Monolegal.Billing.Api.Contracts;

namespace Monolegal.Billing.Api.Endpoints;

public static class ReminderEndpoints
{
    public static void MapReminderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reminders");

        group.MapPost("/process", async (
            ProcessInvoiceReminders processReminders,
            IWebHostEnvironment environment,
            CancellationToken ct) =>
        {
            if (!environment.IsDevelopment())
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var result = await processReminders.ExecuteAsync(ct);

            return Results.Ok(new ReminderProcessResponseDto(
                result.BatchId,
                result.TotalCandidates,
                result.Succeeded,
                result.Failed,
                result.Skipped,
                result.NeedsReview,
                result.Results.Select(r => new ReminderInvoiceResultDto(
                    r.InvoiceId.Value,
                    r.Status.ToString(),
                    r.AttemptId,
                    r.FailureCode,
                    r.Details)).ToArray()));
        })
        .WithName("ProcessReminders")
        .WithOpenApi();

        group.MapGet("/runs/{batchId}", async (
            string batchId,
            INotificationAuditRepository audit,
            CancellationToken ct) =>
        {
            var attempts = await audit.FindByBatchIdAsync(batchId, ct);

            if (!attempts.Any())
            {
                return Results.NotFound();
            }

            return Results.Ok(attempts.Select(a => new
            {
                attemptId = a.AttemptId,
                invoiceId = a.InvoiceId.Value,
                previousState = a.PreviousState.Persisted,
                nextState = a.NextState.Persisted,
                status = a.Status,
                startedAt = a.StartedAt,
                smtpAcceptedAt = a.SmtpAcceptedAt,
                finishedAt = a.FinishedAt,
                messageId = a.MessageId,
                failureCode = a.FailureCode,
            }));
        })
        .WithName("GetReminderRun")
        .WithOpenApi();
    }
}
