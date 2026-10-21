import type { LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'

interface Props {
  etiqueta: string
  valor: string | number
  icono: LucideIcon
  /** Detalle opcional debajo del valor (por ejemplo, el valor sin abreviar o un desglose). */
  detalle?: ReactNode
  /** Posición en la fila: escalona la animación de entrada. */
  orden?: number
}

/** Indicador del panel: etiqueta, valor destacado y detalle opcional. */
export function TarjetaIndicador({ etiqueta, valor, icono: Icono, detalle, orden = 0 }: Props) {
  return (
    <article
      aria-label={etiqueta}
      className="tarjeta space-y-2 motion-safe:animate-aparecer"
      style={{ animationDelay: `${orden * 60}ms` }}
    >
      <div className="flex items-center justify-between gap-2">
        <span className="text-sm text-texto-2">{etiqueta}</span>
        <span
          className="grid size-8 place-items-center rounded-lg bg-acento-suave text-acento"
          aria-hidden="true"
        >
          <Icono size={18} />
        </span>
      </div>
      <strong className="block text-3xl leading-none font-semibold">{valor}</strong>
      {detalle && <div className="text-xs break-words text-tenue">{detalle}</div>}
    </article>
  )
}
