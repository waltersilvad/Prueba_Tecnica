using Facturacion.Application.Clientes;
using Facturacion.Domain.Clientes;
using FluentValidation;

namespace Facturacion.Api.Solicitudes;

/// <summary>
/// Cuerpo de <c>POST /api/clientes</c> y <c>PUT /api/clientes/{id}</c>.
/// </summary>
public sealed class ClienteSolicitud
{
    /// <summary>Nombre o razón social del cliente.</summary>
    /// <example>Comercial Andina S.A.S.</example>
    public string Nombre { get; init; } = string.Empty;

    /// <summary>Correo al que se envían los avisos.</summary>
    /// <example>evaluador@example.com</example>
    public string Email { get; init; } = string.Empty;

    public CrearClienteComando ACrearComando() => new(Nombre, Email);

    public ActualizarClienteComando AActualizarComando() => new(Nombre, Email);
}

/// <summary>
/// Validación de entrada con mensajes por campo. La regla de formato del correo es la del dominio
/// (<see cref="Cliente.EsEmailValido"/>), así no hay dos definiciones distintas de "correo válido".
/// </summary>
public sealed class ClienteSolicitudValidator : AbstractValidator<ClienteSolicitud>
{
    public ClienteSolicitudValidator()
    {
        RuleFor(s => s.Nombre)
            .NotEmpty().WithMessage("El nombre es obligatorio.");

        RuleFor(s => s.Email)
            .NotEmpty().WithMessage("El correo es obligatorio.")
            .Must(email => Cliente.EsEmailValido(email.Trim())).WithMessage("El correo no tiene un formato válido.")
            .When(s => !string.IsNullOrWhiteSpace(s.Email), ApplyConditionTo.CurrentValidator);
    }
}
