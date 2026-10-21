using Facturacion.Application.Clientes;
using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Application.Recordatorios;
using Facturacion.Domain.Facturas;
using Facturacion.IntegrationTests.Infraestructura;
using MimeKit;

namespace Facturacion.IntegrationTests.Recordatorios;

/// <summary>
/// Proceso de recordatorios de punta a punta: Mongo real + proveedor de correo Consola.
/// </summary>
[Collection(ColeccionMongo.Nombre)]
public sealed class CicloCompletoTests(MongoFixture mongo)
{
    [Fact]
    public async Task Procesar_TresVeces_RecorreElCicloDeEstadosHastaDesactivarTodo()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        var procesar = entorno.Obtener<ProcesarRecordatoriosUseCase>();

        // 1.ª: 6 en primer → segundo y 3 en segundo → desactivado. FAC-1004 ya estaba desactivada.
        var primera = (await procesar.EjecutarAsync(null, CancellationToken.None)).Valor;
        Assert.Equal(9, primera.Clientes.Sum(c => c.CorreosEnviados));
        Assert.Equal(9, primera.Clientes.Sum(c => c.FacturasActualizadas));
        Assert.Equal(["FAC-1004"], primera.Clientes.SelectMany(c => c.Omitidas).Select(o => o.Numero));
        Assert.All(primera.Clientes, c => Assert.Empty(c.Errores));
        Assert.All(primera.Clientes, c => Assert.Empty(c.Conflictos));

        // 2.ª: las 6 en segundo → desactivado.
        var segunda = (await procesar.EjecutarAsync(null, CancellationToken.None)).Valor;
        Assert.Equal(6, segunda.Clientes.Sum(c => c.CorreosEnviados));
        Assert.Equal(4, segunda.Clientes.Sum(c => c.Omitidas.Count));

        // 3.ª: no queda nada por avisar.
        var tercera = (await procesar.EjecutarAsync(null, CancellationToken.None)).Valor;
        Assert.Equal(0, tercera.Clientes.Sum(c => c.CorreosEnviados));
        Assert.Equal(10, tercera.Clientes.Sum(c => c.Omitidas.Count));

        var facturas = await entorno.Obtener<IFacturaRepository>().BuscarAsync(new FiltroFacturas(), CancellationToken.None);
        Assert.All(facturas.Facturas, f => Assert.Equal(EstadoFactura.Desactivado, f.Estado));
        Assert.Equal(15, entorno.ArchivosDeCorreo().Length);
        Assert.Equal(15, (await entorno.Obtener<IEnvioRepository>().ObtenerAsync(null, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task Procesar_GuardaCadaAvisoComoEmlConHtmlYTextoDirigidoAlCliente()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        await entorno.Obtener<ProcesarRecordatoriosUseCase>().EjecutarAsync(EntornoPruebas.IdCliente3, CancellationToken.None);

        var archivos = entorno.ArchivosDeCorreo();
        Assert.Equal(3, archivos.Length);

        using var correo = await MimeMessage.LoadAsync(archivos.Single(a => File.ReadAllText(a).Contains("FAC-3001", StringComparison.Ordinal)));
        Assert.Equal("tesoreria.caribe@example.com", Assert.Single(correo.To.Mailboxes).Address);
        Assert.Equal("avisos@example.com", Assert.Single(correo.From.Mailboxes).Address);
        Assert.Equal("Segundo recordatorio: factura FAC-3001 pendiente de pago", correo.Subject);
        Assert.Contains("3.450.000,00 COP", correo.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("15/10/2026", correo.TextBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Procesar_TrasEditarElCorreoDelCliente_EnviaAlNuevoCorreo()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        await entorno.Obtener<ActualizarClienteUseCase>().EjecutarAsync(
            EntornoPruebas.IdCliente2,
            new ActualizarClienteComando("Distribuidora del Sur Ltda.", "evaluador@example.com"),
            CancellationToken.None);

        var resultado = await entorno.Obtener<ProcesarRecordatoriosUseCase>()
            .EjecutarAsync(EntornoPruebas.IdCliente2, CancellationToken.None);

        Assert.Equal(3, Assert.Single(resultado.Valor.Clientes).CorreosEnviados);
        var envios = await entorno.Obtener<IEnvioRepository>().ObtenerAsync(EntornoPruebas.IdCliente2, CancellationToken.None);
        Assert.Equal(3, envios.Count);
        Assert.All(envios, e => Assert.Equal("evaluador@example.com", e.Destinatario));
        Assert.All(envios, e => Assert.Equal(resultado.Valor.EjecucionId, e.EjecucionId));
    }
}
