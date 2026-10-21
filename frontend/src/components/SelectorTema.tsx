import { Monitor, Moon, Sun, type LucideIcon } from 'lucide-react'
import { useTema, type PreferenciaTema } from '../utils/tema'

const OPCIONES: { valor: PreferenciaTema; etiqueta: string; icono: LucideIcon }[] = [
  { valor: 'claro', etiqueta: 'Claro', icono: Sun },
  { valor: 'oscuro', etiqueta: 'Oscuro', icono: Moon },
  { valor: 'sistema', etiqueta: 'Sistema', icono: Monitor },
]

/** Control segmentado Claro / Oscuro / Sistema. La elección se recuerda en el navegador. */
export function SelectorTema() {
  const { preferencia, cambiar } = useTema()

  return (
    <div
      role="radiogroup"
      aria-label="Tema"
      className="flex w-full gap-1 rounded-lg bg-lateral-hover p-1"
    >
      {OPCIONES.map(({ valor, etiqueta, icono: Icono }) => (
        <button
          key={valor}
          type="button"
          role="radio"
          aria-checked={preferencia === valor}
          onClick={() => cambiar(valor)}
          className="flex flex-1 items-center justify-center gap-1.5 rounded-md px-2 py-1.5 text-xs font-medium text-lateral-tenue transition-colors hover:text-white aria-checked:bg-lateral-activo aria-checked:text-white"
        >
          <Icono size={14} aria-hidden="true" />
          {etiqueta}
        </button>
      ))}
    </div>
  )
}
