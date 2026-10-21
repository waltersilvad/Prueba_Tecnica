using System.Runtime.CompilerServices;

namespace Facturacion.Domain.Comun;

/// <summary>
/// Validaciones de contratos técnicos entre capas. Su incumplimiento es un error de programación,
/// por eso lanzan excepción en lugar de devolver un <see cref="Result"/>.
/// </summary>
internal static class Guardas
{
    public static void AsegurarUtc(DateTime fecha, [CallerArgumentExpression(nameof(fecha))] string? parametro = null)
    {
        if (fecha.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("La fecha debe estar en UTC.", parametro);
        }
    }
}
