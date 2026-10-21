namespace Facturacion.Domain.Comun;

/// <summary>
/// Error esperado del negocio. Se devuelve dentro de un <see cref="Result"/> en lugar de lanzar una excepción.
/// </summary>
/// <param name="Codigo">Identificador estable del error, con formato <c>Entidad.Motivo</c>.</param>
/// <param name="Mensaje">Descripción legible del error, apta para mostrarse al usuario.</param>
/// <param name="Tipo">Categoría del error. Es obligatoria para que ningún error quede clasificado por omisión.</param>
public sealed record ErrorDominio(string Codigo, string Mensaje, TipoError Tipo)
{
    /// <summary>Representa la ausencia de error en un resultado exitoso. Su tipo no se usa.</summary>
    public static readonly ErrorDominio Ninguno = new(string.Empty, string.Empty, TipoError.Validacion);
}
