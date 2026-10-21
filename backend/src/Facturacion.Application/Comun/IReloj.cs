namespace Facturacion.Application.Comun;

/// <summary>
/// Fuente de la fecha y hora actual. Permite controlar el tiempo en las pruebas.
/// </summary>
public interface IReloj
{
    /// <summary>Fecha y hora actual en UTC (<see cref="DateTimeKind.Utc"/>).</summary>
    DateTime AhoraUtc { get; }
}
