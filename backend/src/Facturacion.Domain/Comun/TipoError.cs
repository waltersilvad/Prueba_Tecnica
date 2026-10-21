namespace Facturacion.Domain.Comun;

/// <summary>
/// Categoría de un <see cref="ErrorDominio"/>. Permite que otras capas lo traduzcan (por ejemplo, a un código HTTP)
/// sin conocer cada código de error: un error nuevo solo tiene que declarar su tipo.
/// </summary>
public enum TipoError
{
    /// <summary>Los datos recibidos no cumplen las reglas del negocio.</summary>
    Validacion,

    /// <summary>El recurso pedido no existe.</summary>
    NoEncontrado,

    /// <summary>La operación choca con el estado actual (por ejemplo, una factura final o un proceso en curso).</summary>
    Conflicto,
}
