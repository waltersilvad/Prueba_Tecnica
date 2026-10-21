namespace Facturacion.Domain.Facturas;

/// <summary>
/// Estados de una factura en el ciclo de recordatorios:
/// <see cref="PrimerRecordatorio"/> → <see cref="SegundoRecordatorio"/> → <see cref="Desactivado"/> (final).
/// </summary>
public enum EstadoFactura
{
    PrimerRecordatorio,
    SegundoRecordatorio,
    Desactivado,
}
