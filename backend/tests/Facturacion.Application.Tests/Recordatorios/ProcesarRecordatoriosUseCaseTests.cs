using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Application.Correo;
using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Application.Recordatorios;
using Facturacion.Domain.Clientes;
using Facturacion.Domain.Facturas;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Facturacion.Application.Tests.Recordatorios;

public sealed class ProcesarRecordatoriosUseCaseTests
{
    private readonly IClienteRepository _clientes = Substitute.For<IClienteRepository>();
    private readonly IFacturaRepository _facturas = Substitute.For<IFacturaRepository>();
    private readonly IEnvioRepository _envios = Substitute.For<IEnvioRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly IPlantillaCorreo _plantilla = Substitute.For<IPlantillaCorreo>();
    private readonly IReloj _reloj = Substitute.For<IReloj>();
    private readonly IBloqueoEjecucion _bloqueo = Substitute.For<IBloqueoEjecucion>();
    private readonly IDisposable _liberadorBloqueo = Substitute.For<IDisposable>();
    private readonly ProcesarRecordatoriosUseCase _useCase;

    public ProcesarRecordatoriosUseCaseTests()
    {
        _reloj.AhoraUtc.Returns(Datos.Ahora);
        _bloqueo.IntentarAdquirir().Returns(_liberadorBloqueo);
        _plantilla
            .Generar(Arg.Any<TipoAviso>(), Arg.Any<Cliente>(), Arg.Any<Factura>())
            .Returns(llamada => new ContenidoCorreo(
                $"Asunto {llamada.Arg<Factura>().Numero}",
                $"<p>{llamada.Arg<TipoAviso>()}</p>",
                $"Texto {llamada.Arg<TipoAviso>()}"));
        _facturas
            .ActualizarEstadoAsync(Arg.Any<Factura>(), Arg.Any<EstadoFactura>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // El enviador es real (con los mismos dobles de los puertos): las pruebas cubren el flujo completo de cada aviso.
        var enviador = new EnviadorAvisoFactura(
            _facturas,
            _envios,
            _emailSender,
            _plantilla,
            _reloj,
            NullLogger<EnviadorAvisoFactura>.Instance);

        _useCase = new ProcesarRecordatoriosUseCase(
            _clientes,
            _facturas,
            _bloqueo,
            enviador,
            NullLogger<ProcesarRecordatoriosUseCase>.Instance);
    }

    // ---------- Flujo principal ----------

    [Fact]
    public async Task EjecutarAsync_FlujoCompleto_EnviaUnCorreoPorFacturaYAvanzaSusEstados()
    {
        var cliente = Datos.CrearCliente();
        var primera = Datos.CrearFactura("F1", EstadoFactura.PrimerRecordatorio);
        var segunda = Datos.CrearFactura("F2", EstadoFactura.SegundoRecordatorio);
        ConClientes(cliente);
        ConFacturas(cliente, LecturaFacturas.Con(primera, segunda));

        var resultado = await _useCase.EjecutarAsync(null, CancellationToken.None);

        Assert.True(resultado.EsExitoso);
        var resultadoCliente = Assert.Single(resultado.Valor.Clientes);
        Assert.Equal(cliente.Id, resultadoCliente.ClienteId);
        Assert.Equal(cliente.Nombre, resultadoCliente.Nombre);
        Assert.Equal(2, resultadoCliente.CorreosEnviados);
        Assert.Equal(2, resultadoCliente.FacturasActualizadas);
        Assert.Empty(resultadoCliente.Omitidas);
        Assert.Empty(resultadoCliente.Errores);
        Assert.Empty(resultadoCliente.Conflictos);

        Assert.Equal(EstadoFactura.SegundoRecordatorio, primera.Estado);
        Assert.Equal(EstadoFactura.Desactivado, segunda.Estado);
        Assert.Equal(Datos.Ahora, primera.FechaActualizacion);
        await _facturas.Received(1).ActualizarEstadoAsync(primera, EstadoFactura.PrimerRecordatorio, Arg.Any<CancellationToken>());
        await _facturas.Received(1).ActualizarEstadoAsync(segunda, EstadoFactura.SegundoRecordatorio, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_GeneraCadaCorreoConLaPlantillaDelAvisoQueCorresponde()
    {
        var cliente = Datos.CrearCliente();
        var primera = Datos.CrearFactura("F1", EstadoFactura.PrimerRecordatorio);
        var segunda = Datos.CrearFactura("F2", EstadoFactura.SegundoRecordatorio);
        ConClientes(cliente);
        ConFacturas(cliente, LecturaFacturas.Con(primera, segunda));

        await _useCase.EjecutarAsync(null, CancellationToken.None);

        _plantilla.Received(1).Generar(TipoAviso.SegundoRecordatorio, cliente, primera);
        _plantilla.Received(1).Generar(TipoAviso.Desactivacion, cliente, segunda);
        await _emailSender.Received(1).EnviarAsync(
            Arg.Is<MensajeCorreo>(m =>
                m.Asunto == "Asunto F2" && m.CuerpoHtml == "<p>Desactivacion</p>" && m.CuerpoTexto == "Texto Desactivacion"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_DirigeCadaCorreoAlEmailDelClienteDuenoDeLaFactura()
    {
        var andina = Datos.CrearCliente("cliente-1", "Comercial Andina", "andina@example.com");
        var sur = Datos.CrearCliente("cliente-2", "Distribuidora Sur", "sur@example.com");
        ConClientes(andina, sur);
        ConFacturas(andina, LecturaFacturas.Con(Datos.CrearFactura("F1", clienteId: andina.Id)));
        ConFacturas(sur, LecturaFacturas.Con(Datos.CrearFactura("F2", clienteId: sur.Id)));

        await _useCase.EjecutarAsync(null, CancellationToken.None);

        await _emailSender.Received(2).EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>());
        await _emailSender.Received(1).EnviarAsync(
            Arg.Is<MensajeCorreo>(m =>
                m.Destinatario == "andina@example.com" && m.NombreDestinatario == "Comercial Andina" && m.Asunto == "Asunto F1"),
            Arg.Any<CancellationToken>());
        await _emailSender.Received(1).EnviarAsync(
            Arg.Is<MensajeCorreo>(m =>
                m.Destinatario == "sur@example.com" && m.NombreDestinatario == "Distribuidora Sur" && m.Asunto == "Asunto F2"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_RegistraElEnvioExitosoConLosDatosDeLaEjecucion()
    {
        var cliente = Datos.CrearCliente();
        ConClientes(cliente);
        ConFacturas(cliente, LecturaFacturas.Con(Datos.CrearFactura("F1")));

        var resultado = await _useCase.EjecutarAsync(null, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(resultado.Valor.EjecucionId));
        await _envios.Received(1).RegistrarAsync(
            new RegistroEnvio(
                resultado.Valor.EjecucionId,
                cliente.Id,
                "id-F1",
                TipoAviso.SegundoRecordatorio,
                cliente.Email,
                Datos.Ahora,
                ResultadoEnvio.Enviado,
                null),
            Arg.Any<CancellationToken>());
    }

    // ---------- Fallos aislados ----------

    [Fact]
    public async Task EjecutarAsync_FalloDeEnvio_NoCambiaElEstadoYRegistraElEnvioComoFallido()
    {
        var cliente = Datos.CrearCliente();
        var factura = Datos.CrearFactura("F1");
        ConClientes(cliente);
        ConFacturas(cliente, LecturaFacturas.Con(factura));
        _emailSender
            .EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP no disponible"));

        var resultado = await _useCase.EjecutarAsync(null, CancellationToken.None);

        var resultadoCliente = Assert.Single(resultado.Valor.Clientes);
        Assert.Equal(0, resultadoCliente.CorreosEnviados);
        Assert.Equal(0, resultadoCliente.FacturasActualizadas);
        var error = Assert.Single(resultadoCliente.Errores);
        Assert.Contains("F1", error, StringComparison.Ordinal);
        Assert.Contains("SMTP no disponible", error, StringComparison.Ordinal);

        Assert.Equal(EstadoFactura.PrimerRecordatorio, factura.Estado);
        await _facturas.DidNotReceive()
            .ActualizarEstadoAsync(Arg.Any<Factura>(), Arg.Any<EstadoFactura>(), Arg.Any<CancellationToken>());
        await _envios.Received(1).RegistrarAsync(
            Arg.Is<RegistroEnvio>(r =>
                r.FacturaId == "id-F1" && r.Resultado == ResultadoEnvio.Fallido && r.Error == "SMTP no disponible"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_FalloEnUnaFactura_NoAfectaALasDemasDelMismoCliente()
    {
        var cliente = Datos.CrearCliente();
        var falla = Datos.CrearFactura("F1");
        var funciona = Datos.CrearFactura("F2");
        ConClientes(cliente);
        ConFacturas(cliente, LecturaFacturas.Con(falla, funciona));
        _emailSender
            .EnviarAsync(Arg.Is<MensajeCorreo>(m => m.Asunto == "Asunto F1"), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Buzón rechazado"));

        var resultado = await _useCase.EjecutarAsync(null, CancellationToken.None);

        var resultadoCliente = Assert.Single(resultado.Valor.Clientes);
        Assert.Equal(1, resultadoCliente.CorreosEnviados);
        Assert.Equal(1, resultadoCliente.FacturasActualizadas);
        Assert.Single(resultadoCliente.Errores);
        Assert.Equal(EstadoFactura.PrimerRecordatorio, falla.Estado);
        Assert.Equal(EstadoFactura.SegundoRecordatorio, funciona.Estado);
    }

    [Fact]
    public async Task EjecutarAsync_ErrorEnUnCliente_NoAfectaALosDemas()
    {
        var conError = Datos.CrearCliente("cliente-1", "Comercial Andina");
        var sano = Datos.CrearCliente("cliente-2", "Distribuidora Sur");
        ConClientes(conError, sano);
        _facturas
            .BuscarAsync(Arg.Is<FiltroFacturas>(f => f.ClienteId == conError.Id), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Base de datos sin respuesta"));
        ConFacturas(sano, LecturaFacturas.Con(Datos.CrearFactura("F2", clienteId: sano.Id)));

        var resultado = await _useCase.EjecutarAsync(null, CancellationToken.None);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(2, resultado.Valor.Clientes.Count);
        var resultadoConError = resultado.Valor.Clientes.Single(c => c.ClienteId == conError.Id);
        var resultadoSano = resultado.Valor.Clientes.Single(c => c.ClienteId == sano.Id);
        Assert.Equal("Comercial Andina", resultadoConError.Nombre);
        Assert.Contains("Base de datos sin respuesta", Assert.Single(resultadoConError.Errores), StringComparison.Ordinal);
        Assert.Equal(0, resultadoConError.CorreosEnviados);
        Assert.Equal(1, resultadoSano.CorreosEnviados);
        Assert.Equal(1, resultadoSano.FacturasActualizadas);
    }

    [Fact]
    public async Task EjecutarAsync_FalloAlActualizarTrasEnviar_ReportaErrorYConservaElRegistroDelEnvio()
    {
        var cliente = Datos.CrearCliente();
        ConClientes(cliente);
        ConFacturas(cliente, LecturaFacturas.Con(Datos.CrearFactura("F1")));
        _facturas
            .ActualizarEstadoAsync(Arg.Any<Factura>(), Arg.Any<EstadoFactura>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Escritura sin respuesta"));

        var resultado = await _useCase.EjecutarAsync(null, CancellationToken.None);

        var resultadoCliente = Assert.Single(resultado.Valor.Clientes);
        Assert.Equal(1, resultadoCliente.CorreosEnviados);
        Assert.Equal(0, resultadoCliente.FacturasActualizadas);
        var error = Assert.Single(resultadoCliente.Errores);
        Assert.Contains("F1", error, StringComparison.Ordinal);
        Assert.Contains("Escritura sin respuesta", error, StringComparison.Ordinal);
        await _envios.Received(1).RegistrarAsync(
            Arg.Is<RegistroEnvio>(r => r.Resultado == ResultadoEnvio.Enviado),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_FalloAlRegistrarElEnvio_ReportaErrorPeroActualizaLaFactura()
    {
        var cliente = Datos.CrearCliente();
        var factura = Datos.CrearFactura("F1");
        ConClientes(cliente);
        ConFacturas(cliente, LecturaFacturas.Con(factura));
        _envios
            .RegistrarAsync(Arg.Any<RegistroEnvio>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Historial sin respuesta"));

        var resultado = await _useCase.EjecutarAsync(null, CancellationToken.None);

        var resultadoCliente = Assert.Single(resultado.Valor.Clientes);
        Assert.Equal(1, resultadoCliente.CorreosEnviados);
        Assert.Equal(1, resultadoCliente.FacturasActualizadas);
        Assert.Contains("Historial sin respuesta", Assert.Single(resultadoCliente.Errores), StringComparison.Ordinal);
        Assert.Equal(EstadoFactura.SegundoRecordatorio, factura.Estado);
    }

    // ---------- Conflictos, omitidas y casos vacíos ----------

    [Fact]
    public async Task EjecutarAsync_ActualizacionCondicionalSinModificaciones_ReportaConflicto()
    {
        var cliente = Datos.CrearCliente();
        ConClientes(cliente);
        ConFacturas(cliente, LecturaFacturas.Con(Datos.CrearFactura("F1")));
        _facturas
            .ActualizarEstadoAsync(Arg.Any<Factura>(), Arg.Any<EstadoFactura>(), Arg.Any<CancellationToken>())
            .Returns(false);

        var resultado = await _useCase.EjecutarAsync(null, CancellationToken.None);

        var resultadoCliente = Assert.Single(resultado.Valor.Clientes);
        Assert.Equal(1, resultadoCliente.CorreosEnviados);
        Assert.Equal(0, resultadoCliente.FacturasActualizadas);
        Assert.Equal(["F1"], resultadoCliente.Conflictos);
        Assert.Empty(resultadoCliente.Errores);
    }

    [Fact]
    public async Task EjecutarAsync_FacturasNoProcesables_AparecenComoOmitidasConSuMotivo()
    {
        var cliente = Datos.CrearCliente();
        var estadoDesconocido = ErroresFactura.EstadoDesconocido("pagada");
        ConClientes(cliente);
        ConFacturas(cliente, new LecturaFacturas(
            [Datos.CrearFactura("F1", EstadoFactura.Desactivado)],
            [new FacturaNoInterpretable("id-F9", "F9", estadoDesconocido)]));

        var resultado = await _useCase.EjecutarAsync(null, CancellationToken.None);

        var resultadoCliente = Assert.Single(resultado.Valor.Clientes);
        Assert.Equal(
            [
                new FacturaOmitida("F9", estadoDesconocido.Mensaje),
                new FacturaOmitida("F1", ErroresFactura.EstadoFinal.Mensaje),
            ],
            resultadoCliente.Omitidas);
        Assert.Equal(0, resultadoCliente.CorreosEnviados);
        await _emailSender.DidNotReceive().EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ClienteSinFacturas_NoEnviaCorreos()
    {
        var cliente = Datos.CrearCliente();
        ConClientes(cliente);
        ConFacturas(cliente, LecturaFacturas.Con());

        var resultado = await _useCase.EjecutarAsync(null, CancellationToken.None);

        var resultadoCliente = Assert.Single(resultado.Valor.Clientes);
        Assert.Equal(0, resultadoCliente.CorreosEnviados);
        Assert.Empty(resultadoCliente.Omitidas);
        await _emailSender.DidNotReceive().EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>());
    }

    // ---------- Alcance por cliente ----------

    [Fact]
    public async Task EjecutarAsync_ConClienteId_ProcesaSoloEseCliente()
    {
        var cliente = Datos.CrearCliente("cliente-2");
        _clientes.ObtenerPorIdAsync("cliente-2", Arg.Any<CancellationToken>()).Returns(cliente);
        ConFacturas(cliente, LecturaFacturas.Con(Datos.CrearFactura("F1", clienteId: cliente.Id)));

        var resultado = await _useCase.EjecutarAsync("cliente-2", CancellationToken.None);

        Assert.Equal("cliente-2", Assert.Single(resultado.Valor.Clientes).ClienteId);
        await _clientes.DidNotReceive().ObtenerTodosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_ConClienteIdInexistente_FallaConClienteNoEncontrado()
    {
        _clientes.ObtenerPorIdAsync("no-existe", Arg.Any<CancellationToken>()).Returns((Cliente?)null);

        var resultado = await _useCase.EjecutarAsync("no-existe", CancellationToken.None);

        Assert.True(resultado.EsFallido);
        Assert.Equal(ErroresAplicacion.ClienteNoEncontrado("no-existe"), resultado.Error);
        await _emailSender.DidNotReceive().EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>());
    }

    // ---------- Concurrencia y cancelación ----------

    [Fact]
    public async Task EjecutarAsync_ConEjecucionEnCurso_FallaSinProcesar()
    {
        _bloqueo.IntentarAdquirir().Returns((IDisposable?)null);

        var resultado = await _useCase.EjecutarAsync(null, CancellationToken.None);

        Assert.True(resultado.EsFallido);
        Assert.Equal(ErroresAplicacion.EjecucionEnCurso, resultado.Error);
        await _clientes.DidNotReceive().ObtenerTodosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarAsync_AlTerminar_LiberaElBloqueo()
    {
        ConClientes();

        await _useCase.EjecutarAsync(null, CancellationToken.None);

        _liberadorBloqueo.Received(1).Dispose();
    }

    [Fact]
    public async Task EjecutarAsync_SiFallaLaLecturaDeClientes_PropagaLaExcepcionYLiberaElBloqueo()
    {
        _clientes.ObtenerTodosAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new TimeoutException());

        await Assert.ThrowsAsync<TimeoutException>(() => _useCase.EjecutarAsync(null, CancellationToken.None));

        _liberadorBloqueo.Received(1).Dispose();
    }

    [Fact]
    public async Task EjecutarAsync_CuandoSeCancela_PropagaLaCancelacionSinTratarlaComoError()
    {
        var cliente = Datos.CrearCliente();
        ConClientes(cliente);
        ConFacturas(cliente, LecturaFacturas.Con(Datos.CrearFactura("F1")));
        _emailSender
            .EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => _useCase.EjecutarAsync(null, CancellationToken.None));
    }

    // ---------- Una sola factura ----------

    [Fact]
    public async Task EjecutarFacturaAsync_EnviaElAvisoDeEsaFacturaYAvanzaSuEstado()
    {
        var cliente = Datos.CrearCliente("cliente-1", "Comercial Andina", "andina@example.com");
        var factura = Datos.CrearFactura("F1", EstadoFactura.SegundoRecordatorio);
        ConFactura(factura, cliente);

        var resultado = await _useCase.EjecutarFacturaAsync(factura.Id, CancellationToken.None);

        Assert.True(resultado.EsExitoso);
        var resultadoCliente = Assert.Single(resultado.Valor.Clientes);
        Assert.Equal(1, resultadoCliente.CorreosEnviados);
        Assert.Equal(1, resultadoCliente.FacturasActualizadas);
        Assert.Equal(EstadoFactura.Desactivado, factura.Estado);
        _plantilla.Received(1).Generar(TipoAviso.Desactivacion, cliente, factura);
        await _emailSender.Received(1).EnviarAsync(
            Arg.Is<MensajeCorreo>(m => m.Destinatario == "andina@example.com"), Arg.Any<CancellationToken>());
        await _clientes.DidNotReceive().ObtenerTodosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarFacturaAsync_SiFallaElEnvio_NoAvanzaYReportaElError()
    {
        var factura = Datos.CrearFactura("F1");
        ConFactura(factura, Datos.CrearCliente());
        _emailSender
            .EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP no disponible"));

        var resultado = await _useCase.EjecutarFacturaAsync(factura.Id, CancellationToken.None);

        Assert.Contains("SMTP no disponible", Assert.Single(Assert.Single(resultado.Valor.Clientes).Errores), StringComparison.Ordinal);
        Assert.Equal(EstadoFactura.PrimerRecordatorio, factura.Estado);
    }

    [Fact]
    public async Task EjecutarFacturaAsync_ConFacturaDesactivada_FallaConEstadoFinalSinEnviar()
    {
        var factura = Datos.CrearFactura("F1", EstadoFactura.Desactivado);
        ConFactura(factura, Datos.CrearCliente());

        var resultado = await _useCase.EjecutarFacturaAsync(factura.Id, CancellationToken.None);

        Assert.Equal(ErroresFactura.EstadoFinal, resultado.Error);
        await _emailSender.DidNotReceive().EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarFacturaAsync_ConFacturaInexistente_FallaConFacturaNoEncontrada()
    {
        _facturas.BuscarAsync(Arg.Any<FiltroFacturas>(), Arg.Any<CancellationToken>()).Returns(LecturaFacturas.Con());

        var resultado = await _useCase.EjecutarFacturaAsync("no-existe", CancellationToken.None);

        Assert.Equal(ErroresAplicacion.FacturaNoEncontrada("no-existe"), resultado.Error);
    }

    [Fact]
    public async Task EjecutarFacturaAsync_ConFacturaNoInterpretable_FallaConSuMotivo()
    {
        var motivo = ErroresFactura.EstadoDesconocido("pagada");
        _facturas.BuscarAsync(Arg.Any<FiltroFacturas>(), Arg.Any<CancellationToken>())
            .Returns(new LecturaFacturas([], [new FacturaNoInterpretable("id-F9", "F9", motivo)]));

        var resultado = await _useCase.EjecutarFacturaAsync("id-F9", CancellationToken.None);

        Assert.Equal(motivo, resultado.Error);
    }

    [Fact]
    public async Task EjecutarFacturaAsync_SiElClienteDeLaFacturaNoExiste_FallaConClienteNoEncontrado()
    {
        var factura = Datos.CrearFactura("F1", clienteId: "huerfano");
        _facturas.BuscarAsync(Arg.Is<FiltroFacturas>(f => f.FacturaIds != null && f.FacturaIds.Contains(factura.Id)), Arg.Any<CancellationToken>())
            .Returns(LecturaFacturas.Con(factura));

        var resultado = await _useCase.EjecutarFacturaAsync(factura.Id, CancellationToken.None);

        Assert.Equal(ErroresAplicacion.ClienteNoEncontrado("huerfano"), resultado.Error);
    }

    [Fact]
    public async Task EjecutarFacturaAsync_ConEjecucionEnCurso_FallaSinConsultar()
    {
        _bloqueo.IntentarAdquirir().Returns((IDisposable?)null);

        var resultado = await _useCase.EjecutarFacturaAsync("id-F1", CancellationToken.None);

        Assert.Equal(ErroresAplicacion.EjecucionEnCurso, resultado.Error);
        await _facturas.DidNotReceive().BuscarAsync(Arg.Any<FiltroFacturas>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarFacturaAsync_AlTerminar_LiberaElBloqueo()
    {
        ConFactura(Datos.CrearFactura("F1"), Datos.CrearCliente());

        await _useCase.EjecutarFacturaAsync("id-F1", CancellationToken.None);

        _liberadorBloqueo.Received(1).Dispose();
    }

    // ---------- Selección de facturas ----------

    [Fact]
    public async Task EjecutarFacturasAsync_ProcesaSoloLaSeleccionAgrupadaPorCliente()
    {
        var andina = Datos.CrearCliente("cliente-1", "Comercial Andina", "andina@example.com");
        var sur = Datos.CrearCliente("cliente-2", "Distribuidora Sur", "sur@example.com");
        var f1 = Datos.CrearFactura("F1", clienteId: andina.Id);
        var f2 = Datos.CrearFactura("F2", EstadoFactura.SegundoRecordatorio, clienteId: sur.Id);
        var f3 = Datos.CrearFactura("F3", clienteId: andina.Id);
        ConSeleccion([f1, f2, f3], andina, sur);

        var resultado = await _useCase.EjecutarFacturasAsync([f1.Id, f2.Id, f3.Id], CancellationToken.None);

        Assert.True(resultado.EsExitoso);
        Assert.Equal(["cliente-1", "cliente-2"], resultado.Valor.Clientes.Select(c => c.ClienteId));
        Assert.Equal([2, 1], resultado.Valor.Clientes.Select(c => c.CorreosEnviados));
        Assert.Equal(EstadoFactura.SegundoRecordatorio, f1.Estado);
        Assert.Equal(EstadoFactura.Desactivado, f2.Estado);
        await _emailSender.Received(3).EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>());
        await _envios.Received(3).RegistrarAsync(
            Arg.Is<RegistroEnvio>(r => r.EjecucionId == resultado.Valor.EjecucionId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarFacturasAsync_ConFacturaDesactivada_LaInformaComoOmitidaYProcesaLasDemas()
    {
        var cliente = Datos.CrearCliente();
        var activa = Datos.CrearFactura("F1");
        var desactivada = Datos.CrearFactura("F2", EstadoFactura.Desactivado);
        ConSeleccion([activa, desactivada], cliente);

        var resultado = await _useCase.EjecutarFacturasAsync([activa.Id, desactivada.Id], CancellationToken.None);

        var resultadoCliente = Assert.Single(resultado.Valor.Clientes);
        Assert.Equal(1, resultadoCliente.CorreosEnviados);
        Assert.Equal([new FacturaOmitida("F2", ErroresFactura.EstadoFinal.Mensaje)], resultadoCliente.Omitidas);
    }

    [Fact]
    public async Task EjecutarFacturasAsync_ConIdsRepetidos_ProcesaCadaFacturaUnaSolaVez()
    {
        var factura = Datos.CrearFactura("F1");
        ConSeleccion([factura], Datos.CrearCliente());

        await _useCase.EjecutarFacturasAsync([factura.Id, factura.Id], CancellationToken.None);

        await _emailSender.Received(1).EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarFacturasAsync_ConSeleccionVacia_FallaSinTomarElBloqueo()
    {
        var resultado = await _useCase.EjecutarFacturasAsync([], CancellationToken.None);

        Assert.Equal(ErroresAplicacion.SeleccionVacia, resultado.Error);
        _bloqueo.DidNotReceive().IntentarAdquirir();
    }

    [Fact]
    public async Task EjecutarFacturasAsync_ConUnaFacturaInexistente_FallaSinEnviarNingunAviso()
    {
        var factura = Datos.CrearFactura("F1");
        ConSeleccion([factura], Datos.CrearCliente());

        var resultado = await _useCase.EjecutarFacturasAsync([factura.Id, "no-existe"], CancellationToken.None);

        Assert.Equal(ErroresAplicacion.FacturaNoEncontrada("no-existe"), resultado.Error);
        await _emailSender.DidNotReceive().EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarFacturasAsync_ConUnaFacturaNoInterpretable_FallaConSuMotivoSinEnviarNingunAviso()
    {
        var valida = Datos.CrearFactura("F1");
        var motivo = ErroresFactura.EstadoDesconocido("pagada");
        _facturas.BuscarAsync(Arg.Any<FiltroFacturas>(), Arg.Any<CancellationToken>())
            .Returns(new LecturaFacturas([valida], [new FacturaNoInterpretable("id-F9", "F9", motivo)]));

        var resultado = await _useCase.EjecutarFacturasAsync([valida.Id, "id-F9"], CancellationToken.None);

        Assert.Equal(motivo, resultado.Error);
        await _emailSender.DidNotReceive().EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarFacturasAsync_SiElClienteDeUnaFacturaNoExiste_FallaSinEnviarNingunAviso()
    {
        var huerfana = Datos.CrearFactura("F1", clienteId: "huerfano");
        ConSeleccion([huerfana]);

        var resultado = await _useCase.EjecutarFacturasAsync([huerfana.Id], CancellationToken.None);

        Assert.Equal(ErroresAplicacion.ClienteNoEncontrado("huerfano"), resultado.Error);
        await _emailSender.DidNotReceive().EnviarAsync(Arg.Any<MensajeCorreo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EjecutarFacturasAsync_ConEjecucionEnCurso_Falla()
    {
        _bloqueo.IntentarAdquirir().Returns((IDisposable?)null);

        var resultado = await _useCase.EjecutarFacturasAsync(["id-F1"], CancellationToken.None);

        Assert.Equal(ErroresAplicacion.EjecucionEnCurso, resultado.Error);
        await _facturas.DidNotReceive().BuscarAsync(Arg.Any<FiltroFacturas>(), Arg.Any<CancellationToken>());
    }

    private void ConSeleccion(Factura[] seleccion, params Cliente[] clientes)
    {
        _facturas.BuscarAsync(Arg.Is<FiltroFacturas>(f => f.FacturaIds != null), Arg.Any<CancellationToken>())
            .Returns(llamada => LecturaFacturas.Con(
                [.. seleccion.Where(f => llamada.Arg<FiltroFacturas>().FacturaIds!.Contains(f.Id))]));
        _clientes.ObtenerTodosAsync(Arg.Any<CancellationToken>()).Returns(clientes);
    }

    private void ConFactura(Factura factura, Cliente cliente)
    {
        _facturas.BuscarAsync(Arg.Is<FiltroFacturas>(f => f.FacturaIds != null && f.FacturaIds.Contains(factura.Id)), Arg.Any<CancellationToken>())
            .Returns(LecturaFacturas.Con(factura));
        _clientes.ObtenerPorIdAsync(cliente.Id, Arg.Any<CancellationToken>()).Returns(cliente);
    }

    private void ConClientes(params Cliente[] clientes) =>
        _clientes.ObtenerTodosAsync(Arg.Any<CancellationToken>()).Returns(clientes);

    private void ConFacturas(Cliente cliente, LecturaFacturas lectura) =>
        _facturas
            .BuscarAsync(Arg.Is<FiltroFacturas>(f => f.ClienteId == cliente.Id), Arg.Any<CancellationToken>())
            .Returns(lectura);
}
