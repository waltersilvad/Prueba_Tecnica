using System.Globalization;
using Microsoft.Extensions.Options;

namespace Facturacion.Application.Correo;

/// <summary>
/// Valida al arrancar que la zona horaria y la cultura configuradas existan en el sistema.
/// </summary>
public sealed class ValidadorOpcionesPlantillaCorreo : IValidateOptions<OpcionesPlantillaCorreo>
{
    public ValidateOptionsResult Validate(string? name, OpcionesPlantillaCorreo options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errores = new List<string>();

        if (!TimeZoneInfo.TryFindSystemTimeZoneById(options.ZonaHoraria, out _))
        {
            errores.Add($"{OpcionesPlantillaCorreo.Seccion}:{nameof(options.ZonaHoraria)} '{options.ZonaHoraria}' no es una zona horaria válida.");
        }

        if (!EsCulturaValida(options.Cultura))
        {
            errores.Add($"{OpcionesPlantillaCorreo.Seccion}:{nameof(options.Cultura)} '{options.Cultura}' no es una cultura válida.");
        }

        return errores.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errores);
    }

    private static bool EsCulturaValida(string cultura)
    {
        if (string.IsNullOrWhiteSpace(cultura))
        {
            return false;
        }

        try
        {
            _ = CultureInfo.GetCultureInfo(cultura, predefinedOnly: true);
            return true;
        }
        catch (CultureNotFoundException)
        {
            return false;
        }
    }
}
