using Facturacion.Domain.Comun;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Facturacion.Api.Http;

/// <summary>
/// Único punto donde los errores esperados (<see cref="ErrorDominio"/>) se traducen a respuestas HTTP.
/// </summary>
/// <remarks>
/// Se decide por <see cref="ErrorDominio.Tipo"/>, no por cada código: un error nuevo obtiene su código HTTP con solo
/// declarar su tipo, sin modificar esta clase. El código del error viaja en la extensión <c>codigo</c>.
/// </remarks>
internal static class ResultadosHttp
{
    public const string ExtensionCodigo = "codigo";

    public static ProblemHttpResult Problema(ErrorDominio error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var (estado, titulo) = error.Tipo switch
        {
            TipoError.Validacion => (StatusCodes.Status400BadRequest, "Solicitud inválida"),
            TipoError.NoEncontrado => (StatusCodes.Status404NotFound, "Recurso no encontrado"),
            TipoError.Conflicto => (StatusCodes.Status409Conflict, "Conflicto con el estado actual"),
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Tipo, "Tipo de error no soportado."),
        };

        return TypedResults.Problem(
            detail: error.Mensaje,
            statusCode: estado,
            title: titulo,
            extensions: new Dictionary<string, object?> { [ExtensionCodigo] = error.Codigo });
    }
}
