namespace Facturacion.Domain.Facturas;

/// <summary>
/// Aviso que se envía al cliente según el estado actual de la factura.
/// </summary>
public enum TipoAviso
{
    /// <summary>Se envía a facturas en <see cref="EstadoFactura.PrimerRecordatorio"/>.</summary>
    SegundoRecordatorio,

    /// <summary>Se envía a facturas en <see cref="EstadoFactura.SegundoRecordatorio"/>.</summary>
    Desactivacion,
}
