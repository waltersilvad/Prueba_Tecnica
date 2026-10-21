namespace Facturacion.Application.Comun;

/// <summary>
/// Evita que dos ejecuciones del proceso de recordatorios corran al mismo tiempo.
/// </summary>
public interface IBloqueoEjecucion
{
    /// <summary>
    /// Intenta adquirir el bloqueo sin esperar.
    /// </summary>
    /// <returns>
    /// Un objeto que libera el bloqueo al desecharse, o <see langword="null"/> si ya hay una ejecución en curso.
    /// </returns>
    IDisposable? IntentarAdquirir();
}
