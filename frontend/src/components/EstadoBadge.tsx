import type { EstadoFactura } from '../api/tipos'
import { etiquetaEstado } from '../utils/formato'
import { usePaleta } from '../utils/paleta'

/** Estado de una factura: marca del color del estado (el mismo de las gráficas) y el texto en color de tinta. */
export function EstadoBadge({ estado }: { estado: EstadoFactura }) {
  const paleta = usePaleta()
  return (
    <span className="inline-flex items-center gap-2 whitespace-nowrap" title={estado}>
      <span
        className="size-2.5 shrink-0 rounded-full"
        style={{ background: paleta.estados[estado] }}
        aria-hidden="true"
      />
      {etiquetaEstado(estado)}
    </span>
  )
}
