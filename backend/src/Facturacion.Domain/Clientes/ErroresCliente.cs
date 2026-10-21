using Facturacion.Domain.Comun;

namespace Facturacion.Domain.Clientes;

/// <summary>
/// Catálogo de errores esperados de la entidad <see cref="Cliente"/>.
/// </summary>
public static class ErroresCliente
{
    public static readonly ErrorDominio EmailInvalido =
        new("Cliente.EmailInvalido", "El correo electrónico no tiene un formato válido.", TipoError.Validacion);

    public static ErrorDominio CampoRequerido(string campo) =>
        new("Cliente.CampoRequerido", $"El campo '{campo}' del cliente es obligatorio.", TipoError.Validacion);
}
