using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Application.Resumen;
using Facturacion.Domain.Facturas;
using Facturacion.IntegrationTests.Infraestructura;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Facturacion.IntegrationTests.Persistencia;

[Collection(ColeccionMongo.Nombre)]
public sealed class RepositoriosMongoTests(MongoFixture mongo)
{
    private static readonly DateTime Ahora = new(2026, 10, 21, 15, 0, 0, DateTimeKind.Utc);

    // ---------- Facturas ----------

    [Fact]
    public async Task BuscarAsync_ConFiltrosDeClienteYEstado_DevuelveSoloLasQueCoinciden()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        var repositorio = entorno.Obtener<IFacturaRepository>();

        var lectura = await repositorio.BuscarAsync(
            new FiltroFacturas(EntornoPruebas.IdCliente1, EstadoFactura.PrimerRecordatorio), CancellationToken.None);

        Assert.Equal(["FAC-1001", "FAC-1002"], lectura.Facturas.Select(f => f.Numero));
        Assert.All(lectura.Facturas, f => Assert.Equal(EntornoPruebas.IdCliente1, f.ClienteId));
        Assert.Empty(lectura.NoInterpretables);
    }

    [Fact]
    public async Task BuscarAsync_MapeaMontoDecimalYFechasUtc()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        var lectura = await entorno.Obtener<IFacturaRepository>().BuscarAsync(new FiltroFacturas(), CancellationToken.None);

        var factura = lectura.Facturas.Single(f => f.Numero == "FAC-2002");
        Assert.Equal(1_575_250.75m, factura.Monto);
        Assert.Equal("COP", factura.Moneda);
        Assert.Equal(new DateTime(2026, 9, 20, 5, 0, 0, DateTimeKind.Utc), factura.FechaVencimiento);
        Assert.Equal(DateTimeKind.Utc, factura.FechaVencimiento.Kind);
        Assert.Equal(EstadoFactura.SegundoRecordatorio, factura.Estado);
    }

    [Fact]
    public async Task BuscarAsync_ConClienteIdNoValido_DevuelveVacio()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        var lectura = await entorno.Obtener<IFacturaRepository>()
            .BuscarAsync(new FiltroFacturas("no-es-un-objectid"), CancellationToken.None);

        Assert.Empty(lectura.Facturas);
    }

    [Fact]
    public async Task BuscarAsync_DocumentoConEstadoDesconocido_SeDevuelveComoNoInterpretable()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        // Se omite la validación del esquema para simular un dato que llegó por fuera de la aplicación.
        await entorno.Base.GetCollection<BsonDocument>("facturas").InsertOneAsync(
            new BsonDocument
            {
                { "numero", "FAC-9999" },
                { "clienteId", ObjectId.Parse(EntornoPruebas.IdCliente3) },
                { "monto", new BsonDecimal128(100m) },
                { "moneda", "COP" },
                { "fechaEmision", Ahora },
                { "fechaVencimiento", Ahora },
                { "estado", "pagada" },
                { "fechaActualizacion", Ahora },
            },
            new InsertOneOptions { BypassDocumentValidation = true });

        var lectura = await entorno.Obtener<IFacturaRepository>()
            .BuscarAsync(new FiltroFacturas(EntornoPruebas.IdCliente3), CancellationToken.None);

        Assert.Equal(3, lectura.Facturas.Count);
        var noInterpretable = Assert.Single(lectura.NoInterpretables);
        Assert.Equal("FAC-9999", noInterpretable.Numero);
        Assert.Equal(ErroresFactura.EstadoDesconocido("pagada"), noInterpretable.Motivo);
    }

    [Fact]
    public async Task ActualizarEstadoAsync_ConElEstadoEsperadoVigente_GuardaYDevuelveTrue()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        var repositorio = entorno.Obtener<IFacturaRepository>();
        var factura = await ObtenerFacturaAsync(repositorio, "FAC-1001");

        factura.AvanzarEstado(Ahora);
        var actualizada = await repositorio.ActualizarEstadoAsync(factura, EstadoFactura.PrimerRecordatorio, CancellationToken.None);

        Assert.True(actualizada);
        var guardada = await ObtenerFacturaAsync(repositorio, "FAC-1001");
        Assert.Equal(EstadoFactura.SegundoRecordatorio, guardada.Estado);
        Assert.Equal(Ahora, guardada.FechaActualizacion);
    }

    [Fact]
    public async Task ActualizarEstadoAsync_SiOtraEjecucionCambioElEstado_DevuelveFalseSinModificar()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        var repositorio = entorno.Obtener<IFacturaRepository>();
        var factura = await ObtenerFacturaAsync(repositorio, "FAC-1001");

        // Otra ejecución avanza la misma factura después de que esta la leyó.
        var otraCopia = await ObtenerFacturaAsync(repositorio, "FAC-1001");
        otraCopia.AvanzarEstado(Ahora);
        Assert.True(await repositorio.ActualizarEstadoAsync(otraCopia, EstadoFactura.PrimerRecordatorio, CancellationToken.None));

        factura.AvanzarEstado(Ahora.AddMinutes(1));
        var actualizada = await repositorio.ActualizarEstadoAsync(factura, EstadoFactura.PrimerRecordatorio, CancellationToken.None);

        Assert.False(actualizada);
        Assert.Equal(Ahora, (await ObtenerFacturaAsync(repositorio, "FAC-1001")).FechaActualizacion);
    }

    [Fact]
    public async Task IntentarAgregarAsync_GuardaLaFacturaNuevaYRechazaUnNumeroRepetido()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        var repositorio = entorno.Obtener<IFacturaRepository>();
        var generador = entorno.Obtener<IGeneradorId>();
        Factura Nueva(string numero) => Factura.Registrar(
            generador.NuevoId(), numero, EntornoPruebas.IdCliente2, 1_000m, "USD", Ahora, Ahora.AddDays(30), Ahora).Valor;

        Assert.True(await repositorio.IntentarAgregarAsync(Nueva("FAC-7001"), CancellationToken.None));
        Assert.False(await repositorio.IntentarAgregarAsync(Nueva("FAC-7001"), CancellationToken.None));

        var guardada = await ObtenerFacturaAsync(repositorio, "FAC-7001");
        Assert.Equal(EstadoFactura.PrimerRecordatorio, guardada.Estado);
        Assert.Equal("USD", guardada.Moneda);
    }

    [Fact]
    public async Task BuscarAsync_PorIdDeFactura_DevuelveSoloEsaFactura()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        var repositorio = entorno.Obtener<IFacturaRepository>();
        var objetivo = await ObtenerFacturaAsync(repositorio, "FAC-2002");

        var lectura = await repositorio.BuscarAsync(new FiltroFacturas(FacturaIds: [objetivo.Id, "no-valido"]), CancellationToken.None);

        Assert.Equal("FAC-2002", Assert.Single(lectura.Facturas).Numero);
        Assert.Empty((await repositorio.BuscarAsync(new FiltroFacturas(FacturaIds: ["no-valido"]), CancellationToken.None)).Facturas);
    }

    [Fact]
    public async Task CrearCliente_LoGuardaEnMongo()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        var creado = await entorno.Obtener<CrearClienteUseCase>().EjecutarAsync(
            new CrearClienteComando("Industrias Pacífico", "pagos@pacifico.example.com"), CancellationToken.None);

        var guardado = await entorno.Obtener<IClienteRepository>().ObtenerPorIdAsync(creado.Valor.Id, CancellationToken.None);
        Assert.Equal("Industrias Pacífico", guardado!.Nombre);
    }

    [Fact]
    public async Task ResumenDeClientes_ConLaSemilla_AgregaConteosYMontosDesdeMongo()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        var resumen = await entorno.Obtener<ObtenerResumenClientesUseCase>().EjecutarAsync(CancellationToken.None);

        Assert.Equal(
            [
                new ResumenEsperado(EntornoPruebas.IdCliente1, 2, 1, 1, 4, 4_395_500.50m),
                new ResumenEsperado(EntornoPruebas.IdCliente2, 1, 2, 0, 3, 2_865_250.75m),
                new ResumenEsperado(EntornoPruebas.IdCliente3, 3, 0, 0, 3, 4_335_990.00m),
            ],
            resumen.Select(r => new ResumenEsperado(
                r.ClienteId,
                r.PrimerRecordatorio,
                r.SegundoRecordatorio,
                r.Desactivado,
                r.TotalFacturas,
                Assert.Single(r.MontoPendiente, m => m.Moneda == "COP").Monto)));
    }

    // ---------- Clientes ----------

    [Fact]
    public async Task ActualizarCliente_GuardaNombreYCorreoEnMongo()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        var resultado = await entorno.Obtener<ActualizarClienteUseCase>().EjecutarAsync(
            EntornoPruebas.IdCliente2,
            new ActualizarClienteComando("Distribuidora del Sur S.A.", "evaluador@example.com"),
            CancellationToken.None);

        Assert.True(resultado.EsExitoso);
        var guardado = await entorno.Obtener<IClienteRepository>()
            .ObtenerPorIdAsync(EntornoPruebas.IdCliente2, CancellationToken.None);
        Assert.Equal("Distribuidora del Sur S.A.", guardado!.Nombre);
        Assert.Equal("evaluador@example.com", guardado.Email);
    }

    [Fact]
    public async Task ObtenerPorIdAsync_ConIdNoValido_DevuelveNull()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        Assert.Null(await entorno.Obtener<IClienteRepository>().ObtenerPorIdAsync("123", CancellationToken.None));
    }

    [Fact]
    public async Task ObtenerTodosAsync_DevuelveLosClientesOrdenadosPorNombre()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        var clientes = await entorno.Obtener<IClienteRepository>().ObtenerTodosAsync(CancellationToken.None);

        Assert.Equal(
            ["Comercial Andina S.A.S.", "Distribuidora del Sur Ltda.", "Tecnología Caribe S.A.S."],
            clientes.Select(c => c.Nombre));
    }

    // ---------- Envíos ----------

    [Fact]
    public async Task Envios_SeRegistranYConsultanDelMasRecienteAlMasAntiguoPorCliente()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        var repositorio = entorno.Obtener<IEnvioRepository>();
        var anterior = Envio(EntornoPruebas.IdCliente1, Ahora, ResultadoEnvio.Fallido, "SMTP caído");
        var reciente = Envio(EntornoPruebas.IdCliente1, Ahora.AddMinutes(5), ResultadoEnvio.Enviado, null);
        var deOtroCliente = Envio(EntornoPruebas.IdCliente2, Ahora, ResultadoEnvio.Enviado, null);

        await repositorio.RegistrarAsync(anterior, CancellationToken.None);
        await repositorio.RegistrarAsync(reciente, CancellationToken.None);
        await repositorio.RegistrarAsync(deOtroCliente, CancellationToken.None);

        Assert.Equal([reciente, anterior], await repositorio.ObtenerAsync(EntornoPruebas.IdCliente1, CancellationToken.None));
        Assert.Equal(3, (await repositorio.ObtenerAsync(null, CancellationToken.None)).Count);
        Assert.Empty(await repositorio.ObtenerAsync("no-valido", CancellationToken.None));
    }

    private static RegistroEnvio Envio(string clienteId, DateTime fecha, ResultadoEnvio resultado, string? error) =>
        new("ejecucion-1", clienteId, "6716a2000000000000001001", TipoAviso.SegundoRecordatorio, "pagos@example.com", fecha, resultado, error);

    private static async Task<Factura> ObtenerFacturaAsync(IFacturaRepository repositorio, string numero) =>
        (await repositorio.BuscarAsync(new FiltroFacturas(), CancellationToken.None)).Facturas.Single(f => f.Numero == numero);

    private sealed record ResumenEsperado(
        string ClienteId, int Primer, int Segundo, int Desactivado, int Total, decimal PendienteCop);
}
