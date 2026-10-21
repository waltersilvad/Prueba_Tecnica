using Facturacion.Domain.Facturas;

namespace Facturacion.Domain.Tests.Facturas;

public sealed class FacturaTests
{
    private static readonly DateTime Ahora = new(2026, 10, 21, 15, 30, 0, DateTimeKind.Utc);

    public static TheoryData<decimal> MontosNoPositivos => [0m, -0.01m, -150_000m];

    // ---------- Crear ----------

    [Fact]
    public void Crear_ConDatosValidos_DevuelveLaFacturaConSusDatos()
    {
        var resultado = FacturaPrueba.Crear(estado: EstadoFactura.SegundoRecordatorio);

        Assert.True(resultado.EsExitoso);
        var factura = resultado.Valor;
        Assert.Equal("factura-1", factura.Id);
        Assert.Equal("FAC-0001", factura.Numero);
        Assert.Equal("cliente-1", factura.ClienteId);
        Assert.Equal(150_000.50m, factura.Monto);
        Assert.Equal("COP", factura.Moneda);
        Assert.Equal(FacturaPrueba.FechaEmision, factura.FechaEmision);
        Assert.Equal(FacturaPrueba.FechaVencimiento, factura.FechaVencimiento);
        Assert.Equal(EstadoFactura.SegundoRecordatorio, factura.Estado);
        Assert.Equal(FacturaPrueba.FechaEmision, factura.FechaActualizacion);
    }

    [Theory]
    [MemberData(nameof(MontosNoPositivos))]
    public void Crear_ConMontoNoPositivo_FallaConMontoNoPositivo(decimal monto)
    {
        var resultado = FacturaPrueba.Crear(monto: monto);

        Assert.True(resultado.EsFallido);
        Assert.Equal(ErroresFactura.MontoNoPositivo, resultado.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinId_FallaConCampoRequerido(string id)
    {
        var resultado = FacturaPrueba.Crear(id: id);

        Assert.Equal(ErroresFactura.CampoRequerido(nameof(Factura.Id)), resultado.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNumero_FallaConCampoRequerido(string numero)
    {
        var resultado = FacturaPrueba.Crear(numero: numero);

        Assert.Equal(ErroresFactura.CampoRequerido(nameof(Factura.Numero)), resultado.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinCliente_FallaConCampoRequerido(string clienteId)
    {
        var resultado = FacturaPrueba.Crear(clienteId: clienteId);

        Assert.Equal(ErroresFactura.CampoRequerido(nameof(Factura.ClienteId)), resultado.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinMoneda_FallaConCampoRequerido(string moneda)
    {
        var resultado = FacturaPrueba.Crear(moneda: moneda);

        Assert.Equal(ErroresFactura.CampoRequerido(nameof(Factura.Moneda)), resultado.Error);
    }

    [Fact]
    public void Crear_ConVencimientoAnteriorAEmision_FallaConVencimientoAnteriorAEmision()
    {
        var resultado = FacturaPrueba.Crear(fechaVencimiento: FacturaPrueba.FechaEmision.AddDays(-1));

        Assert.True(resultado.EsFallido);
        Assert.Equal(ErroresFactura.VencimientoAnteriorAEmision, resultado.Error);
    }

    [Fact]
    public void Crear_ConVencimientoIgualAEmision_EsValida()
    {
        var resultado = FacturaPrueba.Crear(fechaVencimiento: FacturaPrueba.FechaEmision);

        Assert.True(resultado.EsExitoso);
    }

    [Fact]
    public void Crear_ConEstadoNoDefinido_FallaConEstadoInvalido()
    {
        var resultado = FacturaPrueba.Crear(estado: (EstadoFactura)99);

        Assert.True(resultado.EsFallido);
        Assert.Equal(ErroresFactura.EstadoInvalido, resultado.Error);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Crear_ConFechaEmisionNoUtc_LanzaArgumentException(DateTimeKind tipo)
    {
        var fecha = DateTime.SpecifyKind(FacturaPrueba.FechaEmision, tipo);

        Assert.Throws<ArgumentException>(() => FacturaPrueba.Crear(fechaEmision: fecha));
    }

    [Fact]
    public void Crear_ConFechaVencimientoNoUtc_LanzaArgumentException()
    {
        var fecha = DateTime.SpecifyKind(FacturaPrueba.FechaVencimiento, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(() => FacturaPrueba.Crear(fechaVencimiento: fecha));
    }

    [Fact]
    public void Crear_ConFechaActualizacionNoUtc_LanzaArgumentException()
    {
        var fecha = DateTime.SpecifyKind(FacturaPrueba.FechaEmision, DateTimeKind.Unspecified);

        Assert.Throws<ArgumentException>(() => FacturaPrueba.Crear(fechaActualizacion: fecha));
    }

    [Theory]
    [InlineData("cop")]
    [InlineData("PESOS")]
    [InlineData("CO")]
    public void Crear_ConMonedaQueNoEsCodigoIso_FallaConMonedaInvalida(string moneda)
    {
        var resultado = FacturaPrueba.Crear(moneda: moneda);

        Assert.Equal(ErroresFactura.MonedaInvalida, resultado.Error);
    }

    // ---------- Registrar (factura nueva) ----------

    [Fact]
    public void Registrar_CreaLaFacturaEnPrimerRecordatorioConLaFechaDeRegistro()
    {
        var resultado = Factura.Registrar(
            "factura-9", " FAC-9001 ", "cliente-1", 500_000m, "COP",
            FacturaPrueba.FechaEmision, FacturaPrueba.FechaVencimiento, Ahora);

        Assert.True(resultado.EsExitoso);
        var factura = resultado.Valor;
        Assert.Equal(EstadoFactura.PrimerRecordatorio, factura.Estado);
        Assert.Equal(Ahora, factura.FechaActualizacion);
        Assert.Equal("FAC-9001", factura.Numero);
        Assert.Equal(TipoAviso.SegundoRecordatorio, factura.DeterminarAviso().Valor);
    }

    [Fact]
    public void Registrar_AplicaLasMismasValidacionesQueCrear()
    {
        var resultado = Factura.Registrar(
            "factura-9", "FAC-9001", "cliente-1", 0m, "COP",
            FacturaPrueba.FechaEmision, FacturaPrueba.FechaVencimiento, Ahora);

        Assert.Equal(ErroresFactura.MontoNoPositivo, resultado.Error);
    }

    [Fact]
    public void Registrar_ConNumeroVacio_FallaConCampoRequerido()
    {
        var resultado = Factura.Registrar(
            "factura-9", "   ", "cliente-1", 10m, "COP",
            FacturaPrueba.FechaEmision, FacturaPrueba.FechaVencimiento, Ahora);

        Assert.Equal(ErroresFactura.CampoRequerido(nameof(Factura.Numero)), resultado.Error);
    }

    // ---------- DeterminarAviso ----------

    [Theory]
    [InlineData(EstadoFactura.PrimerRecordatorio, TipoAviso.SegundoRecordatorio)]
    [InlineData(EstadoFactura.SegundoRecordatorio, TipoAviso.Desactivacion)]
    public void DeterminarAviso_EnEstadoProcesable_DevuelveElAvisoQueCorresponde(
        EstadoFactura estado, TipoAviso avisoEsperado)
    {
        var factura = FacturaPrueba.CrearValida(estado);

        var resultado = factura.DeterminarAviso();

        Assert.True(resultado.EsExitoso);
        Assert.Equal(avisoEsperado, resultado.Valor);
    }

    [Fact]
    public void DeterminarAviso_EnDesactivado_FallaConMotivoEstadoFinal()
    {
        var factura = FacturaPrueba.CrearValida(EstadoFactura.Desactivado);

        var resultado = factura.DeterminarAviso();

        Assert.True(resultado.EsFallido);
        Assert.Equal(ErroresFactura.EstadoFinal, resultado.Error);
    }

    // ---------- AvanzarEstado ----------

    [Theory]
    [InlineData(EstadoFactura.PrimerRecordatorio, EstadoFactura.SegundoRecordatorio)]
    [InlineData(EstadoFactura.SegundoRecordatorio, EstadoFactura.Desactivado)]
    public void AvanzarEstado_EnEstadoProcesable_PasaAlSiguienteYRegistraLaFecha(
        EstadoFactura estadoInicial, EstadoFactura estadoEsperado)
    {
        var factura = FacturaPrueba.CrearValida(estadoInicial);

        var resultado = factura.AvanzarEstado(Ahora);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(estadoEsperado, factura.Estado);
        Assert.Equal(Ahora, factura.FechaActualizacion);
    }

    [Fact]
    public void AvanzarEstado_EnDesactivado_FallaYNoModificaLaFactura()
    {
        var factura = FacturaPrueba.CrearValida(EstadoFactura.Desactivado);
        var fechaAnterior = factura.FechaActualizacion;

        var resultado = factura.AvanzarEstado(Ahora);

        Assert.True(resultado.EsFallido);
        Assert.Equal(ErroresFactura.EstadoFinal, resultado.Error);
        Assert.Equal(EstadoFactura.Desactivado, factura.Estado);
        Assert.Equal(fechaAnterior, factura.FechaActualizacion);
    }

    [Fact]
    public void AvanzarEstado_CicloCompleto_TerminaEnDesactivadoYNoAvanzaMas()
    {
        var factura = FacturaPrueba.CrearValida(EstadoFactura.PrimerRecordatorio);

        Assert.True(factura.AvanzarEstado(Ahora).EsExitoso);
        Assert.True(factura.AvanzarEstado(Ahora.AddDays(1)).EsExitoso);
        Assert.True(factura.AvanzarEstado(Ahora.AddDays(2)).EsFallido);

        Assert.Equal(EstadoFactura.Desactivado, factura.Estado);
        Assert.Equal(Ahora.AddDays(1), factura.FechaActualizacion);
    }

    [Fact]
    public void AvanzarEstado_ConFechaNoUtc_LanzaArgumentExceptionYNoModificaLaFactura()
    {
        var factura = FacturaPrueba.CrearValida(EstadoFactura.PrimerRecordatorio);

        Assert.Throws<ArgumentException>(() => factura.AvanzarEstado(DateTime.SpecifyKind(Ahora, DateTimeKind.Local)));
        Assert.Equal(EstadoFactura.PrimerRecordatorio, factura.Estado);
    }
}
