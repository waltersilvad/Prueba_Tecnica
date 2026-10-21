using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Facturacion.Api.Endpoints;

internal static class SaludEndpoints
{
    /// <summary>
    /// <c>GET /health</c>: 200 si la API y MongoDB responden, 503 si no. El cuerpo detalla cada chequeo.
    /// </summary>
    public static IEndpointRouteBuilder MapSalud(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", new HealthCheckOptions { ResponseWriter = EscribirRespuestaAsync })
            .WithTags("Salud");

        return app;
    }

    private static Task EscribirRespuestaAsync(HttpContext contexto, HealthReport reporte) =>
        contexto.Response.WriteAsJsonAsync(new
        {
            estado = reporte.Status.ToString(),
            chequeos = reporte.Entries.ToDictionary(
                e => e.Key,
                e => new { estado = e.Value.Status.ToString(), descripcion = e.Value.Description }),
        });
}
