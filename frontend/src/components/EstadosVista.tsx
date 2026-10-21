import type { ReactNode } from 'react'
import { mensajeDeError } from '../api/clienteHttp'

type Forma = 'lista' | 'tarjetas' | 'panel'

function Bloque({ className }: { className: string }) {
  return <div className={`rounded-lg bg-hundida motion-safe:animate-pulse ${className}`} />
}

/**
 * Silueta de carga con la forma del contenido que viene. Solo se muestra en la primera carga: al refrescar,
 * TanStack Query conserva los datos anteriores y la vista no parpadea.
 */
export function Cargando({
  texto = 'Cargando…',
  forma = 'lista',
}: {
  texto?: string
  forma?: Forma
}) {
  return (
    <div role="status" aria-live="polite">
      <span className="sr-only">{texto}</span>
      {forma === 'panel' && (
        <div className="space-y-6" aria-hidden="true">
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
            {[0, 1, 2, 3].map((i) => (
              <Bloque key={i} className="h-28" />
            ))}
          </div>
          <div className="grid gap-4 lg:grid-cols-5">
            <Bloque className="h-72 lg:col-span-2" />
            <Bloque className="h-72 lg:col-span-3" />
          </div>
        </div>
      )}
      {forma === 'tarjetas' && (
        <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3" aria-hidden="true">
          {[0, 1, 2].map((i) => (
            <Bloque key={i} className="h-24" />
          ))}
        </div>
      )}
      {forma === 'lista' && (
        <div className="space-y-2" aria-hidden="true">
          <Bloque className="h-11" />
          {[0, 1, 2, 3, 4].map((i) => (
            <Bloque key={i} className="h-14" />
          ))}
        </div>
      )}
    </div>
  )
}

export function MensajeVacio({ children }: { children: ReactNode }) {
  return (
    <p className="rounded-xl border border-dashed border-borde bg-superficie px-6 py-10 text-center text-sm text-texto-2">
      {children}
    </p>
  )
}

export function MensajeError({
  error,
  onReintentar,
}: {
  error: unknown
  onReintentar?: () => void
}) {
  return (
    <div className="alerta-error flex flex-wrap items-center justify-between gap-3" role="alert">
      <span>{mensajeDeError(error)}</span>
      {onReintentar && (
        <button type="button" className="boton-secundario boton-pequeno" onClick={onReintentar}>
          Reintentar
        </button>
      )}
    </div>
  )
}
