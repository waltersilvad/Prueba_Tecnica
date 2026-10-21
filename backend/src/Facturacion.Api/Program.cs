using Facturacion.Api.Configuracion;
using Facturacion.Api.Endpoints;
using Facturacion.Application;
using Facturacion.Infrastructure;
using Facturacion.Infrastructure.Correo;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

// Desarrollo local: el mismo .env de la raíz sirve para F5 / dotnet run y para Docker Compose.
// Debe ir antes de crear el builder, que es cuando se leen las variables de entorno.
var variablesEnv = ArchivoEnv.CargarSiEsDesarrollo(Directory.GetCurrentDirectory());

var builder = WebApplication.CreateBuilder(args);

// Composición: cada capa registra lo suyo; la API solo las une.
builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApi(builder.Configuration);

var app = builder.Build();

if (variablesEnv > 0)
{
    RegistroArranque.ArchivoEnvCargado(app.Logger, variablesEnv);
}

var proveedorCorreo = app.Services.GetRequiredService<IOptions<OpcionesEmail>>().Value.Proveedor;
RegistroArranque.ProveedorCorreo(app.Logger, proveedorCorreo);

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseCors(ServiciosApi.PoliticaCors);

// Documentación disponible en todos los entornos: no expone secretos y facilita la evaluación.
app.MapOpenApi();
app.MapScalarApiReference(opciones => opciones.WithTitle("Recordatorios de facturas"));
app.MapGet("/", () => TypedResults.Redirect("/scalar")).ExcludeFromDescription();

app.MapSalud();
app.MapRecordatorios();
app.MapClientes();
app.MapFacturas();
app.MapConsultas();

if (app.Environment.IsDevelopment())
{
    app.MapDatosPrueba();
}

await app.RunAsync();
