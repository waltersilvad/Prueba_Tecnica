using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Application.Recordatorios;
using Facturacion.Infrastructure.Persistencia;
using Facturacion.IntegrationTests.Infraestructura;

namespace Facturacion.IntegrationTests.Persistencia;

[Collection(ColeccionMongo.Nombre)]
public sealed class DatosPruebaTests(MongoFixture mongo)
{
    [Fact]
    public async Task BorrarTodo_EliminaClientesFacturasEnviosYSoloLosCorreosEml()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        await entorno.Obtener<ProcesarRecordatoriosUseCase>().EjecutarAsync(EntornoPruebas.IdCliente3, CancellationToken.None);
        var otroArchivo = Path.Combine(entorno.CarpetaCorreos, "nota.txt");
        await File.WriteAllTextAsync(otroArchivo, "no es un correo");

        var resultado = await entorno.Obtener<IDatosPrueba>().BorrarTodoAsync(CancellationToken.None);

        Assert.Equal(new ResumenBorrado(Clientes: 3, Facturas: 10, Envios: 3, Correos: 3), resultado.Valor);
        Assert.Empty(await entorno.Obtener<IClienteRepository>().ObtenerTodosAsync(CancellationToken.None));
        Assert.Empty((await entorno.Obtener<IFacturaRepository>().BuscarAsync(new FiltroFacturas(), CancellationToken.None)).Facturas);
        Assert.Empty(await entorno.Obtener<IEnvioRepository>().ObtenerAsync(null, CancellationToken.None));
        Assert.Empty(entorno.ArchivosDeCorreo());
        Assert.True(File.Exists(otroArchivo), "Solo se borran archivos .eml.");
    }

    [Fact]
    public async Task BorrarTodo_ConUnProcesoEnEjecucion_FallaSinTocarLosDatos()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        using (entorno.Obtener<IBloqueoEjecucion>().IntentarAdquirir())
        {
            var resultado = await entorno.Obtener<IDatosPrueba>().BorrarTodoAsync(CancellationToken.None);

            Assert.Equal(ErroresAplicacion.EjecucionEnCurso, resultado.Error);
        }

        Assert.Equal(3, (await entorno.Obtener<IClienteRepository>().ObtenerTodosAsync(CancellationToken.None)).Count);
    }

    [Fact]
    public async Task CargarEjemplo_ConLaBaseVacia_CargaLosClientesYFacturasDeEjemplo()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        var datos = entorno.Obtener<IDatosPrueba>();
        await datos.BorrarTodoAsync(CancellationToken.None);

        var resultado = await datos.CargarEjemploAsync(CancellationToken.None);

        Assert.Equal(new ResumenCargaEjemplo(Clientes: 3, Facturas: 10), resultado.Valor);
        Assert.Equal(10, (await entorno.Obtener<IFacturaRepository>().BuscarAsync(new FiltroFacturas(), CancellationToken.None)).Facturas.Count);
    }

    [Fact]
    public async Task CargarEjemplo_ConDatos_FallaConBaseConDatos()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        var resultado = await entorno.Obtener<IDatosPrueba>().CargarEjemploAsync(CancellationToken.None);

        Assert.Equal(ErroresDatosPrueba.BaseConDatos, resultado.Error);
    }

    [Fact]
    public async Task EliminarCliente_BorraSoloLosDatosDeEseCliente()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);
        await entorno.Obtener<ProcesarRecordatoriosUseCase>().EjecutarAsync(null, CancellationToken.None);

        var resultado = await entorno.Obtener<EliminarClienteUseCase>().EjecutarAsync(EntornoPruebas.IdCliente2, CancellationToken.None);

        Assert.Equal(3, resultado.Valor.FacturasEliminadas);
        Assert.Equal(3, resultado.Valor.EnviosEliminados);
        Assert.Equal(2, (await entorno.Obtener<IClienteRepository>().ObtenerTodosAsync(CancellationToken.None)).Count);
        Assert.Equal(7, (await entorno.Obtener<IFacturaRepository>().BuscarAsync(new FiltroFacturas(), CancellationToken.None)).Facturas.Count);
        Assert.Equal(6, (await entorno.Obtener<IEnvioRepository>().ObtenerAsync(null, CancellationToken.None)).Count);
    }

    [Fact]
    public async Task Procesar_ConUnBorradoEnCurso_FallaPorEjecucionEnCurso()
    {
        await using var entorno = await EntornoPruebas.CrearAsync(mongo);

        using (entorno.Obtener<IBloqueoEjecucion>().IntentarAdquirir())
        {
            var resultado = await entorno.Obtener<ProcesarRecordatoriosUseCase>().EjecutarAsync(null, CancellationToken.None);

            Assert.Equal(ErroresAplicacion.EjecucionEnCurso, resultado.Error);
        }

        var libre = await entorno.Obtener<ProcesarRecordatoriosUseCase>().EjecutarAsync(null, CancellationToken.None);
        Assert.True(libre.EsExitoso);
    }
}
