using Facturacion.Application.Correo;
using Facturacion.Domain.Facturas;
using Microsoft.Extensions.Options;

namespace Facturacion.Application.Tests.Correo;

public sealed class PlantillaCorreoAvisosTests
{
    private readonly PlantillaCorreoAvisos _plantilla = new(Options.Create(new OpcionesPlantillaCorreo
    {
        ZonaHoraria = "America/Bogota",
        Cultura = "es-CO",
    }));

    [Fact]
    public void Generar_CadaAvisoTieneUnAsuntoPropioConElNumeroDeFactura()
    {
        var cliente = Datos.CrearCliente();
        var factura = Datos.CrearFactura("FAC-0001");

        var segundo = _plantilla.Generar(TipoAviso.SegundoRecordatorio, cliente, factura);
        var desactivacion = _plantilla.Generar(TipoAviso.Desactivacion, cliente, factura);

        Assert.NotEqual(segundo.Asunto, desactivacion.Asunto);
        Assert.Contains("FAC-0001", segundo.Asunto, StringComparison.Ordinal);
        Assert.Contains("FAC-0001", desactivacion.Asunto, StringComparison.Ordinal);
        Assert.Contains("Segundo recordatorio", segundo.Asunto, StringComparison.Ordinal);
        Assert.Contains("desactivación", desactivacion.Asunto, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TipoAviso.SegundoRecordatorio)]
    [InlineData(TipoAviso.Desactivacion)]
    public void Generar_IncluyeLosDatosDeLaFacturaEnHtmlYTexto(TipoAviso aviso)
    {
        var cliente = Datos.CrearCliente(nombre: "Comercial Andina");
        var factura = Datos.CrearFactura(
            "FAC-0001",
            monto: 150_000.5m,
            moneda: "COP",
            fechaVencimiento: new DateTime(2026, 10, 1, 5, 0, 0, DateTimeKind.Utc));

        var contenido = _plantilla.Generar(aviso, cliente, factura);

        foreach (var cuerpo in new[] { contenido.CuerpoHtml, contenido.CuerpoTexto })
        {
            Assert.Contains("Comercial Andina", cuerpo, StringComparison.Ordinal);
            Assert.Contains("FAC-0001", cuerpo, StringComparison.Ordinal);
            Assert.Contains("150.000,50 COP", cuerpo, StringComparison.Ordinal);
            Assert.Contains("01/10/2026", cuerpo, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Generar_MuestraElMontoConLaMonedaDeLaFactura()
    {
        var factura = Datos.CrearFactura("FAC-0002", monto: 1_250.75m, moneda: "USD");

        var contenido = _plantilla.Generar(TipoAviso.SegundoRecordatorio, Datos.CrearCliente(), factura);

        Assert.Contains("1.250,75 USD", contenido.CuerpoTexto, StringComparison.Ordinal);
    }

    [Fact]
    public void Generar_MuestraElVencimientoEnLaZonaHorariaConfigurada()
    {
        // 03:00 UTC del 1 de octubre son las 22:00 del 30 de septiembre en Bogotá (UTC-5).
        var factura = Datos.CrearFactura(
            "FAC-0001", fechaVencimiento: new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc));

        var contenido = _plantilla.Generar(TipoAviso.SegundoRecordatorio, Datos.CrearCliente(), factura);

        Assert.Contains("30/09/2026", contenido.CuerpoTexto, StringComparison.Ordinal);
    }

    [Fact]
    public void Generar_EscapaElHtmlDeLosDatosDelCliente()
    {
        var cliente = Datos.CrearCliente(nombre: "Andes & Hijos <script>");

        var contenido = _plantilla.Generar(TipoAviso.SegundoRecordatorio, cliente, Datos.CrearFactura("F1"));

        Assert.Contains("Andes &amp; Hijos &lt;script&gt;", contenido.CuerpoHtml, StringComparison.Ordinal);
        Assert.DoesNotContain("<script>", contenido.CuerpoHtml, StringComparison.Ordinal);
        Assert.Contains("Andes & Hijos <script>", contenido.CuerpoTexto, StringComparison.Ordinal);
    }

    [Fact]
    public void Generar_ElTextoPlanoNoContieneEtiquetasHtml()
    {
        var contenido = _plantilla.Generar(TipoAviso.Desactivacion, Datos.CrearCliente(), Datos.CrearFactura("F1"));

        Assert.DoesNotContain("<", contenido.CuerpoTexto, StringComparison.Ordinal);
        Assert.StartsWith("<!DOCTYPE html>", contenido.CuerpoHtml, StringComparison.Ordinal);
    }

    [Fact]
    public void Generar_ConAvisoNoDefinido_LanzaArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _plantilla.Generar((TipoAviso)99, Datos.CrearCliente(), Datos.CrearFactura("F1")));
    }
}
