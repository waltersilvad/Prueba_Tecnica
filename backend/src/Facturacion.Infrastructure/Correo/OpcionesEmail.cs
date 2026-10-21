namespace Facturacion.Infrastructure.Correo;

/// <summary>Implementación de <c>IEmailSender</c> que se usa.</summary>
public enum ProveedorEmail
{
    /// <summary>No envía: escribe cada correo como archivo <c>.eml</c> en <see cref="OpcionesEmail.CarpetaSalida"/>.</summary>
    Consola,

    /// <summary>Envía por SMTP con Brevo usando <see cref="OpcionesSmtp"/>.</summary>
    Brevo,
}

/// <summary>
/// Configuración general del correo (sección <c>Email</c>).
/// </summary>
public sealed class OpcionesEmail
{
    public const string Seccion = "Email";

    public ProveedorEmail Proveedor { get; set; } = ProveedorEmail.Consola;

    /// <summary>Dirección del remitente. Con Brevo debe ser un remitente verificado en la cuenta.</summary>
    public string Remitente { get; set; } = string.Empty;

    /// <summary>Nombre visible del remitente.</summary>
    public string NombreRemitente { get; set; } = string.Empty;

    /// <summary>Carpeta donde el proveedor Consola guarda los correos. Relativa al directorio de trabajo.</summary>
    public string CarpetaSalida { get; set; } = string.Empty;
}

/// <summary>
/// Servidor SMTP (sección <c>Smtp</c>). Solo se exige cuando el proveedor es Brevo.
/// Usuario y clave deben venir de user secrets o variables de entorno, nunca del repositorio.
/// </summary>
public sealed class OpcionesSmtp
{
    public const string Seccion = "Smtp";

    public string Servidor { get; set; } = string.Empty;

    public int Puerto { get; set; }

    public string Usuario { get; set; } = string.Empty;

    public string Clave { get; set; } = string.Empty;

    /// <summary>Reintentos adicionales ante fallas transitorias (red o respuestas SMTP 4xx).</summary>
    public int Reintentos { get; set; }

    /// <summary>Espera entre reintentos.</summary>
    public TimeSpan EsperaEntreReintentos { get; set; }
}
