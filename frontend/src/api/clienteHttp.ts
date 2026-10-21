import { configuracion } from '../configuracion'

/** Errores de validación por campo, con las claves en camelCase como en el JSON. */
export type ErroresCampo = Record<string, string[]>

/** Error de la API construido a partir de su respuesta ProblemDetails. */
export class ErrorApi extends Error {
  readonly estado: number
  readonly codigo?: string
  readonly erroresCampo: ErroresCampo

  constructor(mensaje: string, estado: number, codigo?: string, erroresCampo: ErroresCampo = {}) {
    super(mensaje)
    this.name = 'ErrorApi'
    this.estado = estado
    this.codigo = codigo
    this.erroresCampo = erroresCampo
  }
}

interface ProblemDetails {
  title?: string
  detail?: string
  codigo?: string
  errors?: ErroresCampo
}

/** Hace la petición a la API y devuelve el JSON tipado, o lanza un {@link ErrorApi}. */
export async function solicitar<T>(ruta: string, opciones: RequestInit = {}): Promise<T> {
  let respuesta: Response
  try {
    respuesta = await fetch(`${configuracion.urlApi}${ruta}`, {
      ...opciones,
      headers: {
        Accept: 'application/json',
        ...(opciones.body ? { 'Content-Type': 'application/json' } : {}),
        ...opciones.headers,
      },
    })
  } catch {
    throw new ErrorApi(
      `No se pudo conectar con la API en ${configuracion.urlApi}. Verifica que esté en ejecución.`,
      0,
    )
  }

  if (!respuesta.ok) {
    throw await crearError(respuesta)
  }

  return respuesta.status === 204 ? (undefined as T) : ((await respuesta.json()) as T)
}

async function crearError(respuesta: Response): Promise<ErrorApi> {
  let problema: ProblemDetails = {}
  try {
    problema = (await respuesta.json()) as ProblemDetails
  } catch {
    // Respuesta sin cuerpo JSON: se usa el código de estado.
  }

  return new ErrorApi(
    problema.detail ?? problema.title ?? `La API respondió con el código ${respuesta.status}.`,
    respuesta.status,
    problema.codigo,
    problema.errors ?? {},
  )
}

/** Mensaje legible para mostrar cualquier error en la interfaz. */
export function mensajeDeError(error: unknown): string {
  return error instanceof Error ? error.message : 'Ocurrió un error inesperado.'
}
