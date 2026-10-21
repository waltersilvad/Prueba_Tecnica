using Facturacion.Application.Comun;
using Facturacion.Application.Correo;
using Microsoft.Extensions.Options;

namespace Facturacion.Application.Tests.Comun;

public sealed class CalendarioNegocioTests
{
    [Fact]
    public void InicioDelDiaUtc_ConvierteLaMedianocheDeBogotaAUtc()
    {
        var calendario = new CalendarioNegocio(Options.Create(new OpcionesPlantillaCorreo
        {
            ZonaHoraria = "America/Bogota",
            Cultura = "es-CO",
        }));

        var instante = calendario.InicioDelDiaUtc(new DateOnly(2026, 10, 21));

        Assert.Equal(new DateTime(2026, 10, 21, 5, 0, 0, DateTimeKind.Utc), instante);
        Assert.Equal(DateTimeKind.Utc, instante.Kind);
    }
}
