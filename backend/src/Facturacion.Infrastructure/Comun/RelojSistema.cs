using Facturacion.Application.Comun;

namespace Facturacion.Infrastructure.Comun;

/// <summary>Reloj real del sistema. Es el único lugar donde se lee la hora actual.</summary>
internal sealed class RelojSistema(TimeProvider proveedorTiempo) : IReloj
{
    public DateTime AhoraUtc => proveedorTiempo.GetUtcNow().UtcDateTime;
}
