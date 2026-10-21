import { useSyncExternalStore } from 'react'

const CONSULTA = '(prefers-reduced-motion: reduce)'

function suscribir(alCambiar: () => void): () => void {
  if (typeof window.matchMedia !== 'function') {
    return () => {}
  }
  const consulta = window.matchMedia(CONSULTA)
  consulta.addEventListener('change', alCambiar)
  return () => consulta.removeEventListener('change', alCambiar)
}

/** Sin matchMedia (pruebas, entornos sin navegador) se asume movimiento reducido: nada se anima. */
function prefiereReducido(): boolean {
  return typeof window.matchMedia !== 'function' || window.matchMedia(CONSULTA).matches
}

/**
 * Indica si el usuario pidió reducir el movimiento en su sistema. Las clases CSS usan la variante
 * `motion-safe:`; este hook es para lo que se anima desde JavaScript (las gráficas de Recharts).
 */
export function useMovimientoReducido(): boolean {
  return useSyncExternalStore(suscribir, prefiereReducido, () => true)
}
