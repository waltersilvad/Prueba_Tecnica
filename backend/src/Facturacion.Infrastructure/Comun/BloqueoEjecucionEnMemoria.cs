using Facturacion.Application.Comun;

namespace Facturacion.Infrastructure.Comun;

/// <summary>
/// Bloqueo de ejecución para una sola instancia de la API. Con varias instancias se necesitaría
/// un bloqueo distribuido (límite documentado en el README).
/// </summary>
internal sealed class BloqueoEjecucionEnMemoria : IBloqueoEjecucion, IDisposable
{
    private readonly SemaphoreSlim _semaforo = new(1, 1);

    public IDisposable? IntentarAdquirir() => _semaforo.Wait(TimeSpan.Zero) ? new Liberador(_semaforo) : null;

    public void Dispose() => _semaforo.Dispose();

    /// <summary>Libera el semáforo una sola vez aunque se deseche varias veces.</summary>
    private sealed class Liberador(SemaphoreSlim semaforo) : IDisposable
    {
        private int _liberado;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _liberado, 1) == 0)
            {
                semaforo.Release();
            }
        }
    }
}
