using FluentValidation;

namespace Facturacion.Api.Solicitudes;

/// <summary>
/// Cuerpo de <c>POST /api/facturas/procesar</c> y <c>POST /api/facturas/eliminar</c>: las facturas seleccionadas.
/// </summary>
public sealed class SeleccionFacturasSolicitud
{
    /// <summary>Ids de las facturas seleccionadas.</summary>
    /// <example>["6716a2000000000000001001", "6716a2000000000000002002"]</example>
    public IReadOnlyList<string> FacturaIds { get; init; } = [];
}

public sealed class SeleccionFacturasSolicitudValidator : AbstractValidator<SeleccionFacturasSolicitud>
{
    public SeleccionFacturasSolicitudValidator()
    {
        RuleFor(s => s.FacturaIds).NotEmpty().WithMessage("Selecciona al menos una factura.");
        RuleForEach(s => s.FacturaIds).NotEmpty().WithMessage("Hay un id de factura vacío.");
    }
}
