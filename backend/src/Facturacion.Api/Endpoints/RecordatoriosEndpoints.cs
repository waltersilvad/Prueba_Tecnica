using Facturacion.Api.Http;
using Facturacion.Application.Recordatorios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Facturacion.Api.Endpoints;

internal static class RecordatoriosEndpoints
{
    public static IEndpointRouteBuilder MapRecordatorios(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/recordatorios")
            .WithTags("Recordatorios")
            .MapPost("/procesar", ProcesarAsync)
            .WithName("ProcesarRecordatorios")
            .WithSummary("Procesa los recordatorios de facturas.")
            .WithDescription(
                "Envía un correo por cada factura en primerrecordatorio o segundorecordatorio y, solo si el correo " +
                "sale, avanza su estado. Procesa todos los clientes o solo `clienteId`. Un fallo en un cliente o en " +
                "una factura no detiene a los demás. Devuelve 409 si ya hay una ejecución en curso.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<ResultadoEjecucion>, ProblemHttpResult>> ProcesarAsync(
        string? clienteId, ProcesarRecordatoriosUseCase useCase, CancellationToken cancellationToken)
    {
        var resultado = await useCase.EjecutarAsync(
            string.IsNullOrWhiteSpace(clienteId) ? null : clienteId, cancellationToken);

        return resultado.EsExitoso ? TypedResults.Ok(resultado.Valor) : ResultadosHttp.Problema(resultado.Error);
    }
}
