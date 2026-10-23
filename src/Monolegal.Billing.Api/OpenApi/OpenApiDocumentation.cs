using System.Text.Json.Nodes;
using Microsoft.OpenApi;

namespace Monolegal.Billing.Api.OpenApi;

internal static class OpenApiDocumentation
{
    internal static void Document(OpenApiOperation operation)
    {
        switch (operation.OperationId)
        {
            case "GetCustomers":
                DescribeResponses(operation, new Dictionary<string, string>
                {
                    ["200"] = "Correcto",
                    ["500"] = "Error interno del servidor",
                });
                ApplyExamples(operation, new Dictionary<string, string>
                {
                    ["200"] = """
                        [{"id":"cliente-a","name":"Cliente A","email":"cliente-a@example.test","active":true}]
                        """,
                });
                break;

            case "GetInvoices":
                DescribeResponses(operation, new Dictionary<string, string>
                {
                    ["200"] = "Correcto",
                    ["400"] = "Solicitud incorrecta",
                    ["500"] = "Error interno del servidor",
                });
                ApplyExamples(operation, new Dictionary<string, string>
                {
                    ["200"] = """
                        {
                          "items": [{
                            "id": "FAC-001",
                            "customerId": "cliente-a",
                            "number": "FAC-001",
                            "issuedAt": "2026-09-01T00:00:00+00:00",
                            "dueAt": "2026-09-10T00:00:00+00:00",
                            "updatedAt": "2026-10-01T00:00:00+00:00",
                            "amount": 150000,
                            "currency": "COP",
                            "state": "primerrecordatorio"
                          }],
                          "page": 1,
                          "pageSize": 20,
                          "totalCount": 6,
                          "totalPages": 1
                        }
                        """,
                    ["400"] = """
                        {"type":"about:blank","title":"Invalid pagination","status":400,"detail":"pageSize must be between 1 and 100."}
                        """,
                });
                DescribeParameters(operation, new Dictionary<string, string>
                {
                    ["page"] = "Página solicitada. Debe ser mayor o igual a 1. Valor predeterminado: 1.",
                    ["pageSize"] = "Cantidad de facturas por página. Valores permitidos: 1 a 100. Valor predeterminado: 20.",
                    ["customerId"] = "Identificador opcional del cliente. Ejemplo: cliente-a.",
                    ["status"] = "Estado exacto opcional: primerrecordatorio, segundorecordatorio, pendiente o desactivado.",
                });
                break;

            case "GetInvoiceSummary":
                DescribeResponses(operation, new Dictionary<string, string>
                {
                    ["200"] = "Correcto",
                    ["500"] = "Error interno del servidor",
                });
                ApplyExamples(operation, new Dictionary<string, string>
                {
                    ["200"] = """
                        {
                          "totalInvoices": 6,
                          "countByStatus": {"primerrecordatorio":2,"segundorecordatorio":2,"pendiente":1,"desactivado":1},
                          "countByCustomer": {"cliente-a":2,"cliente-b":2,"cliente-c":2},
                          "totalAmountCop": 1060000
                        }
                        """,
                });
                break;

            case "ProcessReminders":
                DescribeResponses(operation, new Dictionary<string, string>
                {
                    ["200"] = "Correcto",
                    ["403"] = "Operación no disponible fuera de Development",
                    ["500"] = "Error interno del servidor",
                });
                ApplyExamples(operation, new Dictionary<string, string>
                {
                    ["200"] = """
                        {
                          "batchId": "batch-0123456789abcdef0123456789abcdef",
                          "totalCandidates": 4,
                          "succeeded": 4,
                          "failed": 0,
                          "skipped": 0,
                          "needsReview": 0,
                          "results": [{
                            "invoiceId": "FAC-001",
                            "status": "Succeeded",
                            "attemptId": "attempt-0123456789abcdef0123456789abcdef",
                            "failureCode": null,
                            "details": null
                          }]
                        }
                        """,
                });
                break;

            case "GetReminderRun":
                DescribeResponses(operation, new Dictionary<string, string>
                {
                    ["200"] = "Correcto",
                    ["404"] = "Ejecución no encontrada",
                    ["500"] = "Error interno del servidor",
                });
                ApplyExamples(operation, new Dictionary<string, string>
                {
                    ["200"] = """
                        [{
                          "attemptId": "attempt-0123456789abcdef0123456789abcdef",
                          "invoiceId": "FAC-001",
                          "previousState": "primerrecordatorio",
                          "nextState": "segundorecordatorio",
                          "status": 3,
                          "startedAt": "2026-10-22T12:00:00+00:00",
                          "smtpAcceptedAt": "2026-10-22T12:00:01+00:00",
                          "finishedAt": "2026-10-22T12:00:02+00:00",
                          "messageId": "message@example.test",
                          "failureCode": null
                        }]
                        """,
                    ["404"] = """
                        {"type":"about:blank","title":"Reminder run not found","status":404,"detail":"No notification attempts were recorded for batch 'batch-empty'."}
                        """,
                });
                DescribeParameters(operation, new Dictionary<string, string>
                {
                    ["batchId"] = "Identificador de auditoría devuelto por POST /api/reminders/process.",
                });
                break;

            case "GetHealth":
                DescribeResponses(operation, new Dictionary<string, string>
                {
                    ["200"] = "Correcto",
                });
                ApplyExamples(operation, new Dictionary<string, string>
                {
                    ["200"] = """{"status":"healthy"}""",
                });
                break;
        }
    }

    private static void ApplyExamples(
        OpenApiOperation operation,
        IReadOnlyDictionary<string, string> responseExamples)
    {
        foreach (var (statusCode, json) in responseExamples)
        {
            if (operation.Responses is null ||
                !operation.Responses.TryGetValue(statusCode, out var response) ||
                response.Content is null)
            {
                continue;
            }

            var mediaType = response.Content.TryGetValue("application/json", out var jsonMediaType)
                ? jsonMediaType
                : response.Content.Values.FirstOrDefault();

            if (mediaType is null)
            {
                continue;
            }

            mediaType.Example = JsonNode.Parse(json);
        }
    }

    private static void DescribeParameters(
        OpenApiOperation operation,
        IReadOnlyDictionary<string, string> parameterDescriptions)
    {
        if (operation.Parameters is null)
        {
            return;
        }

        foreach (var parameter in operation.Parameters)
        {
            if (parameter.Name is { } name &&
                parameterDescriptions.TryGetValue(name, out var description))
            {
                parameter.Description = description;
            }
        }
    }

    private static void DescribeResponses(
        OpenApiOperation operation,
        IReadOnlyDictionary<string, string> responseDescriptions)
    {
        if (operation.Responses is null)
        {
            return;
        }

        foreach (var (statusCode, description) in responseDescriptions)
        {
            if (operation.Responses.TryGetValue(statusCode, out var response))
            {
                response.Description = description;
            }
        }
    }
}
