import type { EstadoFactura } from '../api/tipos'
import { useTema } from './tema'

/**
 * Colores de datos (gráficas y etiquetas de estado). Única fuente de verdad: la interfaz y las gráficas los
 * leen de aquí. Validados con el validador de la guía de visualización:
 * - Estados: rampa ordinal de un solo tono (azul), monótona en luminosidad y con contraste suficiente sobre el
 *   fondo, en modo claro (pasos 250/450/650) y oscuro (250/450/600).
 * - Monto pendiente: una sola serie en naranja, distinto del azul de los estados para no confundir significados.
 * Los colores de texto nunca son colores de datos: el color lo lleva una marca junto al texto.
 */
export interface Paleta {
  superficie: string
  textoSecundario: string
  textoTenue: string
  rejilla: string
  estados: Record<EstadoFactura, string>
  montoPendiente: string
}

const CLARO: Paleta = {
  superficie: '#fcfcfb',
  textoSecundario: '#52514e',
  textoTenue: '#898781',
  rejilla: '#e1e0d9',
  estados: {
    primerrecordatorio: '#86b6ef',
    segundorecordatorio: '#2a78d6',
    desactivado: '#104281',
  },
  montoPendiente: '#eb6834',
}

const OSCURO: Paleta = {
  superficie: '#1a1a19',
  textoSecundario: '#c3c2b7',
  textoTenue: '#898781',
  rejilla: '#2c2c2a',
  estados: {
    primerrecordatorio: '#86b6ef',
    segundorecordatorio: '#2a78d6',
    desactivado: '#184f95',
  },
  montoPendiente: '#d95926',
}

/** Paleta del tema efectivo (el que eligió el usuario o, con "Sistema", el del sistema operativo). */
export function usePaleta(): Paleta {
  return useTema().tema === 'oscuro' ? OSCURO : CLARO
}
