namespace Facturacion.Application.Correo;

/// <summary>
/// Configuración regional de los correos (sección <c>PlantillaCorreo</c>).
/// </summary>
public sealed class OpcionesPlantillaCorreo
{
    public const string Seccion = "PlantillaCorreo";

    /// <summary>Zona horaria IANA en la que se muestran las fechas. Ejemplo: <c>America/Bogota</c>.</summary>
    public string ZonaHoraria { get; set; } = string.Empty;

    /// <summary>Cultura para formatear números. Ejemplo: <c>es-CO</c>.</summary>
    public string Cultura { get; set; } = string.Empty;
}
