import { FileText, LayoutDashboard, Receipt, Send, Users, type LucideIcon } from 'lucide-react'
import { NavLink } from 'react-router'
import { DatosPrueba } from './DatosPrueba'
import { SelectorTema } from './SelectorTema'

const VISTAS: { ruta: string; titulo: string; icono: LucideIcon }[] = [
  { ruta: '/panel', titulo: 'Panel', icono: LayoutDashboard },
  { ruta: '/clientes', titulo: 'Clientes', icono: Users },
  { ruta: '/facturas', titulo: 'Facturas', icono: FileText },
  { ruta: '/envios', titulo: 'Envíos', icono: Send },
]

/**
 * Navegación principal. Desde pantallas grandes (lg) es una barra lateral fija; en pantallas angostas se apila
 * arriba del contenido sin desplazamiento horizontal. La vista activa se marca con aria-current (accesible).
 */
export function BarraLateral() {
  return (
    <aside className="flex flex-col gap-4 bg-lateral px-4 py-4 text-lateral-texto lg:sticky lg:top-0 lg:h-screen lg:gap-6 lg:px-3 lg:py-6">
      <div className="flex items-center gap-3 px-2">
        <span
          className="grid size-9 place-items-center rounded-lg bg-lateral-activo"
          aria-hidden="true"
        >
          <Receipt size={20} />
        </span>
        <div className="leading-tight">
          <strong className="block text-base">Facturación</strong>
          <span className="text-xs text-lateral-tenue">Recordatorios de cobro</span>
        </div>
      </div>

      <nav aria-label="Vistas" className="flex flex-wrap gap-1 lg:flex-col">
        {VISTAS.map(({ ruta, titulo, icono: Icono }) => (
          <NavLink
            key={ruta}
            to={ruta}
            className="flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium text-lateral-texto transition-colors hover:bg-lateral-hover aria-[current=page]:bg-lateral-activo aria-[current=page]:text-white"
          >
            <Icono size={18} aria-hidden="true" />
            {titulo}
          </NavLink>
        ))}
      </nav>

      <div className="grid gap-3 sm:grid-cols-2 lg:mt-auto lg:grid-cols-1">
        <SelectorTema />
        <DatosPrueba />
      </div>
    </aside>
  )
}
