using Facturacion.Application.Envios;
using Facturacion.Application.Resumen;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Facturacion.Api.Endpoints;

/// <summary>Endpoints de solo lectura: resumen por cliente e historial de envíos.</summary>
internal static class ConsultasEndpoints
{
    public static IEndpointRouteBuilder MapConsultas(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/resumen/clientes", ObtenerResumenAsync)
            .WithTags("Resumen")
            .WithName("ResumenPorCliente")
            .WithSummary("Conteo de facturas por estado, total y monto pendiente por moneda de cada cliente.");

        app.MapGet("/api/envios", ConsultarEnviosAsync)
            .WithTags("Envíos")
            .WithName("HistorialEnvios")
            .WithSummary("Historial de envíos, del más reciente al más antiguo.")
            .WithDescription("`clienteId` es opcional.");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ResumenClienteDto>>> ObtenerResumenAsync(
        ObtenerResumenClientesUseCase useCase, CancellationToken cancellationToken) =>
        TypedResults.Ok(await useCase.EjecutarAsync(cancellationToken));

    private static async Task<Ok<IReadOnlyList<EnvioDto>>> ConsultarEnviosAsync(
        string? clienteId, ConsultarEnviosUseCase useCase, CancellationToken cancellationToken) =>
        TypedResults.Ok(await useCase.EjecutarAsync(
            string.IsNullOrWhiteSpace(clienteId) ? null : clienteId, cancellationToken));
}
