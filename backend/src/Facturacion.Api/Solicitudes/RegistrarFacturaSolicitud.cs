using Facturacion.Application.Facturas;
using FluentValidation;

namespace Facturacion.Api.Solicitudes;

/// <summary>
/// Cuerpo de <c>POST /api/facturas</c>. La factura se crea siempre en <c>primerrecordatorio</c>.
/// </summary>
public sealed class RegistrarFacturaSolicitud
{
    /// <summary>Id del cliente dueño de la factura.</summary>
    /// <example>6716a1000000000000000001</example>
    public string ClienteId { get; init; } = string.Empty;

    /// <summary>Número de factura. Debe ser único.</summary>
    /// <example>FAC-9001</example>
    public string Numero { get; init; } = string.Empty;

    /// <summary>Monto, mayor que cero.</summary>
    /// <example>850000.50</example>
    public decimal Monto { get; init; }

    /// <summary>Código ISO 4217 de la moneda.</summary>
    /// <example>COP</example>
    public string Moneda { get; init; } = string.Empty;

    /// <summary>Fecha de emisión (fecha de calendario en Bogotá).</summary>
    /// <example>2026-10-21</example>
    public DateOnly FechaEmision { get; init; }

    /// <summary>Fecha de vencimiento (fecha de calendario en Bogotá).</summary>
    /// <example>2026-11-20</example>
    public DateOnly FechaVencimiento { get; init; }

    public RegistrarFacturaComando AComando() =>
        new(ClienteId, Numero, Monto, Moneda, FechaEmision, FechaVencimiento);
}

/// <summary>
/// Validación de entrada con mensajes por campo. Las mismas reglas las vuelve a aplicar el dominio.
/// </summary>
public sealed class RegistrarFacturaSolicitudValidator : AbstractValidator<RegistrarFacturaSolicitud>
{
    public RegistrarFacturaSolicitudValidator()
    {
        RuleFor(s => s.ClienteId).NotEmpty().WithMessage("Selecciona el cliente.");
        RuleFor(s => s.Numero).NotEmpty().WithMessage("El número de factura es obligatorio.");
        RuleFor(s => s.Monto).GreaterThan(0).WithMessage("El monto debe ser mayor que cero.");
        RuleFor(s => s.Moneda)
            .Matches("^[A-Z]{3}$").WithMessage("La moneda debe ser un código de 3 letras en mayúsculas (por ejemplo, COP).");
        RuleFor(s => s.FechaEmision).NotEmpty().WithMessage("La fecha de emisión es obligatoria.");
        RuleFor(s => s.FechaVencimiento)
            .NotEmpty().WithMessage("La fecha de vencimiento es obligatoria.")
            .GreaterThanOrEqualTo(s => s.FechaEmision)
            .WithMessage("La fecha de vencimiento no puede ser anterior a la de emisión.");
    }
}
