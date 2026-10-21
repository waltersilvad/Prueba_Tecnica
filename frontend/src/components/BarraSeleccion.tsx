import { Send, Trash2, X } from 'lucide-react'

interface Props {
  /** Cantidad de facturas marcadas. */
  cantidad: number
  /** Bloquea los botones mientras corre cualquier proceso que usa el bloqueo de la API. */
  bloqueada: boolean
  enviando: boolean
  eliminando: boolean
  onQuitarSeleccion: () => void
  onEnviarAvisos: () => void
  onEliminar: () => void
}

/** Acciones sobre las facturas marcadas: enviar sus avisos, eliminarlas o quitar la selección. */
export function BarraSeleccion({
  cantidad,
  bloqueada,
  enviando,
  eliminando,
  onQuitarSeleccion,
  onEnviarAvisos,
  onEliminar,
}: Props) {
  return (
    <div className="tarjeta flex flex-wrap items-center justify-between gap-4 py-4">
      <p className="text-sm text-texto-2" aria-live="polite">
        {cantidad === 0
          ? 'Marca las casillas de la derecha para enviar avisos o eliminar varias facturas a la vez.'
          : `${cantidad} ${cantidad === 1 ? 'factura seleccionada' : 'facturas seleccionadas'}. Las acciones individuales quedan bloqueadas.`}
      </p>
      <div className="flex flex-wrap gap-2">
        {cantidad > 0 && (
          <button
            type="button"
            className="boton-secundario"
            onClick={onQuitarSeleccion}
            disabled={bloqueada}
          >
            <X size={16} aria-hidden="true" />
            Quitar selección
          </button>
        )}
        <button
          type="button"
          className="boton"
          onClick={onEnviarAvisos}
          disabled={cantidad === 0 || bloqueada}
        >
          <Send size={16} aria-hidden="true" />
          {enviando
            ? 'Enviando avisos…'
            : `Enviar aviso de las facturas seleccionadas (${cantidad})`}
        </button>
        <button
          type="button"
          className="boton-peligro"
          onClick={onEliminar}
          disabled={cantidad === 0 || bloqueada}
        >
          <Trash2 size={16} aria-hidden="true" />
          {eliminando ? 'Eliminando…' : `Eliminar facturas seleccionadas (${cantidad})`}
        </button>
      </div>
    </div>
  )
}
