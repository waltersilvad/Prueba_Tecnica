import type { ReactNode } from 'react'

interface Props {
  /** Id del título, para que la sección lo use como nombre accesible (aria-labelledby). */
  id: string
  titulo: string
  descripcion?: ReactNode
  /** Acción principal de la vista (por ejemplo, "Nueva factura"), alineada a la derecha. */
  accion?: ReactNode
}

/** Encabezado común: todas las vistas empiezan con la misma tipografía y el mismo espaciado. */
export function EncabezadoVista({ id, titulo, descripcion, accion }: Props) {
  return (
    <header className="flex flex-wrap items-end justify-between gap-4">
      <div className="space-y-1">
        <h1 id={id} className="text-2xl font-semibold tracking-tight">
          {titulo}
        </h1>
        {descripcion && <p className="text-sm text-texto-2">{descripcion}</p>}
      </div>
      {accion}
    </header>
  )
}
