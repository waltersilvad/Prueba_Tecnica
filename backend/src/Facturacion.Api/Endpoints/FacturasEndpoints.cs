using Facturacion.Api.Http;
using Facturacion.Api.Solicitudes;
using Facturacion.Application.Facturas;
using Facturacion.Application.Recordatorios;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Facturacion.Api.Endpoints;

internal static class FacturasEndpoints
{
    public static IEndpointRouteBuilder MapFacturas(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/facturas").WithTags("Facturas");

        grupo.MapGet("/", ConsultarAsync)
            .WithName("ConsultarFacturas")
            .WithSummary("Lista las facturas, con filtros opcionales.")
            .WithDescription(
                "`estado` acepta primerrecordatorio, segundorecordatorio o desactivado (texto exacto). " +
                "`clienteId` filtra por cliente. Un estado desconocido devuelve 400.")
            .ProducesProblem(StatusCodes.Status400BadRequest);

        grupo.MapPost("/", RegistrarAsync)
            .AddEndpointFilter<FiltroValidacion<RegistrarFacturaSolicitud>>()
            .WithName("RegistrarFactura")
            .WithSummary("Registra una factura nueva en primerrecordatorio.")
            .WithDescription("Devuelve 404 si el cliente no existe y 409 si el número de factura ya existe.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/procesar", ProcesarSeleccionAsync)
            .AddEndpointFilter<FiltroValidacion<SeleccionFacturasSolicitud>>()
            .WithName("ProcesarFacturasSeleccionadas")
            .WithSummary("Envía el aviso de varias facturas y, por cada correo que sale, avanza su estado.")
            .WithDescription(
                "Mismas reglas que el proceso masivo; el resultado se agrupa por cliente y las facturas desactivadas " +
                "se informan como omitidas. Si alguna factura no existe no se envía ningún aviso (404). " +
                "Devuelve 409 si ya hay una ejecución en curso.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapDelete("/{id}", EliminarAsync)
            .WithName("EliminarFactura")
            .WithSummary("Elimina una factura junto con su historial de envíos.")
            .WithDescription("Devuelve 404 si no existe y 409 si hay un proceso en ejecución.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/eliminar", EliminarSeleccionAsync)
            .AddEndpointFilter<FiltroValidacion<SeleccionFacturasSolicitud>>()
            .WithName("EliminarFacturasSeleccionadas")
            .WithSummary("Elimina varias facturas junto con su historial de envíos.")
            .WithDescription(
                "Si alguna factura no existe no se elimina ninguna (404). Devuelve 409 si hay un proceso en ejecución. " +
                "Es POST porque un DELETE con cuerpo no es estándar.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/{id}/procesar", ProcesarAsync)
            .WithName("ProcesarFactura")
            .WithSummary("Envía el aviso de una sola factura y, si sale, avanza su estado.")
            .WithDescription(
                "Mismas reglas que el proceso masivo. Devuelve 404 si la factura no existe y 409 si está " +
                "desactivada o si ya hay una ejecución en curso.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<FacturaDto>>, ProblemHttpResult>> ConsultarAsync(
        string? estado, string? clienteId, ConsultarFacturasUseCase useCase, CancellationToken cancellationToken)
    {
        var resultado = await useCase.EjecutarAsync(estado, clienteId, cancellationToken);
        return resultado.EsExitoso ? TypedResults.Ok(resultado.Valor) : ResultadosHttp.Problema(resultado.Error);
    }

    private static async Task<Results<Created<FacturaDto>, ProblemHttpResult>> RegistrarAsync(
        RegistrarFacturaSolicitud solicitud, RegistrarFacturaUseCase useCase, CancellationToken cancellationToken)
    {
        var resultado = await useCase.EjecutarAsync(solicitud.AComando(), cancellationToken);
        return resultado.EsExitoso
            ? TypedResults.Created($"/api/facturas?clienteId={resultado.Valor.ClienteId}", resultado.Valor)
            : ResultadosHttp.Problema(resultado.Error);
    }

    private static async Task<Results<Ok<ResultadoEjecucion>, ProblemHttpResult>> ProcesarSeleccionAsync(
        SeleccionFacturasSolicitud solicitud, ProcesarRecordatoriosUseCase useCase, CancellationToken cancellationToken)
    {
        var resultado = await useCase.EjecutarFacturasAsync(solicitud.FacturaIds, cancellationToken);
        return resultado.EsExitoso ? TypedResults.Ok(resultado.Valor) : ResultadosHttp.Problema(resultado.Error);
    }

    private static async Task<Results<Ok<ResultadoEliminacionFacturas>, ProblemHttpResult>> EliminarAsync(
        string id, EliminarFacturasUseCase useCase, CancellationToken cancellationToken)
    {
        var resultado = await useCase.EjecutarAsync([id], cancellationToken);
        return resultado.EsExitoso ? TypedResults.Ok(resultado.Valor) : ResultadosHttp.Problema(resultado.Error);
    }

    private static async Task<Results<Ok<ResultadoEliminacionFacturas>, ProblemHttpResult>> EliminarSeleccionAsync(
        SeleccionFacturasSolicitud solicitud, EliminarFacturasUseCase useCase, CancellationToken cancellationToken)
    {
        var resultado = await useCase.EjecutarAsync(solicitud.FacturaIds, cancellationToken);
        return resultado.EsExitoso ? TypedResults.Ok(resultado.Valor) : ResultadosHttp.Problema(resultado.Error);
    }

    private static async Task<Results<Ok<ResultadoEjecucion>, ProblemHttpResult>> ProcesarAsync(
        string id, ProcesarRecordatoriosUseCase useCase, CancellationToken cancellationToken)
    {
        var resultado = await useCase.EjecutarFacturaAsync(id, cancellationToken);
        return resultado.EsExitoso ? TypedResults.Ok(resultado.Valor) : ResultadosHttp.Problema(resultado.Error);
    }
}
