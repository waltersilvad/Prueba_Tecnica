using Facturacion.Domain.Comun;

namespace Facturacion.Domain.Facturas;

/// <summary>
/// Factura de un cliente. Decide qué aviso le corresponde y controla sus transiciones de estado.
/// </summary>
public sealed class Factura
{
    private Factura(
        string id,
        string numero,
        string clienteId,
        decimal monto,
        string moneda,
        DateTime fechaEmision,
        DateTime fechaVencimiento,
        EstadoFactura estado,
        DateTime fechaActualizacion)
    {
        Id = id;
        Numero = numero;
        ClienteId = clienteId;
        Monto = monto;
        Moneda = moneda;
        FechaEmision = fechaEmision;
        FechaVencimiento = fechaVencimiento;
        Estado = estado;
        FechaActualizacion = fechaActualizacion;
    }

    public string Id { get; }
    public string Numero { get; }
    public string ClienteId { get; }
    public decimal Monto { get; }
    public string Moneda { get; }
    public DateTime FechaEmision { get; }
    public DateTime FechaVencimiento { get; }
    public EstadoFactura Estado { get; private set; }
    public DateTime FechaActualizacion { get; private set; }

    /// <summary>
    /// Crea una factura validando sus invariantes. Todas las fechas deben estar en UTC.
    /// </summary>
    /// <exception cref="ArgumentException">Si alguna fecha no está en UTC.</exception>
    public static Result<Factura> Crear(
        string id,
        string numero,
        string clienteId,
        decimal monto,
        string moneda,
        DateTime fechaEmision,
        DateTime fechaVencimiento,
        EstadoFactura estado,
        DateTime fechaActualizacion)
    {
        Guardas.AsegurarUtc(fechaEmision);
        Guardas.AsegurarUtc(fechaVencimiento);
        Guardas.AsegurarUtc(fechaActualizacion);

        var error = ValidarCampoRequerido(id, nameof(Id))
            ?? ValidarCampoRequerido(numero, nameof(Numero))
            ?? ValidarCampoRequerido(clienteId, nameof(ClienteId))
            ?? ValidarCampoRequerido(moneda, nameof(Moneda))
            ?? ValidarMoneda(moneda)
            ?? ValidarMonto(monto)
            ?? ValidarFechas(fechaEmision, fechaVencimiento)
            ?? ValidarEstado(estado);

        if (error is not null)
        {
            return Result.Fallo<Factura>(error);
        }

        return Result.Exito(new Factura(
            id, numero, clienteId, monto, moneda, fechaEmision, fechaVencimiento, estado, fechaActualizacion));
    }

    /// <summary>
    /// Registra una factura nueva. Siempre empieza en <see cref="EstadoFactura.PrimerRecordatorio"/>:
    /// quien la crea no puede elegir otro estado.
    /// </summary>
    /// <exception cref="ArgumentException">Si alguna fecha no está en UTC.</exception>
    public static Result<Factura> Registrar(
        string id,
        string numero,
        string clienteId,
        decimal monto,
        string moneda,
        DateTime fechaEmision,
        DateTime fechaVencimiento,
        DateTime ahoraUtc) =>
        Crear(
            id,
            numero.Trim(),
            clienteId,
            monto,
            moneda,
            fechaEmision,
            fechaVencimiento,
            EstadoFactura.PrimerRecordatorio,
            ahoraUtc);

    /// <summary>
    /// Determina el aviso que corresponde al estado actual, o el motivo por el que no se debe avisar.
    /// </summary>
    public Result<TipoAviso> DeterminarAviso() =>
        TransicionDesde(Estado) is { } transicion
            ? Result.Exito(transicion.Aviso)
            : Result.Fallo<TipoAviso>(ErroresFactura.EstadoFinal);

    /// <summary>
    /// Avanza al siguiente estado del ciclo. Desde <see cref="EstadoFactura.Desactivado"/> no se puede avanzar.
    /// </summary>
    /// <exception cref="ArgumentException">Si <paramref name="ahoraUtc"/> no está en UTC.</exception>
    public Result AvanzarEstado(DateTime ahoraUtc)
    {
        Guardas.AsegurarUtc(ahoraUtc);

        if (TransicionDesde(Estado) is not { } transicion)
        {
            return Result.Fallo(ErroresFactura.EstadoFinal);
        }

        Estado = transicion.Siguiente;
        FechaActualizacion = ahoraUtc;

        return Result.Exito();
    }

    /// <summary>
    /// Única definición del ciclo: el aviso que corresponde a cada estado y el estado al que pasa la factura cuando
    /// ese aviso sale. <see langword="null"/> si el estado es final. Así el aviso y la transición no pueden desalinearse.
    /// </summary>
    private static (TipoAviso Aviso, EstadoFactura Siguiente)? TransicionDesde(EstadoFactura estado) => estado switch
    {
        EstadoFactura.PrimerRecordatorio => (TipoAviso.SegundoRecordatorio, EstadoFactura.SegundoRecordatorio),
        EstadoFactura.SegundoRecordatorio => (TipoAviso.Desactivacion, EstadoFactura.Desactivado),
        _ => null,
    };

    private static ErrorDominio? ValidarCampoRequerido(string valor, string campo) =>
        string.IsNullOrWhiteSpace(valor) ? ErroresFactura.CampoRequerido(campo) : null;

    /// <summary>Código ISO 4217: tres letras mayúsculas. Coincide con el <c>$jsonSchema</c> de la colección.</summary>
    private static ErrorDominio? ValidarMoneda(string moneda) =>
        moneda.Length == 3 && moneda.All(char.IsAsciiLetterUpper) ? null : ErroresFactura.MonedaInvalida;

    private static ErrorDominio? ValidarMonto(decimal monto) =>
        monto <= 0 ? ErroresFactura.MontoNoPositivo : null;

    private static ErrorDominio? ValidarFechas(DateTime fechaEmision, DateTime fechaVencimiento) =>
        fechaVencimiento < fechaEmision ? ErroresFactura.VencimientoAnteriorAEmision : null;

    private static ErrorDominio? ValidarEstado(EstadoFactura estado) =>
        Enum.IsDefined(estado) ? null : ErroresFactura.EstadoInvalido;
}
