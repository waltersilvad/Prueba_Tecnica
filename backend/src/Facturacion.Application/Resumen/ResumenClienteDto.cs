namespace Facturacion.Application.Resumen;

/// <summary>
/// Estado de la cartera de un cliente: conteo por estado, total y monto pendiente por moneda.
/// </summary>
public sealed record ResumenClienteDto(
    string ClienteId,
    string Nombre,
    string Email,
    int PrimerRecordatorio,
    int SegundoRecordatorio,
    int Desactivado,
    int TotalFacturas,
    IReadOnlyList<MontoPorMonedaDto> MontoPendiente);

/// <summary>Suma de montos en una moneda.</summary>
public sealed record MontoPorMonedaDto(string Moneda, decimal Monto);
