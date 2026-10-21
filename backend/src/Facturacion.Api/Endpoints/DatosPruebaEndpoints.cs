using Facturacion.Api.Http;
using Facturacion.Infrastructure.Persistencia;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Facturacion.Api.Endpoints;

internal static class DatosPruebaEndpoints
{
    /// <summary>
    /// Solo se registran en Development: en cualquier otro entorno las rutas no existen (404).
    /// </summary>
    public static IEndpointRouteBuilder MapDatosPrueba(this IEndpointRouteBuilder app)
    {
        var grupo = app.MapGroup("/api/datos").WithTags("Datos de prueba");

        grupo.MapPost("/borrar", BorrarAsync)
            .WithName("BorrarTodosLosDatos")
            .WithSummary("Borra todos los datos (solo Development).")
            .WithDescription(
                "Elimina todos los clientes, facturas y envíos, incluidos los de ejemplo, y los archivos .eml que " +
                "generó el proveedor Consola. Devuelve lo que se eliminó. 409 si hay un proceso en curso.")
            .ProducesProblem(StatusCodes.Status409Conflict);

        grupo.MapPost("/ejemplo", CargarEjemploAsync)
            .WithName("CargarDatosDeEjemplo")
            .WithSummary("Carga los clientes y facturas de ejemplo (solo Development).")
            .WithDescription("Solo con la base vacía: si ya hay datos devuelve 409 para no mezclarlos con datos propios.")
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<Results<Ok<ResumenBorrado>, ProblemHttpResult>> BorrarAsync(
        IDatosPrueba datos, CancellationToken cancellationToken)
    {
        var resultado = await datos.BorrarTodoAsync(cancellationToken);
        return resultado.EsExitoso ? TypedResults.Ok(resultado.Valor) : ResultadosHttp.Problema(resultado.Error);
    }

    private static async Task<Results<Ok<ResumenCargaEjemplo>, ProblemHttpResult>> CargarEjemploAsync(
        IDatosPrueba datos, CancellationToken cancellationToken)
    {
        var resultado = await datos.CargarEjemploAsync(cancellationToken);
        return resultado.EsExitoso ? TypedResults.Ok(resultado.Valor) : ResultadosHttp.Problema(resultado.Error);
    }
}
