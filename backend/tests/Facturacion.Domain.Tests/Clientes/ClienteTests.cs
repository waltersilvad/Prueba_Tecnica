using Facturacion.Domain.Clientes;

namespace Facturacion.Domain.Tests.Clientes;

public sealed class ClienteTests
{
    private static readonly DateTime FechaCreacion = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Ahora = new(2026, 10, 21, 15, 30, 0, DateTimeKind.Utc);

    private static Cliente CrearClienteValido() =>
        Cliente.Crear("cliente-1", "Comercial Andina", "pagos@example.com", FechaCreacion).Valor;

    // ---------- Crear ----------

    [Fact]
    public void Crear_ConDatosValidos_DevuelveElClienteConSusDatos()
    {
        var resultado = Cliente.Crear("cliente-1", "Comercial Andina", "pagos@example.com", FechaCreacion);

        Assert.True(resultado.EsExitoso);
        var cliente = resultado.Valor;
        Assert.Equal("cliente-1", cliente.Id);
        Assert.Equal("Comercial Andina", cliente.Nombre);
        Assert.Equal("pagos@example.com", cliente.Email);
        Assert.Equal(FechaCreacion, cliente.FechaActualizacion);
    }

    [Fact]
    public void Crear_EliminaEspaciosSobrantesDeNombreYEmail()
    {
        var cliente = Cliente.Crear("cliente-1", "  Comercial Andina ", " pagos@example.com  ", FechaCreacion).Valor;

        Assert.Equal("Comercial Andina", cliente.Nombre);
        Assert.Equal("pagos@example.com", cliente.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinId_FallaConCampoRequerido(string id)
    {
        var resultado = Cliente.Crear(id, "Comercial Andina", "pagos@example.com", FechaCreacion);

        Assert.Equal(ErroresCliente.CampoRequerido(nameof(Cliente.Id)), resultado.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinNombre_FallaConCampoRequerido(string nombre)
    {
        var resultado = Cliente.Crear("cliente-1", nombre, "pagos@example.com", FechaCreacion);

        Assert.Equal(ErroresCliente.CampoRequerido(nameof(Cliente.Nombre)), resultado.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Crear_SinEmail_FallaConCampoRequerido(string email)
    {
        var resultado = Cliente.Crear("cliente-1", "Comercial Andina", email, FechaCreacion);

        Assert.Equal(ErroresCliente.CampoRequerido(nameof(Cliente.Email)), resultado.Error);
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("usuario@")]
    [InlineData("@example.com")]
    [InlineData("dos@@example.com")]
    [InlineData("Nombre <pagos@example.com>")]
    public void Crear_ConEmailMalFormado_FallaConEmailInvalido(string email)
    {
        var resultado = Cliente.Crear("cliente-1", "Comercial Andina", email, FechaCreacion);

        Assert.True(resultado.EsFallido);
        Assert.Equal(ErroresCliente.EmailInvalido, resultado.Error);
    }

    [Theory]
    [InlineData("pagos@example.com", true)]
    [InlineData("cartera.sur@empresa.com.co", true)]
    [InlineData("sin-arroba", false)]
    [InlineData("Nombre <pagos@example.com>", false)]
    [InlineData(" pagos@example.com", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void EsEmailValido_AplicaLaMismaReglaQueLaEntidad(string? email, bool esperado)
    {
        Assert.Equal(esperado, Cliente.EsEmailValido(email));
    }

    [Fact]
    public void Crear_ConFechaNoUtc_LanzaArgumentException()
    {
        var fechaLocal = DateTime.SpecifyKind(FechaCreacion, DateTimeKind.Local);

        Assert.Throws<ArgumentException>(
            () => Cliente.Crear("cliente-1", "Comercial Andina", "pagos@example.com", fechaLocal));
    }

    // ---------- ActualizarDatos ----------

    [Fact]
    public void ActualizarDatos_ConDatosValidos_CambiaNombreEmailYFecha()
    {
        var cliente = CrearClienteValido();

        var resultado = cliente.ActualizarDatos(" Distribuidora Sur ", "cartera@example.com", Ahora);

        Assert.True(resultado.EsExitoso);
        Assert.Equal("Distribuidora Sur", cliente.Nombre);
        Assert.Equal("cartera@example.com", cliente.Email);
        Assert.Equal(Ahora, cliente.FechaActualizacion);
    }

    [Fact]
    public void ActualizarDatos_ConEmailInvalido_FallaYNoModificaNada()
    {
        var cliente = CrearClienteValido();

        var resultado = cliente.ActualizarDatos("Distribuidora Sur", "correo-invalido", Ahora);

        Assert.Equal(ErroresCliente.EmailInvalido, resultado.Error);
        Assert.Equal("Comercial Andina", cliente.Nombre);
        Assert.Equal("pagos@example.com", cliente.Email);
        Assert.Equal(FechaCreacion, cliente.FechaActualizacion);
    }

    [Fact]
    public void ActualizarDatos_SinNombre_FallaYNoModificaNada()
    {
        var cliente = CrearClienteValido();

        var resultado = cliente.ActualizarDatos(" ", "cartera@example.com", Ahora);

        Assert.Equal(ErroresCliente.CampoRequerido(nameof(Cliente.Nombre)), resultado.Error);
        Assert.Equal("pagos@example.com", cliente.Email);
    }

    [Fact]
    public void ActualizarDatos_ConFechaNoUtc_LanzaArgumentException()
    {
        var cliente = CrearClienteValido();

        Assert.Throws<ArgumentException>(
            () => cliente.ActualizarDatos("Distribuidora Sur", "cartera@example.com", DateTime.SpecifyKind(Ahora, DateTimeKind.Local)));
    }
}
