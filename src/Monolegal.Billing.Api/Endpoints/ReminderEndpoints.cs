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
        .WithTags("Recordatorios")
        .WithSummary("Ejecutar manualmente los recordatorios")
        .WithDescription("""
            Ejecuta manualmente un lote sobre todas las facturas elegibles de la base de datos. Los filtros activos del frontend Angular no restringen su alcance.

            - `primerrecordatorio` puede avanzar a `segundorecordatorio`.
            - `segundorecordatorio` puede avanzar a `desactivado`.
            - Cada factura elegible genera una notificación individual.
            - El estado solo se actualiza después de la aceptación SMTP y de confirmar la transición en MongoDB según las reglas del dominio.
            - El resultado puede contener operaciones exitosas, fallidas, omitidas o que requieren revisión manual.
            - Una ejecución posterior puede producir una nueva transición sobre facturas procesadas anteriormente.

            **ADVERTENCIA:** esta operación modifica datos en MongoDB y genera mensajes en Mailpit. Está disponible exclusivamente en el entorno local Development.
            """)
        .Produces<ReminderProcessResponseDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status500InternalServerError);

        group.MapGet("/runs/{batchId}", async (
            string batchId,
            INotificationAuditRepository audit,
            CancellationToken ct) =>
        {
            var attempts = await audit.FindByBatchIdAsync(batchId, ct);

            if (!attempts.Any())
            {
                return Results.Problem(
                    title: "Reminder run not found",
                    detail: $"No notification attempts were recorded for batch '{batchId}'.",
                    statusCode: StatusCodes.Status404NotFound);
            }

            return Results.Ok(attempts.Select(a => new ReminderRunAttemptDto(
                a.AttemptId,
                a.InvoiceId.Value,
                a.PreviousState.Persisted,
                a.NextState.Persisted,
                (int)a.Status,
                a.StartedAt,
                a.SmtpAcceptedAt,
                a.FinishedAt,
                a.MessageId,
                a.FailureCode)));
        })
        .WithName("GetReminderRun")
        .WithTags("Recordatorios")
        .WithSummary("Consultar auditoría de una ejecución")
        .WithDescription(
            "Devuelve los intentos de notificación registrados para el `batchId`. Una ejecución sin candidatas no crea intentos y, por ello, responde 404. El campo numérico `status` representa la fase auditada: 0 Claimed, 1 Dispatching, 2 SmtpAccepted, 3 Completed, 4 Failed y 5 NeedsReview.")
        .Produces<ReminderRunAttemptDto[]>(StatusCodes.Status200OK)
        .ProducesProblem(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status500InternalServerError);
    }
}
