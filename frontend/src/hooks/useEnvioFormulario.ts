import { useState } from 'react'
import { ErrorApi, mensajeDeError, type ErroresCampo } from '../api/clienteHttp'

interface Opciones<T> {
  /** Validación en el navegador; si devuelve errores, no se llama a la API. */
  validar: (datos: T) => ErroresCampo
  guardar: (datos: T) => Promise<unknown>
  /** Traduce errores de la API identificados por su código a errores de un campo concreto. */
  erroresPorCodigo?: (error: ErrorApi) => ErroresCampo | undefined
}

/**
 * Flujo común de los formularios: valida, envía y reparte los errores de la API entre los campos
 * (si vienen por campo) o un mensaje general.
 */
export function useEnvioFormulario<T>({ validar, guardar, erroresPorCodigo }: Opciones<T>) {
  const [errores, setErrores] = useState<ErroresCampo>({})
  const [errorGeneral, setErrorGeneral] = useState<string>()
  const [guardando, setGuardando] = useState(false)

  async function enviar(datos: T) {
    setErrorGeneral(undefined)

    const erroresLocales = validar(datos)
    setErrores(erroresLocales)
    if (Object.keys(erroresLocales).length > 0) {
      return
    }

    setGuardando(true)
    try {
      await guardar(datos)
    } catch (error) {
      const deCampo =
        error instanceof ErrorApi ? { ...error.erroresCampo, ...erroresPorCodigo?.(error) } : {}
      setErrores(deCampo)
      if (Object.keys(deCampo).length === 0) {
        setErrorGeneral(mensajeDeError(error))
      }
    } finally {
      setGuardando(false)
    }
  }

  return { errores, errorGeneral, guardando, enviar }
}
