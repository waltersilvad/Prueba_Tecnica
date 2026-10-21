/** Configuración de la aplicación. La URL de la API se define con VITE_API_URL al compilar. */
export const configuracion = {
  urlApi: (import.meta.env.VITE_API_URL ?? 'http://localhost:5080').replace(/\/$/, ''),
  cultura: 'es-CO',
  zonaHoraria: 'America/Bogota',
} as const
