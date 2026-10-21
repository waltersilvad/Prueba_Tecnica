# API de recordatorios de facturas. Contexto de compilación: carpeta backend/.

# ---------- Compilación ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /src

# Primero solo los archivos de proyecto: la restauración queda en caché mientras no cambien las dependencias.
COPY global.json Directory.Build.props .editorconfig ./
COPY src/Facturacion.Domain/Facturacion.Domain.csproj src/Facturacion.Domain/
COPY src/Facturacion.Application/Facturacion.Application.csproj src/Facturacion.Application/
COPY src/Facturacion.Infrastructure/Facturacion.Infrastructure.csproj src/Facturacion.Infrastructure/
COPY src/Facturacion.Api/Facturacion.Api.csproj src/Facturacion.Api/
RUN dotnet restore src/Facturacion.Api/Facturacion.Api.csproj

COPY src/ src/
RUN dotnet publish src/Facturacion.Api/Facturacion.Api.csproj -c Release -o /app --no-restore

# ---------- Ejecución ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS ejecucion
WORKDIR /app
COPY --from=compilacion /app .

# Carpeta del proveedor de correo Consola, con permisos para el usuario sin privilegios de la imagen.
RUN mkdir -p /app/salida-correos && chown "$APP_UID" /app/salida-correos

USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Facturacion.Api.dll"]
