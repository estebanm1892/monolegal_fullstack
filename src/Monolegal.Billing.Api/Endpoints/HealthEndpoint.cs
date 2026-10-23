using Monolegal.Billing.Api.Contracts;

namespace Monolegal.Billing.Api.Endpoints;

public static class HealthEndpoint
{
    public static void MapHealthEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", () => Results.Ok(new HealthResponseDto("healthy")))
            .WithName("GetHealth")
            .WithTags("Salud del servicio")
            .WithSummary("Consultar el estado del servicio")
            .WithDescription(
                "Comprueba que el proceso de la API está respondiendo. No verifica la disponibilidad de MongoDB ni de Mailpit.")
            .Produces<HealthResponseDto>(StatusCodes.Status200OK);
    }
}
