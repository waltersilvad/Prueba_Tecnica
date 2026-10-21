using System.Net.Mail;
using Facturacion.Domain.Comun;

namespace Facturacion.Domain.Clientes;

/// <summary>
/// Cliente dueño de facturas y destinatario de los avisos.
/// </summary>
public sealed class Cliente
{
    private Cliente(string id, string nombre, string email, DateTime fechaActualizacion)
    {
        Id = id;
        Nombre = nombre;
        Email = email;
        FechaActualizacion = fechaActualizacion;
    }

    public string Id { get; }
    public string Nombre { get; private set; }
    public string Email { get; private set; }
    public DateTime FechaActualizacion { get; private set; }

    /// <summary>
    /// Crea un cliente validando nombre y correo. Los espacios sobrantes se eliminan.
    /// </summary>
    /// <exception cref="ArgumentException">Si <paramref name="fechaActualizacion"/> no está en UTC.</exception>
    public static Result<Cliente> Crear(string id, string nombre, string email, DateTime fechaActualizacion)
    {
        Guardas.AsegurarUtc(fechaActualizacion);

        var error = ValidarCampoRequerido(id, nameof(Id)) ?? ValidarDatos(nombre, email);
        if (error is not null)
        {
            return Result.Fallo<Cliente>(error);
        }

        return Result.Exito(new Cliente(id, nombre.Trim(), email.Trim(), fechaActualizacion));
    }

    /// <summary>
    /// Cambia nombre y correo. Si alguno es inválido, el cliente queda sin cambios.
    /// </summary>
    /// <exception cref="ArgumentException">Si <paramref name="ahoraUtc"/> no está en UTC.</exception>
    public Result ActualizarDatos(string nombre, string email, DateTime ahoraUtc)
    {
        Guardas.AsegurarUtc(ahoraUtc);

        var error = ValidarDatos(nombre, email);
        if (error is not null)
        {
            return Result.Fallo(error);
        }

        Nombre = nombre.Trim();
        Email = email.Trim();
        FechaActualizacion = ahoraUtc;

        return Result.Exito();
    }

    /// <summary>
    /// Regla de formato de correo del dominio. Es pública para que otras capas (por ejemplo, la validación
    /// de la API) apliquen exactamente la misma regla en lugar de duplicarla.
    /// </summary>
    /// <remarks>
    /// Se usa <see cref="MailAddress"/> de la BCL para no depender de paquetes externos. Se exige que la
    /// dirección interpretada coincida con el texto completo, para rechazar formas como "Nombre &lt;a@b.com&gt;".
    /// </remarks>
    public static bool EsEmailValido(string? email) =>
        MailAddress.TryCreate(email, out var direccion) && direccion.Address == email;

    private static ErrorDominio? ValidarDatos(string nombre, string email) =>
        ValidarCampoRequerido(nombre, nameof(Nombre))
        ?? ValidarCampoRequerido(email, nameof(Email))
        ?? (EsEmailValido(email.Trim()) ? null : ErroresCliente.EmailInvalido);

    private static ErrorDominio? ValidarCampoRequerido(string valor, string campo) =>
        string.IsNullOrWhiteSpace(valor) ? ErroresCliente.CampoRequerido(campo) : null;
}
