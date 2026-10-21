namespace Facturacion.Application.Comun;

/// <summary>
/// Genera identificadores para entidades nuevas. La persistencia decide el formato.
/// </summary>
public interface IGeneradorId
{
    /// <summary>Devuelve un identificador nuevo y único.</summary>
    string NuevoId();
}
