namespace Facturacion.Api.Configuracion;

/// <summary>
/// Carga el archivo <c>.env</c> de la raíz del repositorio en las variables de entorno del proceso, para que la misma
/// configuración sirva en Docker Compose y al ejecutar con F5 o <c>dotnet run</c>.
/// </summary>
/// <remarks>
/// Solo se usa en Development. Las variables de entorno que ya existan siempre ganan (Docker, CI o un servidor no se
/// ven afectados) y los valores vacíos se ignoran para no reemplazar los valores por defecto de appsettings.json.
/// Formato: <c>CLAVE=valor</c> por línea, comentarios con <c>#</c> al inicio de la línea y comillas opcionales.
/// Las claves usan <c>__</c> como separador de secciones (por ejemplo, <c>Smtp__Clave</c>).
/// </remarks>
public static class ArchivoEnv
{
    public const string NombreArchivo = ".env";

    /// <summary>
    /// Si el entorno es Development, busca el <c>.env</c> desde <paramref name="directorioInicial"/> hacia arriba y lo carga.
    /// </summary>
    /// <returns>Cantidad de variables cargadas (0 si no aplica o no hay archivo).</returns>
    public static int CargarSiEsDesarrollo(string directorioInicial)
    {
        var entorno = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        if (!string.Equals(entorno, "Development", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var ruta = Buscar(directorioInicial);
        return ruta is null ? 0 : Cargar(ruta);
    }

    /// <summary>Busca el archivo en <paramref name="directorioInicial"/> y en sus carpetas superiores.</summary>
    public static string? Buscar(string directorioInicial)
    {
        for (var directorio = new DirectoryInfo(directorioInicial); directorio is not null; directorio = directorio.Parent)
        {
            var ruta = Path.Combine(directorio.FullName, NombreArchivo);
            if (File.Exists(ruta))
            {
                return ruta;
            }
        }

        return null;
    }

    /// <summary>Carga el archivo sin sobrescribir variables de entorno existentes.</summary>
    /// <returns>Cantidad de variables cargadas.</returns>
    public static int Cargar(string ruta)
    {
        var cargadas = 0;
        foreach (var (clave, valor) in Interpretar(File.ReadAllLines(ruta)))
        {
            if (Environment.GetEnvironmentVariable(clave) is null)
            {
                Environment.SetEnvironmentVariable(clave, valor);
                cargadas++;
            }
        }

        return cargadas;
    }

    /// <summary>
    /// Interpreta las líneas del archivo. Ignora líneas vacías, comentarios, líneas sin <c>=</c> y valores vacíos.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Interpretar(IEnumerable<string> lineas)
    {
        ArgumentNullException.ThrowIfNull(lineas);

        var variables = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var linea in lineas.Select(l => l.Trim()))
        {
            var separador = linea.IndexOf('=', StringComparison.Ordinal);
            if (linea.Length == 0 || linea.StartsWith('#') || separador <= 0)
            {
                continue;
            }

            var clave = linea[..separador].Trim();
            var valor = QuitarComillas(linea[(separador + 1)..].Trim());
            if (valor.Length > 0)
            {
                variables[clave] = valor;
            }
        }

        return variables;
    }

    private static string QuitarComillas(string valor) =>
        valor.Length >= 2 && (valor[0] is '"' or '\'') && valor[^1] == valor[0] ? valor[1..^1] : valor;
}
