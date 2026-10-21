using Facturacion.Application.Correo;
using Microsoft.Extensions.Options;

namespace Facturacion.Application.Comun;

/// <summary>
/// Convierte fechas de calendario que escribe el usuario en instantes UTC, usando la misma zona horaria en la que
/// se muestran las fechas (<see cref="OpcionesPlantillaCorreo.ZonaHoraria"/>). Así la fecha que el usuario elige
/// es exactamente la que luego ve en la interfaz y en el correo.
/// </summary>
public sealed class CalendarioNegocio
{
    private readonly TimeZoneInfo _zonaHoraria;

    public CalendarioNegocio(IOptions<OpcionesPlantillaCorreo> opciones)
    {
        ArgumentNullException.ThrowIfNull(opciones);
        _zonaHoraria = TimeZoneInfo.FindSystemTimeZoneById(opciones.Value.ZonaHoraria);
    }

    /// <summary>Instante UTC correspondiente a las 00:00 de <paramref name="fecha"/> en la zona del negocio.</summary>
    public DateTime InicioDelDiaUtc(DateOnly fecha) =>
        TimeZoneInfo.ConvertTimeToUtc(fecha.ToDateTime(TimeOnly.MinValue), _zonaHoraria);
}
