using Facturacion.Api.Http;
using Facturacion.Api.Solicitudes;
using Facturacion.Application.Clientes;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Facturacion.Api.Endpoints;

internal static class ClientesEndpoints
{
    public static IEndpointRouteBuilder MapClientes(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/clientes").WithTags("Clientes");

        grupo.MapGet("/", ListarAsync)
            .WithName("ListarClientes")
            .WithSummary("Lista los clientes ordenados por nombre.");

        grupo.MapGet("/{id}", ObtenerAsync)
            .WithName("ObtenerCliente")
            .WithSummary("Obtiene un cliente por su id.")
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapPost("/", CrearAsync)
            .AddEndpointFilter<FiltroValidacion<ClienteSolicitud>>()
            .WithName("CrearCliente")
            .WithSummary("Registra un cliente nuevo.")
            .ProducesValidationProblem();

        grupo.MapPut("/{id}", ActualizarAsync)
            .AddEndpointFilter<FiltroValidacion<ClienteSolicitud>>()
            .WithName("ActualizarCliente")
            .WithSummary("Edita el nombre y el correo de un cliente.")
            .WithDescription("El correo editado es el destinatario de los siguientes avisos.")
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        grupo.MapDelete("/{id}", EliminarAsync)
            .WithName("EliminarCliente")
            .WithSummary("Elimina un cliente junto con sus facturas y su historial de envíos.")
            .WithDescription("Devuelve 404 si no existe y 409 si hay un proceso de avisos en ejecución.")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<ClienteDto>>> ListarAsync(
        ConsultarClientesUseCase useCase, CancellationToken cancellationToken) =>
        TypedResults.Ok(await useCase.ListarAsync(cancellationToken));

    private static async Task<Results<Ok<ClienteDto>, ProblemHttpResult>> ObtenerAsync(
        string id, ConsultarClientesUseCase useCase, CancellationToken cancellationToken)
    {
        var resultado = await useCase.ObtenerAsync(id, cancellationToken);
        return resultado.EsExitoso ? TypedResults.Ok(resultado.Valor) : ResultadosHttp.Problema(resultado.Error);
    }

    private static async Task<Results<Created<ClienteDto>, ProblemHttpResult>> CrearAsync(
        ClienteSolicitud solicitud, CrearClienteUseCase useCase, CancellationToken cancellationToken)
    {
        var resultado = await useCase.EjecutarAsync(solicitud.ACrearComando(), cancellationToken);
        return resultado.EsExitoso
            ? TypedResults.Created($"/api/clientes/{resultado.Valor.Id}", resultado.Valor)
            : ResultadosHttp.Problema(resultado.Error);
    }

    private static async Task<Results<Ok<ResultadoEliminacionCliente>, ProblemHttpResult>> EliminarAsync(
        string id, EliminarClienteUseCase useCase, CancellationToken cancellationToken)
    {
        var resultado = await useCase.EjecutarAsync(id, cancellationToken);
        return resultado.EsExitoso ? TypedResults.Ok(resultado.Valor) : ResultadosHttp.Problema(resultado.Error);
    }

    private static async Task<Results<Ok<ClienteDto>, ProblemHttpResult>> ActualizarAsync(
        string id,
        ClienteSolicitud solicitud,
        ActualizarClienteUseCase useCase,
        CancellationToken cancellationToken)
    {
        var resultado = await useCase.EjecutarAsync(id, solicitud.AActualizarComando(), cancellationToken);
        return resultado.EsExitoso ? TypedResults.Ok(resultado.Valor) : ResultadosHttp.Problema(resultado.Error);
    }
}
