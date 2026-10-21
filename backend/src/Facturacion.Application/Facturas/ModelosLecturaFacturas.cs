using Facturacion.Domain.Comun;
using Facturacion.Domain.Facturas;

namespace Facturacion.Application.Facturas;

/// <summary>Criterios opcionales para buscar facturas. Un valor nulo no filtra.</summary>
/// <param name="ClienteId">Solo las facturas de este cliente.</param>
/// <param name="Estado">Solo las facturas en este estado.</param>
/// <param name="FacturaIds">Ids de factura a incluir (por ejemplo, una selección hecha en la interfaz).</param>
public sealed record FiltroFacturas(
    string? ClienteId = null,
    EstadoFactura? Estado = null,
    IReadOnlyCollection<string>? FacturaIds = null);

/// <summary>
/// Resultado de leer facturas: las válidas y las que no se pudieron interpretar con su motivo.
/// </summary>
public sealed record LecturaFacturas(
    IReadOnlyList<Factura> Facturas,
    IReadOnlyList<FacturaNoInterpretable> NoInterpretables)
{
    public static LecturaFacturas Con(params Factura[] facturas) => new(facturas, []);
}

/// <summary>Documento de factura almacenado que no cumple las reglas del dominio.</summary>
public sealed record FacturaNoInterpretable(string Id, string Numero, ErrorDominio Motivo);

/// <summary>Fila agregada: cantidad y monto de facturas de un cliente en un estado y moneda.</summary>
public sealed record ConteoFacturas(
    string ClienteId,
    EstadoFactura Estado,
    string Moneda,
    int Cantidad,
    decimal MontoTotal);
