using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Facturacion.Application.Clientes;
using Facturacion.Application.Comun;
using Facturacion.Application.Envios;
using Facturacion.Application.Facturas;
using Facturacion.Application.Recordatorios;
using Facturacion.Application.Resumen;
using Facturacion.Infrastructure.Persistencia;
using Facturacion.IntegrationTests.Infraestructura;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Facturacion.IntegrationTests.Api;

[Collection(ColeccionMongo.Nombre)]
public sealed class ApiTests(MongoFixture mongo)
{
    private const string ClienteInexistente = "6716a1000000000000000099";

    private static readonly string[] RutasDelPlan =
    [
        "/api/recordatorios/procesar", "/api/clientes", "/api/clientes/{id}", "/api/facturas",
        "/api/resumen/clientes", "/api/envios", "/api/datos/borrar", "/api/datos/ejemplo",
        "/api/facturas/procesar", "/api/facturas/{id}/procesar", "/api/facturas/eliminar",
        "/api/facturas/{id}",
    ];

    private static readonly string[] CamposNombreYEmail = ["nombre", "email"];

    private static readonly string[] CamposFacturaInvalida = ["monto", "moneda", "fechaVencimiento"];

    // ---------- Salud y documentación ----------

    [Fact]
    public async Task Health_ConMongoDisponible_Devuelve200ConElDetalleDeMongo()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().GetAsync(new Uri("/health", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = await LeerJsonAsync(respuesta);
        Assert.Equal("Healthy", json.RootElement.GetProperty("estado").GetString());
        Assert.Equal("Healthy", json.RootElement.GetProperty("chequeos").GetProperty("mongo").GetProperty("estado").GetString());
    }

    [Fact]
    public async Task OpenApi_DocumentaTodosLosEndpointsDelPlan()
    {
        await using var api = new FabricaApi(mongo);

        using var json = await LeerJsonAsync(await api.CreateClient().GetAsync(new Uri("/openapi/v1.json", UriKind.Relative)));

        var rutas = json.RootElement.GetProperty("paths").EnumerateObject().Select(p => p.Name).ToList();
        Assert.Equivalent(RutasDelPlan, rutas);
    }

    [Fact]
    public async Task Raiz_RedirigeAScalar()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var respuesta = await cliente.GetAsync(new Uri("/", UriKind.Relative));

        Assert.Equal(HttpStatusCode.Redirect, respuesta.StatusCode);
        Assert.Equal("/scalar", respuesta.Headers.Location?.ToString());
        var scalar = await api.CreateClient().GetAsync(new Uri("/scalar", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, scalar.StatusCode);
        Assert.Equal("text/html", scalar.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("http://localhost:5173", true)]
    [InlineData("http://localhost:8080", true)]
    // Docker Desktop muestra los puertos publicados en 127.0.0.1: el front también se abre con esa dirección.
    [InlineData("http://127.0.0.1:5173", true)]
    [InlineData("http://127.0.0.1:8080", true)]
    [InlineData("http://sitio-ajeno.example.com", false)]
    public async Task Cors_SoloPermiteLosOrigenesConfigurados(string origen, bool permitido)
    {
        await using var api = new FabricaApi(mongo);
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/clientes/" + EntornoPruebas.IdCliente1);
        preflight.Headers.Add("Origin", origen);
        preflight.Headers.Add("Access-Control-Request-Method", "PUT");

        var respuesta = await api.CreateClient().SendAsync(preflight);

        Assert.Equal(permitido, respuesta.Headers.TryGetValues("Access-Control-Allow-Origin", out var valores) && valores.Contains(origen));
    }

    [Fact]
    public async Task Cors_PermiteDeleteDesdeElFrontParaEliminarClientes()
    {
        await using var api = new FabricaApi(mongo);
        using var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/clientes/" + EntornoPruebas.IdCliente1);
        preflight.Headers.Add("Origin", "http://localhost:5173");
        preflight.Headers.Add("Access-Control-Request-Method", "DELETE");

        var respuesta = await api.CreateClient().SendAsync(preflight);

        Assert.True(respuesta.Headers.TryGetValues("Access-Control-Allow-Methods", out var metodos));
        Assert.Contains("DELETE", string.Join(",", metodos), StringComparison.Ordinal);
    }

    // ---------- Clientes ----------

    [Fact]
    public async Task ListarClientes_DevuelveLosClientesDelSeedEnCamelCase()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().GetAsync(new Uri("/api/clientes", UriKind.Relative));

        using var json = await LeerJsonAsync(respuesta);
        Assert.Equal(3, json.RootElement.GetArrayLength());
        var primero = json.RootElement[0];
        Assert.Equal(EntornoPruebas.IdCliente1, primero.GetProperty("id").GetString());
        Assert.Equal("Comercial Andina S.A.S.", primero.GetProperty("nombre").GetString());
        Assert.Equal("cartera.andina@example.com", primero.GetProperty("email").GetString());
    }

    [Fact]
    public async Task ObtenerCliente_Inexistente_Devuelve404ConProblemDetails()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().GetAsync(new Uri($"/api/clientes/{ClienteInexistente}", UriKind.Relative));

        await AfirmarProblemaAsync(respuesta, HttpStatusCode.NotFound, ErroresAplicacion.CodigoClienteNoEncontrado);
    }

    [Fact]
    public async Task ActualizarCliente_ConDatosValidos_Devuelve200YLoGuarda()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/clientes/{EntornoPruebas.IdCliente2}",
            new { nombre = "Distribuidora del Sur S.A.", email = "evaluador@example.com" });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var guardado = await cliente.GetFromJsonAsync<ClienteDto>($"/api/clientes/{EntornoPruebas.IdCliente2}");
        Assert.Equal("evaluador@example.com", guardado!.Email);
        Assert.Equal("Distribuidora del Sur S.A.", guardado.Nombre);
    }

    [Fact]
    public async Task ActualizarCliente_ConCorreoInvalido_Devuelve400ConElErrorDelCampoSinGuardar()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();

        var respuesta = await cliente.PutAsJsonAsync(
            $"/api/clientes/{EntornoPruebas.IdCliente1}", new { nombre = "Comercial Andina", email = "correo-invalido" });

        var errores = await LeerErroresDeValidacionAsync(respuesta);
        Assert.Equal(["email"], errores.Keys);
        var guardado = await cliente.GetFromJsonAsync<ClienteDto>($"/api/clientes/{EntornoPruebas.IdCliente1}");
        Assert.Equal("cartera.andina@example.com", guardado!.Email);
    }

    [Fact]
    public async Task ActualizarCliente_SinNombreNiCorreo_Devuelve400ConAmbosCampos()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().PutAsJsonAsync(
            $"/api/clientes/{EntornoPruebas.IdCliente1}", new { nombre = " ", email = "" });

        var errores = await LeerErroresDeValidacionAsync(respuesta);
        Assert.Equivalent(CamposNombreYEmail, errores.Keys);
    }

    [Fact]
    public async Task ActualizarCliente_Inexistente_Devuelve404()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().PutAsJsonAsync(
            $"/api/clientes/{ClienteInexistente}", new { nombre = "Nadie", email = "nadie@example.com" });

        await AfirmarProblemaAsync(respuesta, HttpStatusCode.NotFound, ErroresAplicacion.CodigoClienteNoEncontrado);
    }

    // ---------- Facturas, resumen y envíos ----------

    [Fact]
    public async Task ConsultarFacturas_ConFiltros_DevuelveSoloLasQueCoinciden()
    {
        await using var api = new FabricaApi(mongo);

        var facturas = (await api.CreateClient().GetFromJsonAsync<List<FacturaDto>>(
            $"/api/facturas?estado=segundorecordatorio&clienteId={EntornoPruebas.IdCliente2}"))!;

        Assert.Equal(["FAC-2002", "FAC-2003"], facturas.Select(f => f.Numero));
        Assert.All(facturas, f => Assert.Equal("Distribuidora del Sur Ltda.", f.NombreCliente));
    }

    [Fact]
    public async Task ConsultarFacturas_ConEstadoDesconocido_Devuelve400()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().GetAsync(new Uri("/api/facturas?estado=pagada", UriKind.Relative));

        await AfirmarProblemaAsync(respuesta, HttpStatusCode.BadRequest, ErroresAplicacion.CodigoFiltroInvalido);
    }

    [Fact]
    public async Task ResumenClientes_DevuelveUnResumenPorCliente()
    {
        await using var api = new FabricaApi(mongo);

        var resumen = await api.CreateClient().GetFromJsonAsync<List<ResumenClienteDto>>("/api/resumen/clientes");

        Assert.Equal([4, 3, 3], resumen!.Select(r => r.TotalFacturas));
    }

    // ---------- Proceso de recordatorios ----------

    [Fact]
    public async Task Procesar_Devuelve200ConElResultadoPorClienteYRegistraLosEnvios()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();

        var respuesta = await cliente.PostAsync(new Uri("/api/recordatorios/procesar", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var resultado = await respuesta.Content.ReadFromJsonAsync<ResultadoEjecucion>();
        Assert.Equal(3, resultado!.Clientes.Count);
        Assert.Equal(9, resultado.Clientes.Sum(c => c.CorreosEnviados));
        Assert.Equal("FAC-1004", resultado.Clientes.SelectMany(c => c.Omitidas).Single().Numero);

        var envios = await cliente.GetFromJsonAsync<List<EnvioDto>>("/api/envios");
        Assert.Equal(9, envios!.Count);
        Assert.All(envios, e => Assert.Equal("Enviado", e.Resultado));
    }

    [Fact]
    public async Task Procesar_UnSoloCliente_ProcesaSoloEseCliente()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().PostAsync(
            new Uri($"/api/recordatorios/procesar?clienteId={EntornoPruebas.IdCliente3}", UriKind.Relative), content: null);

        var resultado = await respuesta.Content.ReadFromJsonAsync<ResultadoEjecucion>();
        Assert.Equal(EntornoPruebas.IdCliente3, Assert.Single(resultado!.Clientes).ClienteId);
    }

    [Fact]
    public async Task Procesar_ClienteInexistente_Devuelve404()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().PostAsync(
            new Uri($"/api/recordatorios/procesar?clienteId={ClienteInexistente}", UriKind.Relative), content: null);

        await AfirmarProblemaAsync(respuesta, HttpStatusCode.NotFound, ErroresAplicacion.CodigoClienteNoEncontrado);
    }

    [Fact]
    public async Task Procesar_ConOtraEjecucionEnCurso_Devuelve409()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();

        using (api.Services.GetRequiredService<IBloqueoEjecucion>().IntentarAdquirir())
        {
            var respuesta = await cliente.PostAsync(new Uri("/api/recordatorios/procesar", UriKind.Relative), content: null);

            await AfirmarProblemaAsync(respuesta, HttpStatusCode.Conflict, ErroresAplicacion.CodigoEjecucionEnCurso);
        }

        Assert.Empty((await cliente.GetFromJsonAsync<List<EnvioDto>>("/api/envios"))!);
    }

    // ---------- Reinicio de datos ----------

    [Fact]
    public async Task BorrarDatos_EnDevelopment_EliminaTodoYDevuelveLasCantidades()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();

        var respuesta = await cliente.PostAsync(new Uri("/api/datos/borrar", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        using var json = await LeerJsonAsync(respuesta);
        Assert.Equal(3, json.RootElement.GetProperty("clientes").GetInt32());
        Assert.Equal(10, json.RootElement.GetProperty("facturas").GetInt32());
        Assert.Empty((await cliente.GetFromJsonAsync<List<ClienteDto>>("/api/clientes"))!);
        Assert.Empty((await cliente.GetFromJsonAsync<List<FacturaDto>>("/api/facturas"))!);
    }

    [Fact]
    public async Task CargarEjemplo_ConLaBaseVacia_CargaLosDatosYConDatos_Devuelve409()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();
        await cliente.PostAsync(new Uri("/api/datos/borrar", UriKind.Relative), content: null);

        var primera = await cliente.PostAsync(new Uri("/api/datos/ejemplo", UriKind.Relative), content: null);
        var segunda = await cliente.PostAsync(new Uri("/api/datos/ejemplo", UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        Assert.Equal(10, (await cliente.GetFromJsonAsync<List<FacturaDto>>("/api/facturas"))!.Count);
        await AfirmarProblemaAsync(segunda, HttpStatusCode.Conflict, ErroresDatosPrueba.CodigoBaseConDatos);
    }

    [Theory]
    [InlineData("/api/datos/borrar")]
    [InlineData("/api/datos/ejemplo")]
    public async Task DatosDePrueba_FueraDeDevelopment_NoExisten(string ruta)
    {
        await using var api = new FabricaApi(mongo, "Production");

        var respuesta = await api.CreateClient().PostAsync(new Uri(ruta, UriKind.Relative), content: null);

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
    }

    // ---------- Eliminar cliente y procesar una selección ----------

    [Fact]
    public async Task EliminarCliente_BorraElClienteConSusFacturasYEnvios()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();
        await cliente.PostAsync(
            new Uri($"/api/recordatorios/procesar?clienteId={EntornoPruebas.IdCliente1}", UriKind.Relative), content: null);

        var respuesta = await cliente.DeleteAsync(new Uri($"/api/clientes/{EntornoPruebas.IdCliente1}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var eliminado = (await respuesta.Content.ReadFromJsonAsync<ResultadoEliminacionCliente>())!;
        Assert.Equal(4, eliminado.FacturasEliminadas);
        Assert.Equal(3, eliminado.EnviosEliminados);
        Assert.Equal(HttpStatusCode.NotFound, (await cliente.GetAsync(new Uri($"/api/clientes/{EntornoPruebas.IdCliente1}", UriKind.Relative))).StatusCode);
        Assert.Equal(6, (await cliente.GetFromJsonAsync<List<FacturaDto>>("/api/facturas"))!.Count);
        Assert.Empty((await cliente.GetFromJsonAsync<List<EnvioDto>>($"/api/envios?clienteId={EntornoPruebas.IdCliente1}"))!);
    }

    [Fact]
    public async Task EliminarCliente_Inexistente_Devuelve404()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().DeleteAsync(new Uri($"/api/clientes/{ClienteInexistente}", UriKind.Relative));

        await AfirmarProblemaAsync(respuesta, HttpStatusCode.NotFound, ErroresAplicacion.CodigoClienteNoEncontrado);
    }

    [Fact]
    public async Task ProcesarSeleccion_EnviaSoloLasFacturasElegidasAgrupadasPorCliente()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();
        var facturas = (await cliente.GetFromJsonAsync<List<FacturaDto>>("/api/facturas"))!;
        var elegidas = facturas.Where(f => f.Numero is "FAC-1001" or "FAC-2002" or "FAC-1004").Select(f => f.Id).ToList();

        var respuesta = await cliente.PostAsJsonAsync("/api/facturas/procesar", new { facturaIds = elegidas });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var resultado = (await respuesta.Content.ReadFromJsonAsync<ResultadoEjecucion>())!;
        Assert.Equal(2, resultado.Clientes.Count);
        Assert.Equal(2, resultado.Clientes.Sum(c => c.CorreosEnviados));
        Assert.Equal("FAC-1004", Assert.Single(resultado.Clientes.SelectMany(c => c.Omitidas)).Numero);
        Assert.Equal(2, (await cliente.GetFromJsonAsync<List<EnvioDto>>("/api/envios"))!.Count);
    }

    [Fact]
    public async Task EliminarFactura_BorraLaFacturaYSuHistorial()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();
        var factura = (await cliente.GetFromJsonAsync<List<FacturaDto>>("/api/facturas"))!.Single(f => f.Numero == "FAC-3001");
        await cliente.PostAsync(new Uri($"/api/facturas/{factura.Id}/procesar", UriKind.Relative), content: null);

        var respuesta = await cliente.DeleteAsync(new Uri($"/api/facturas/{factura.Id}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        var eliminado = (await respuesta.Content.ReadFromJsonAsync<ResultadoEliminacionFacturas>())!;
        Assert.Equal(["FAC-3001"], eliminado.Numeros);
        Assert.Equal(1, eliminado.EnviosEliminados);
        Assert.DoesNotContain((await cliente.GetFromJsonAsync<List<FacturaDto>>("/api/facturas"))!, f => f.Id == factura.Id);
        Assert.Empty((await cliente.GetFromJsonAsync<List<EnvioDto>>("/api/envios"))!);
    }

    [Fact]
    public async Task EliminarSeleccion_BorraTodasLasElegidasIncluidasLasDesactivadas()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();
        var elegidas = (await cliente.GetFromJsonAsync<List<FacturaDto>>("/api/facturas"))!
            .Where(f => f.Numero is "FAC-1001" or "FAC-1004")
            .Select(f => f.Id)
            .ToList();

        var respuesta = await cliente.PostAsJsonAsync("/api/facturas/eliminar", new { facturaIds = elegidas });

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal(2, (await respuesta.Content.ReadFromJsonAsync<ResultadoEliminacionFacturas>())!.FacturasEliminadas);
        Assert.Equal(8, (await cliente.GetFromJsonAsync<List<FacturaDto>>("/api/facturas"))!.Count);
    }

    [Fact]
    public async Task EliminarSeleccion_ConUnaFacturaInexistente_Devuelve404SinBorrarNinguna()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();
        var existente = (await cliente.GetFromJsonAsync<List<FacturaDto>>("/api/facturas"))![0].Id;

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/facturas/eliminar", new { facturaIds = new[] { existente, "6716a2000000000000009999" } });

        await AfirmarProblemaAsync(respuesta, HttpStatusCode.NotFound, ErroresAplicacion.CodigoFacturaNoEncontrada);
        Assert.Equal(10, (await cliente.GetFromJsonAsync<List<FacturaDto>>("/api/facturas"))!.Count);
    }

    [Fact]
    public async Task ProcesarSeleccion_Vacia_Devuelve400()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/facturas/procesar", new { facturaIds = Array.Empty<string>() });

        Assert.Equal(["facturaIds"], (await LeerErroresDeValidacionAsync(respuesta)).Keys);
    }

    // ---------- Ampliaciones: crear cliente, crear factura y procesar una factura ----------

    [Fact]
    public async Task CrearCliente_ConDatosValidos_Devuelve201YApareceEnElListado()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/clientes", new { nombre = "Industrias Pacífico", email = "pagos@pacifico.example.com" });

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var creado = (await respuesta.Content.ReadFromJsonAsync<ClienteDto>())!;
        Assert.Equal($"/api/clientes/{creado.Id}", respuesta.Headers.Location?.ToString());
        var listado = (await cliente.GetFromJsonAsync<List<ClienteDto>>("/api/clientes"))!;
        Assert.Equal(4, listado.Count);
        Assert.Contains(listado, c => c.Id == creado.Id && c.Email == "pagos@pacifico.example.com");
    }

    [Fact]
    public async Task CrearCliente_ConCorreoInvalido_Devuelve400ConElErrorDelCampo()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().PostAsJsonAsync(
            "/api/clientes", new { nombre = "Sin correo", email = "no-es-correo" });

        Assert.Equal(["email"], (await LeerErroresDeValidacionAsync(respuesta)).Keys);
    }

    [Fact]
    public async Task RegistrarFactura_ConDatosValidos_Devuelve201EnPrimerRecordatorio()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/api/facturas", FacturaNueva("FAC-9001"));

        Assert.Equal(HttpStatusCode.Created, respuesta.StatusCode);
        var factura = (await respuesta.Content.ReadFromJsonAsync<FacturaDto>())!;
        Assert.Equal("primerrecordatorio", factura.Estado);
        Assert.Equal("Comercial Andina S.A.S.", factura.NombreCliente);
        Assert.Equal(850_000.50m, factura.Monto);
        Assert.Equal(new DateTime(2026, 11, 20, 5, 0, 0, DateTimeKind.Utc), factura.FechaVencimiento);
        var delCliente = (await cliente.GetFromJsonAsync<List<FacturaDto>>(
            $"/api/facturas?clienteId={EntornoPruebas.IdCliente1}"))!;
        Assert.Contains(delCliente, f => f.Numero == "FAC-9001");
    }

    [Fact]
    public async Task RegistrarFactura_ConNumeroExistente_Devuelve409()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/facturas", FacturaNueva("FAC-1001"));

        await AfirmarProblemaAsync(respuesta, HttpStatusCode.Conflict, ErroresAplicacion.CodigoNumeroFacturaDuplicado);
    }

    [Fact]
    public async Task RegistrarFactura_ConClienteInexistente_Devuelve404()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().PostAsJsonAsync(
            "/api/facturas", FacturaNueva("FAC-9002", ClienteInexistente));

        await AfirmarProblemaAsync(respuesta, HttpStatusCode.NotFound, ErroresAplicacion.CodigoClienteNoEncontrado);
    }

    [Fact]
    public async Task RegistrarFactura_ConDatosInvalidos_Devuelve400ConLosCampos()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().PostAsJsonAsync("/api/facturas", new
        {
            clienteId = EntornoPruebas.IdCliente1,
            numero = "FAC-9003",
            monto = 0,
            moneda = "pesos",
            fechaEmision = "2026-10-21",
            fechaVencimiento = "2026-10-01",
        });

        Assert.Equivalent(CamposFacturaInvalida, (await LeerErroresDeValidacionAsync(respuesta)).Keys);
    }

    [Fact]
    public async Task ProcesarFactura_AvanzaSoloEsaFacturaHastaDesactivarlaYLuegoDevuelve409()
    {
        await using var api = new FabricaApi(mongo);
        var cliente = api.CreateClient();
        var alta = await cliente.PostAsJsonAsync("/api/facturas", FacturaNueva("FAC-9004"));
        var creada = (await alta.Content.ReadFromJsonAsync<FacturaDto>())!;
        var ruta = new Uri($"/api/facturas/{creada.Id}/procesar", UriKind.Relative);

        var primera = (await (await cliente.PostAsync(ruta, content: null)).Content.ReadFromJsonAsync<ResultadoEjecucion>())!;
        var segunda = (await (await cliente.PostAsync(ruta, content: null)).Content.ReadFromJsonAsync<ResultadoEjecucion>())!;
        var tercera = await cliente.PostAsync(ruta, content: null);

        Assert.Equal(1, Assert.Single(primera.Clientes).CorreosEnviados);
        Assert.Equal(1, Assert.Single(segunda.Clientes).FacturasActualizadas);
        await AfirmarProblemaAsync(tercera, HttpStatusCode.Conflict, "Factura.EstadoFinal");

        var facturas = (await cliente.GetFromJsonAsync<List<FacturaDto>>("/api/facturas"))!;
        Assert.Equal("desactivado", facturas.Single(f => f.Id == creada.Id).Estado);
        Assert.Equal(6, facturas.Count(f => f.Estado == "primerrecordatorio"));
        Assert.Equal(2, (await cliente.GetFromJsonAsync<List<EnvioDto>>("/api/envios"))!.Count);
    }

    [Fact]
    public async Task ProcesarFactura_Inexistente_Devuelve404()
    {
        await using var api = new FabricaApi(mongo);

        var respuesta = await api.CreateClient().PostAsync(
            new Uri("/api/facturas/6716a2000000000000009999/procesar", UriKind.Relative), content: null);

        await AfirmarProblemaAsync(respuesta, HttpStatusCode.NotFound, ErroresAplicacion.CodigoFacturaNoEncontrada);
    }

    private static object FacturaNueva(string numero, string clienteId = EntornoPruebas.IdCliente1) => new
    {
        clienteId,
        numero,
        monto = 850_000.50m,
        moneda = "COP",
        fechaEmision = "2026-10-21",
        fechaVencimiento = "2026-11-20",
    };

    private static async Task<JsonDocument> LeerJsonAsync(HttpResponseMessage respuesta) =>
        await JsonDocument.ParseAsync(await respuesta.Content.ReadAsStreamAsync());

    private static async Task AfirmarProblemaAsync(HttpResponseMessage respuesta, HttpStatusCode estado, string codigo)
    {
        Assert.Equal(estado, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var json = await LeerJsonAsync(respuesta);
        Assert.Equal((int)estado, json.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(codigo, json.RootElement.GetProperty("codigo").GetString());
        Assert.False(string.IsNullOrWhiteSpace(json.RootElement.GetProperty("detail").GetString()));
    }

    private static async Task<Dictionary<string, string[]>> LeerErroresDeValidacionAsync(HttpResponseMessage respuesta)
    {
        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Equal("application/problem+json", respuesta.Content.Headers.ContentType?.MediaType);
        using var json = await LeerJsonAsync(respuesta);
        return json.RootElement.GetProperty("errors").Deserialize<Dictionary<string, string[]>>()!;
    }
}
