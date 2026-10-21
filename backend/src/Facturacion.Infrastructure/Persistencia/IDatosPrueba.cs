using Facturacion.Domain.Comun;

namespace Facturacion.Infrastructure.Persistencia;

/// <summary>
/// Administración de los datos de prueba. Solo debe exponerse en Development.
/// </summary>
public interface IDatosPrueba
{
    /// <summary>
    /// Elimina todos los clientes, facturas y envíos (incluidos los de ejemplo) y los archivos <c>.eml</c> que generó
    /// el proveedor de correo Consola en su carpeta de salida.
    /// </summary>
    /// <returns>Lo que se eliminó, o un fallo si hay un proceso de avisos en ejecución.</returns>
    Task<Result<ResumenBorrado>> BorrarTodoAsync(CancellationToken cancellationToken);

    /// <summary>Carga los clientes y facturas de ejemplo. Solo con la base vacía, para no mezclarlos con datos propios.</summary>
    /// <returns>Lo que se cargó, o un fallo si la base tiene datos o hay un proceso en ejecución.</returns>
    Task<Result<ResumenCargaEjemplo>> CargarEjemploAsync(CancellationToken cancellationToken);
}

/// <summary>Cantidades eliminadas por <see cref="IDatosPrueba.BorrarTodoAsync"/>.</summary>
public sealed record ResumenBorrado(long Clientes, long Facturas, long Envios, int Correos);

/// <summary>Cantidades cargadas por <see cref="IDatosPrueba.CargarEjemploAsync"/>.</summary>
public sealed record ResumenCargaEjemplo(int Clientes, int Facturas);

/// <summary>Errores esperados de la administración de datos de prueba.</summary>
public static class ErroresDatosPrueba
{
    public const string CodigoBaseConDatos = "Datos.BaseConDatos";

    public static readonly ErrorDominio BaseConDatos = new(
        CodigoBaseConDatos,
        "La base ya tiene datos. Bórralos primero para cargar los datos de ejemplo.",
        TipoError.Conflicto);
}
