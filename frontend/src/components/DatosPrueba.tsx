import { DatabaseZap, Trash2 } from 'lucide-react'
import { ErrorApi, mensajeDeError } from '../api/clienteHttp'
import { useBorrarDatos, useCargarEjemplo, useProcesoEnCurso } from '../hooks/useFacturacion'

const CONFIRMACION_BORRAR =
  '¿Borrar todos los datos? Se eliminan todos los clientes, facturas y envíos (incluidos los de ejemplo) y los correos guardados como .eml. Esta acción no se puede deshacer.'

function mensajeDatosPrueba(error: unknown): string {
  return error instanceof ErrorApi && error.estado === 404
    ? 'Esta opción solo está disponible cuando la API corre en Development.'
    : mensajeDeError(error)
}

/** Administración de los datos de prueba, en la barra lateral: borrar todo (con confirmación) y cargar el ejemplo. */
export function DatosPrueba() {
  const borrar = useBorrarDatos()
  const cargar = useCargarEjemplo()
  const ocupado = useProcesoEnCurso()

  function alBorrar() {
    if (window.confirm(CONFIRMACION_BORRAR)) {
      cargar.reset()
      borrar.mutate()
    }
  }

  function alCargar() {
    borrar.reset()
    cargar.mutate()
  }

  const error = borrar.error ?? cargar.error

  return (
    <div className="space-y-2">
      <button type="button" className="boton-lateral" onClick={alCargar} disabled={ocupado}>
        <DatabaseZap size={16} aria-hidden="true" />
        {cargar.isPending ? 'Cargando…' : 'Cargar datos de ejemplo'}
      </button>
      <button
        type="button"
        className="boton-lateral enabled:hover:text-red-200"
        onClick={alBorrar}
        disabled={ocupado}
      >
        <Trash2 size={16} aria-hidden="true" />
        {borrar.isPending ? 'Borrando…' : 'Borrar todos los datos'}
      </button>

      {borrar.data && (
        <p className="px-1 text-xs text-lateral-tenue" role="status">
          Se borraron {borrar.data.clientes} clientes, {borrar.data.facturas} facturas,{' '}
          {borrar.data.envios} envíos y {borrar.data.correos} correos guardados.
        </p>
      )}
      {cargar.data && (
        <p className="px-1 text-xs text-lateral-tenue" role="status">
          Se cargaron {cargar.data.clientes} clientes y {cargar.data.facturas} facturas de ejemplo.
        </p>
      )}
      {error && (
        <p className="px-1 text-xs text-red-300" role="alert">
          {mensajeDatosPrueba(error)}
        </p>
      )}
    </div>
  )
}
