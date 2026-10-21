using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Application.Facturas;
using Facturacion.Domain.Clientes;
using Facturacion.Domain.Comun;
using Facturacion.Domain.Facturas;
using Microsoft.Extensions.Logging;

namespace Facturacion.Application.Recordatorios;

/// <summary>
/// Envía el aviso que corresponde a cada factura pendiente y, solo si el correo salió, avanza su estado.
/// </summary>
/// <remarks>
/// Decide <b>qué</b> facturas procesar (todas, las de un cliente, una sola o una selección), con el bloqueo de
/// ejecución y las validaciones previas; el <b>cómo</b> de cada aviso lo resuelve <see cref="EnviadorAvisoFactura"/>.
/// Aislamiento de fallos: un error en un cliente no detiene a los demás, y un error en una factura no
/// detiene a las demás del mismo cliente. La cancelación sí se propaga.
/// </remarks>
public sealed partial class ProcesarRecordatoriosUseCase(
    IClienteRepository clientes,
    IFacturaRepository facturas,
    IBloqueoEjecucion bloqueo,
    EnviadorAvisoFactura enviador,
    ILogger<ProcesarRecordatoriosUseCase> logger)
{
    /// <summary>
    /// Procesa todos los clientes, o solo <paramref name="clienteId"/> si se indica.
    /// </summary>
    /// <returns>
    /// El resultado por cliente, o un fallo si ya hay una ejecución en curso o el cliente no existe.
    /// </returns>
    public async Task<Result<ResultadoEjecucion>> EjecutarAsync(string? clienteId, CancellationToken cancellationToken)
    {
        using var bloqueoAdquirido = bloqueo.IntentarAdquirir();
        if (bloqueoAdquirido is null)
        {
            return Result.Fallo<ResultadoEjecucion>(ErroresAplicacion.EjecucionEnCurso);
        }

        var clientesAProcesar = await ObtenerClientesAsync(clienteId, cancellationToken);
        if (clientesAProcesar.EsFallido)
        {
            return Result.Fallo<ResultadoEjecucion>(clientesAProcesar.Error);
        }

        var ejecucionId = Guid.NewGuid().ToString();
        var resultados = new List<ResultadoCliente>();
        foreach (var cliente in clientesAProcesar.Valor)
        {
            resultados.Add(await ProcesarClienteAsync(ejecucionId, cliente, cancellationToken));
        }

        return Result.Exito(new ResultadoEjecucion(ejecucionId, resultados));
    }

    /// <summary>
    /// Procesa una sola factura con las mismas reglas del proceso masivo: envía su aviso y, solo si el correo
    /// sale, avanza su estado. Comparte el bloqueo de ejecución.
    /// </summary>
    /// <returns>
    /// El resultado (con un solo cliente), o un fallo si hay una ejecución en curso, si la factura o su cliente
    /// no existen, o si la factura no admite más avisos (por ejemplo, porque está desactivada).
    /// </returns>
    public async Task<Result<ResultadoEjecucion>> EjecutarFacturaAsync(string facturaId, CancellationToken cancellationToken)
    {
        using var bloqueoAdquirido = bloqueo.IntentarAdquirir();
        if (bloqueoAdquirido is null)
        {
            return Result.Fallo<ResultadoEjecucion>(ErroresAplicacion.EjecucionEnCurso);
        }

        var lectura = await facturas.BuscarAsync(new FiltroFacturas(FacturaIds: [facturaId]), cancellationToken);
        if (lectura.NoInterpretables is [var noInterpretable, ..])
        {
            return Result.Fallo<ResultadoEjecucion>(noInterpretable.Motivo);
        }

        if (lectura.Facturas is not [var factura, ..])
        {
            return Result.Fallo<ResultadoEjecucion>(ErroresAplicacion.FacturaNoEncontrada(facturaId));
        }

        // Al pedir una factura concreta, un estado final es un error para quien la pidió (no una "omitida").
        var aviso = factura.DeterminarAviso();
        if (aviso.EsFallido)
        {
            return Result.Fallo<ResultadoEjecucion>(aviso.Error);
        }

        var cliente = await clientes.ObtenerPorIdAsync(factura.ClienteId, cancellationToken);
        if (cliente is null)
        {
            return Result.Fallo<ResultadoEjecucion>(ErroresAplicacion.ClienteNoEncontrado(factura.ClienteId));
        }

        var ejecucionId = Guid.NewGuid().ToString();
        var acumulador = new AcumuladorResultadoCliente(cliente);
        await enviador.ProcesarAsync(new ContextoEnvio(ejecucionId, cliente, factura, aviso.Valor, acumulador), cancellationToken);

        return Result.Exito(new ResultadoEjecucion(ejecucionId, [acumulador.Construir()]));
    }

    /// <summary>
    /// Procesa una selección de facturas (de uno o varios clientes) con las mismas reglas del proceso masivo.
    /// Las facturas que no admiten más avisos se informan como omitidas.
    /// </summary>
    /// <returns>
    /// El resultado agrupado por cliente, o un fallo si la selección está vacía, hay una ejecución en curso o
    /// alguna factura (o su cliente) no existe. Si falla la validación no se envía ningún aviso.
    /// </returns>
    public async Task<Result<ResultadoEjecucion>> EjecutarFacturasAsync(
        IReadOnlyCollection<string> facturaIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(facturaIds);

        var ids = facturaIds.Distinct(StringComparer.Ordinal).ToList();
        if (ids.Count == 0)
        {
            return Result.Fallo<ResultadoEjecucion>(ErroresAplicacion.SeleccionVacia);
        }

        using var bloqueoAdquirido = bloqueo.IntentarAdquirir();
        if (bloqueoAdquirido is null)
        {
            return Result.Fallo<ResultadoEjecucion>(ErroresAplicacion.EjecucionEnCurso);
        }

        var lectura = await facturas.BuscarAsync(new FiltroFacturas(FacturaIds: ids), cancellationToken);
        if (lectura.NoInterpretables is [var noInterpretable, ..])
        {
            return Result.Fallo<ResultadoEjecucion>(noInterpretable.Motivo);
        }

        var encontradas = lectura.Facturas.Select(f => f.Id).ToHashSet(StringComparer.Ordinal);
        if (ids.FirstOrDefault(id => !encontradas.Contains(id)) is { } faltante)
        {
            return Result.Fallo<ResultadoEjecucion>(ErroresAplicacion.FacturaNoEncontrada(faltante));
        }

        // Se valida todo antes de enviar el primer aviso: una selección inválida no deja envíos a medias.
        var todos = await clientes.ObtenerTodosAsync(cancellationToken);
        var clientesPorId = todos.ToDictionary(c => c.Id);
        var facturasPorCliente = lectura.Facturas.ToLookup(f => f.ClienteId);
        if (facturasPorCliente.FirstOrDefault(g => !clientesPorId.ContainsKey(g.Key)) is { } sinCliente)
        {
            return Result.Fallo<ResultadoEjecucion>(ErroresAplicacion.ClienteNoEncontrado(sinCliente.Key));
        }

        var ejecucionId = Guid.NewGuid().ToString();
        var resultados = new List<ResultadoCliente>();
        foreach (var cliente in todos.Where(c => facturasPorCliente.Contains(c.Id)))
        {
            var acumulador = new AcumuladorResultadoCliente(cliente);
            await ProcesarFacturasAsync(ejecucionId, cliente, facturasPorCliente[cliente.Id], acumulador, cancellationToken);
            resultados.Add(acumulador.Construir());
        }

        return Result.Exito(new ResultadoEjecucion(ejecucionId, resultados));
    }

    private async Task<Result<IReadOnlyList<Cliente>>> ObtenerClientesAsync(
        string? clienteId, CancellationToken cancellationToken)
    {
        if (clienteId is null)
        {
            return Result.Exito(await clientes.ObtenerTodosAsync(cancellationToken));
        }

        var cliente = await clientes.ObtenerPorIdAsync(clienteId, cancellationToken);
        return cliente is null
            ? Result.Fallo<IReadOnlyList<Cliente>>(ErroresAplicacion.ClienteNoEncontrado(clienteId))
            : Result.Exito<IReadOnlyList<Cliente>>([cliente]);
    }

    private async Task<ResultadoCliente> ProcesarClienteAsync(
        string ejecucionId, Cliente cliente, CancellationToken cancellationToken)
    {
        var acumulador = new AcumuladorResultadoCliente(cliente);
        try
        {
            var lectura = await facturas.BuscarAsync(new FiltroFacturas(ClienteId: cliente.Id), cancellationToken);

            foreach (var noInterpretable in lectura.NoInterpretables)
            {
                acumulador.Omitir(noInterpretable.Numero, noInterpretable.Motivo.Mensaje);
            }

            await ProcesarFacturasAsync(ejecucionId, cliente, lectura.Facturas, acumulador, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            LogErrorCliente(logger, ex, cliente.Id);
            acumulador.RegistrarError($"No se pudo procesar el cliente: {ex.Message}");
        }

        return acumulador.Construir();
    }

    /// <summary>
    /// Recorre facturas de un mismo cliente: las que no admiten aviso se omiten con su motivo y las demás se procesan
    /// una por una (cada una aislada de las otras). Lo comparten el proceso masivo y el de una selección.
    /// </summary>
    private async Task ProcesarFacturasAsync(
        string ejecucionId,
        Cliente cliente,
        IEnumerable<Factura> facturasDelCliente,
        AcumuladorResultadoCliente acumulador,
        CancellationToken cancellationToken)
    {
        foreach (var factura in facturasDelCliente)
        {
            var aviso = factura.DeterminarAviso();
            if (aviso.EsFallido)
            {
                acumulador.Omitir(factura.Numero, aviso.Error.Mensaje);
                continue;
            }

            await enviador.ProcesarAsync(
                new ContextoEnvio(ejecucionId, cliente, factura, aviso.Valor, acumulador), cancellationToken);
        }
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Error al procesar el cliente {ClienteId}.")]
    private static partial void LogErrorCliente(ILogger logger, Exception ex, string clienteId);
}
