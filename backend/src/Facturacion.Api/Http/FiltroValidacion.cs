using System.Text.Json;
using FluentValidation;

namespace Facturacion.Api.Http;

/// <summary>
/// Valida el cuerpo de tipo <typeparamref name="T"/> con FluentValidation antes de ejecutar el endpoint.
/// Si no es válido responde 400 con los errores por campo (en camelCase, como el JSON).
/// </summary>
internal sealed class FiltroValidacion<T>(IValidator<T> validador) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var solicitud = context.Arguments.OfType<T>().Single();
        var resultado = await validador.ValidateAsync(solicitud, context.HttpContext.RequestAborted);
        if (resultado.IsValid)
        {
            return await next(context);
        }

        var errores = resultado.Errors
            .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return TypedResults.ValidationProblem(errores, title: "Los datos enviados no son válidos.");
    }
}
