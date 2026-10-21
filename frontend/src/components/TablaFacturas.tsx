import { Send, Trash2 } from 'lucide-react'
import { useEffect, useRef } from 'react'
import type { Factura } from '../api/tipos'
import { formatearFecha, formatearMoneda } from '../utils/formato'
import { EstadoBadge } from './EstadoBadge'

/** Selección de facturas para enviar avisos o eliminar varias a la vez. */
export interface SeleccionFacturas {
  ids: ReadonlySet<string>
  alCambiar: (factura: Factura, marcada: boolean) => void
  /** Marca o desmarca todas las facturas de la tabla. */
  alCambiarTodas: (marcar: boolean) => void
}

interface Props {
  facturas: Factura[]
  /** Si se indica, cada fila muestra el botón "Enviar aviso" para procesar solo esa factura. */
  onEnviarAviso?: (factura: Factura) => void
  /** Si se indica, cada fila muestra el botón "Eliminar" para borrar solo esa factura. */
  onEliminar?: (factura: Factura) => void
  /** Si se indica, cada fila tiene una casilla a la derecha para seleccionarla. */
  seleccion?: SeleccionFacturas
  /** Id de la factura que se está procesando (su botón muestra el progreso). */
  facturaEnProceso?: string
  /** Deshabilita todas las acciones, por ejemplo mientras corre otro proceso. */
  accionesBloqueadas?: boolean
}

/** Una factura desactivada ya no admite avisos (sí se puede seleccionar y eliminar). */
const admiteAviso = (factura: Factura) => factura.estado !== 'desactivado'

/** Casilla del encabezado: marcada si están todas, "parcial" (indeterminada) si solo algunas. */
function CasillaTodas({
  marcadas,
  total,
  deshabilitada,
  alCambiar,
}: {
  marcadas: number
  total: number
  deshabilitada: boolean
  alCambiar: (marcar: boolean) => void
}) {
  const referencia = useRef<HTMLInputElement>(null)
  useEffect(() => {
    if (referencia.current) {
      referencia.current.indeterminate = marcadas > 0 && marcadas < total
    }
  }, [marcadas, total])

  return (
    <input
      ref={referencia}
      type="checkbox"
      className="size-4 accent-acento"
      aria-label="Seleccionar todas las facturas visibles"
      checked={total > 0 && marcadas === total}
      disabled={deshabilitada || total === 0}
      onChange={(e) => alCambiar(e.target.checked)}
    />
  )
}

/** Tabla de facturas. En pantallas angostas cada fila se muestra como tarjeta (ver .tabla-responsiva). */
export function TablaFacturas({
  facturas,
  onEnviarAviso,
  onEliminar,
  seleccion,
  facturaEnProceso,
  accionesBloqueadas = false,
}: Props) {
  const marcadas = seleccion ? facturas.filter((f) => seleccion.ids.has(f.id)).length : 0
  // Modo selección: con al menos una casilla marcada, las acciones individuales se bloquean.
  const enModoSeleccion = (seleccion?.ids.size ?? 0) > 0

  return (
    <div className="tabla-contenedor tabla-responsiva-contenedor">
      <table className="tabla tabla-responsiva">
        <thead>
          <tr>
            <th scope="col">Cliente</th>
            <th scope="col">Número</th>
            <th scope="col" className="text-right">
              Monto
            </th>
            <th scope="col">Vencimiento</th>
            <th scope="col">Estado</th>
            {(onEnviarAviso || onEliminar) && <th scope="col">Acciones</th>}
            {seleccion && (
              <th scope="col" className="w-12 text-center">
                <CasillaTodas
                  marcadas={marcadas}
                  total={facturas.length}
                  deshabilitada={accionesBloqueadas}
                  alCambiar={seleccion.alCambiarTodas}
                />
              </th>
            )}
          </tr>
        </thead>
        <tbody>
          {facturas.map((factura) => {
            const desactivada = !admiteAviso(factura)
            const enProceso = facturaEnProceso === factura.id
            const marcada = seleccion?.ids.has(factura.id) ?? false
            return (
              <tr key={factura.id} className={marcada ? 'bg-acento-suave' : undefined}>
                <td data-label="Cliente">{factura.nombreCliente}</td>
                <td data-label="Número" className="font-mono">
                  {factura.numero}
                </td>
                <td data-label="Monto" className="text-right whitespace-nowrap tabular-nums">
                  {formatearMoneda(factura.monto, factura.moneda)}
                </td>
                <td data-label="Vencimiento" className="whitespace-nowrap tabular-nums">
                  {formatearFecha(factura.fechaVencimiento)}
                </td>
                <td data-label="Estado">
                  <EstadoBadge estado={factura.estado} />
                </td>
                {(onEnviarAviso || onEliminar) && (
                  <td data-label="Acciones">
                    <div className="flex flex-wrap justify-end gap-2 md:justify-start">
                      {onEnviarAviso && (
                        <button
                          type="button"
                          className="boton-secundario boton-pequeno"
                          onClick={() => onEnviarAviso(factura)}
                          disabled={desactivada || accionesBloqueadas || enModoSeleccion}
                          title={
                            desactivada
                              ? 'La factura está desactivada: no requiere más avisos.'
                              : enModoSeleccion
                                ? 'Hay facturas seleccionadas: usa los botones de la selección.'
                                : 'Envía el aviso de esta factura y, si sale, avanza su estado.'
                          }
                          aria-label={`Enviar aviso de ${factura.numero}`}
                        >
                          <Send size={14} aria-hidden="true" />
                          {enProceso ? 'Enviando…' : 'Enviar aviso'}
                        </button>
                      )}
                      {onEliminar && (
                        <button
                          type="button"
                          className="boton-peligro boton-pequeno"
                          onClick={() => onEliminar(factura)}
                          disabled={accionesBloqueadas || enModoSeleccion}
                          title={
                            enModoSeleccion
                              ? 'Hay facturas seleccionadas: usa los botones de la selección.'
                              : 'Elimina esta factura y su historial de envíos.'
                          }
                          aria-label={`Eliminar ${factura.numero}`}
                        >
                          <Trash2 size={14} aria-hidden="true" />
                          Eliminar
                        </button>
                      )}
                    </div>
                  </td>
                )}
                {seleccion && (
                  <td data-label="Seleccionar" className="text-center">
                    <input
                      type="checkbox"
                      className="size-4 accent-acento"
                      aria-label={`Seleccionar ${factura.numero}`}
                      checked={marcada}
                      disabled={accionesBloqueadas}
                      onChange={(e) => seleccion.alCambiar(factura, e.target.checked)}
                    />
                  </td>
                )}
              </tr>
            )
          })}
        </tbody>
      </table>
    </div>
  )
}
