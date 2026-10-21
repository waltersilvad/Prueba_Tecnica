namespace Facturacion.Domain.Comun;

/// <summary>
/// Resultado de una operación que puede fallar por un motivo esperado del negocio.
/// </summary>
public class Result
{
    protected Result(bool esExitoso, ErrorDominio error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (!esExitoso && error == ErrorDominio.Ninguno)
        {
            throw new ArgumentException("Un resultado fallido debe indicar un error.", nameof(error));
        }

        EsExitoso = esExitoso;
        Error = error;
    }

    /// <summary>Indica si la operación terminó correctamente.</summary>
    public bool EsExitoso { get; }

    /// <summary>Indica si la operación falló.</summary>
    public bool EsFallido => !EsExitoso;

    /// <summary>Error de la operación, o <see cref="ErrorDominio.Ninguno"/> si fue exitosa.</summary>
    public ErrorDominio Error { get; }

    /// <summary>Crea un resultado exitoso sin valor.</summary>
    public static Result Exito() => new(true, ErrorDominio.Ninguno);

    /// <summary>Crea un resultado fallido con el error indicado.</summary>
    public static Result Fallo(ErrorDominio error) => new(false, error);

    /// <summary>Crea un resultado exitoso con un valor.</summary>
    public static Result<T> Exito<T>(T valor) => new(valor, true, ErrorDominio.Ninguno);

    /// <summary>Crea un resultado fallido que habría devuelto un valor de tipo <typeparamref name="T"/>.</summary>
    public static Result<T> Fallo<T>(ErrorDominio error) => new(default, false, error);
}

/// <summary>
/// Resultado de una operación que, si es exitosa, produce un valor.
/// </summary>
/// <typeparam name="T">Tipo del valor producido.</typeparam>
public sealed class Result<T> : Result
{
    private readonly T? _valor;

    internal Result(T? valor, bool esExitoso, ErrorDominio error)
        : base(esExitoso, error)
    {
        _valor = valor;
    }

    /// <summary>Valor producido. Lanza <see cref="InvalidOperationException"/> si el resultado es fallido.</summary>
    public T Valor => EsExitoso
        ? _valor!
        : throw new InvalidOperationException("No se puede leer el valor de un resultado fallido.");
}
