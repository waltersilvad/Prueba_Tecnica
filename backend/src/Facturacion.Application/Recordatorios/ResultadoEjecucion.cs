namespace Facturacion.Application.Recordatorios;

/// <summary>
/// Resultado de una ejecución del proceso de recordatorios.
/// </summary>
public sealed record ResultadoEjecucion(string EjecucionId, IReadOnlyList<ResultadoCliente> Clientes);

/// <summary>
/// Resultado del proceso para un cliente.
/// </summary>
/// <param name="ClienteId">Id del cliente.</param>
/// <param name="Nombre">Nombre del cliente.</param>
/// <param name="CorreosEnviados">Avisos que salieron correctamente.</param>
/// <param name="FacturasActualizadas">Facturas que avanzaron de estado.</param>
/// <param name="Omitidas">Facturas que no requerían aviso, con su motivo.</param>
/// <param name="Errores">Fallos de envío o de procesamiento; las facturas afectadas no cambian de estado.</param>
/// <param name="Conflictos">Números de factura cuyo estado cambió otra ejecución después de enviar el correo.</param>
public sealed record ResultadoCliente(
    string ClienteId,
    string Nombre,
    int CorreosEnviados,
    int FacturasActualizadas,
    IReadOnlyList<FacturaOmitida> Omitidas,
    IReadOnlyList<string> Errores,
    IReadOnlyList<string> Conflictos);

/// <summary>Factura que no se procesó y el motivo.</summary>
public sealed record FacturaOmitida(string Numero, string Motivo);
