using Facturacion.Api.Configuracion;

namespace Facturacion.IntegrationTests.Api;

public sealed class ArchivoEnvTests
{
    [Fact]
    public void Interpretar_LeeClavesYValoresIgnorandoComentariosYLineasVacias()
    {
        var variables = ArchivoEnv.Interpretar(
        [
            "# Comentario",
            "",
            "Email__Proveedor=Brevo",
            "  Smtp__Usuario = login@smtp-brevo.com  ",
            "linea sin separador",
        ]);

        Assert.Equal(2, variables.Count);
        Assert.Equal("Brevo", variables["Email__Proveedor"]);
        Assert.Equal("login@smtp-brevo.com", variables["Smtp__Usuario"]);
    }

    [Fact]
    public void Interpretar_QuitaComillasYConservaSignosIgualEnElValor()
    {
        var variables = ArchivoEnv.Interpretar(
        [
            "Email__NombreRemitente=\"Cartera - Facturación\"",
            "Smtp__Clave='abc=123=='",
        ]);

        Assert.Equal("Cartera - Facturación", variables["Email__NombreRemitente"]);
        Assert.Equal("abc=123==", variables["Smtp__Clave"]);
    }

    [Fact]
    public void Interpretar_IgnoraValoresVaciosParaNoReemplazarLosValoresPorDefecto()
    {
        var variables = ArchivoEnv.Interpretar(["Email__Remitente=", "Smtp__Clave=\"\"", "=sin-clave"]);

        Assert.Empty(variables);
    }

    [Fact]
    public void Buscar_EncuentraElArchivoEnUnaCarpetaSuperior()
    {
        var raiz = Directory.CreateTempSubdirectory("archivo-env-");
        try
        {
            var profunda = Directory.CreateDirectory(Path.Combine(raiz.FullName, "backend", "src", "Api"));
            File.WriteAllText(Path.Combine(raiz.FullName, ArchivoEnv.NombreArchivo), "A=1");

            Assert.Equal(Path.Combine(raiz.FullName, ArchivoEnv.NombreArchivo), ArchivoEnv.Buscar(profunda.FullName));
        }
        finally
        {
            raiz.Delete(recursive: true);
        }
    }

    [Fact]
    public void Cargar_NoSobrescribeVariablesDeEntornoExistentes()
    {
        var existente = $"PRUEBA_ENV_EXISTENTE_{Guid.NewGuid():N}";
        var nueva = $"PRUEBA_ENV_NUEVA_{Guid.NewGuid():N}";
        var archivo = Path.GetTempFileName();
        try
        {
            Environment.SetEnvironmentVariable(existente, "valor-real");
            File.WriteAllLines(archivo, [$"{existente}=valor-del-archivo", $"{nueva}=cargado"]);

            var cargadas = ArchivoEnv.Cargar(archivo);

            Assert.Equal(1, cargadas);
            Assert.Equal("valor-real", Environment.GetEnvironmentVariable(existente));
            Assert.Equal("cargado", Environment.GetEnvironmentVariable(nueva));
        }
        finally
        {
            Environment.SetEnvironmentVariable(existente, null);
            Environment.SetEnvironmentVariable(nueva, null);
            File.Delete(archivo);
        }
    }
}
