namespace Facturacion.Api.Configuracion;

/// <summary>
/// Orígenes del frontend que pueden llamar a la API (sección <c>Cors</c>).
/// </summary>
public sealed class OpcionesCors
{
    public const string Seccion = "Cors";

    public string[] OrigenesPermitidos { get; set; } = [];
}

/// <summary>
/// Parámetros del endpoint <c>/health</c> (sección <c>Salud</c>).
/// </summary>
public sealed class OpcionesSalud
{
    public const string Seccion = "Salud";

    /// <summary>Tiempo máximo para que MongoDB responda antes de declararlo no saludable.</summary>
    public TimeSpan TiempoMaximoMongo { get; set; }
}
