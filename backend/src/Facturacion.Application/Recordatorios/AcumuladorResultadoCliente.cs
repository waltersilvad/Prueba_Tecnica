using Facturacion.Domain.Clientes;

namespace Facturacion.Application.Recordatorios;

/// <summary>
/// Va reuniendo lo ocurrido con las facturas de un cliente durante una ejecución.
/// </summary>
internal sealed class AcumuladorResultadoCliente(Cliente cliente)
{
    private readonly List<FacturaOmitida> _omitidas = [];
    private readonly List<string> _errores = [];
    private readonly List<string> _conflictos = [];
    private int _correosEnviados;
    private int _facturasActualizadas;

    public void Omitir(string numeroFactura, string motivo) => _omitidas.Add(new FacturaOmitida(numeroFactura, motivo));

    public void ContarCorreoEnviado() => _correosEnviados++;

    public void ContarFacturaActualizada() => _facturasActualizadas++;

    public void RegistrarConflicto(string numeroFactura) => _conflictos.Add(numeroFactura);

    public void RegistrarError(string error) => _errores.Add(error);

    public ResultadoCliente Construir() => new(
        cliente.Id,
        cliente.Nombre,
        _correosEnviados,
        _facturasActualizadas,
        _omitidas.AsReadOnly(),
        _errores.AsReadOnly(),
        _conflictos.AsReadOnly());
}
