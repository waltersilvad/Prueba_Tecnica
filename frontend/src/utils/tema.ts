import { useSyncExternalStore } from 'react'

export type PreferenciaTema = 'claro' | 'oscuro' | 'sistema'
export type Tema = 'claro' | 'oscuro'

/** Clave en localStorage. index.html la lee antes de cargar React para evitar un destello del tema equivocado. */
export const CLAVE_TEMA = 'facturacion.tema'
const CONSULTA_OSCURO = '(prefers-color-scheme: dark)'

const oyentes = new Set<() => void>()

function leerPreferencia(): PreferenciaTema {
  try {
    const guardada = window.localStorage.getItem(CLAVE_TEMA)
    return guardada === 'claro' || guardada === 'oscuro' ? guardada : 'sistema'
  } catch {
    // Almacenamiento bloqueado (modo privado, políticas del navegador): se sigue al sistema.
    return 'sistema'
  }
}

function sistemaEnOscuro(): boolean {
  return typeof window.matchMedia === 'function' && window.matchMedia(CONSULTA_OSCURO).matches
}

let preferencia: PreferenciaTema = leerPreferencia()

export function resolverTema(valor: PreferenciaTema): Tema {
  return valor === 'sistema' ? (sistemaEnOscuro() ? 'oscuro' : 'claro') : valor
}

function aplicar(tema: Tema): void {
  const raiz = document.documentElement
  raiz.dataset.theme = tema === 'oscuro' ? 'dark' : 'light'
  raiz.style.colorScheme = tema === 'oscuro' ? 'dark' : 'light'
}

/** Cambia la preferencia, la recuerda en el navegador y aplica el tema resultante. */
export function cambiarPreferenciaTema(nueva: PreferenciaTema): void {
  preferencia = nueva
  try {
    window.localStorage.setItem(CLAVE_TEMA, nueva)
  } catch {
    // Sin almacenamiento la elección dura solo esta sesión.
  }
  aplicar(resolverTema(nueva))
  oyentes.forEach((oyente) => oyente())
}

function suscribir(oyente: () => void): () => void {
  oyentes.add(oyente)
  const consulta =
    typeof window.matchMedia === 'function' ? window.matchMedia(CONSULTA_OSCURO) : null
  const alCambiarSistema = () => {
    if (preferencia === 'sistema') {
      aplicar(resolverTema('sistema'))
      oyente()
    }
  }
  consulta?.addEventListener('change', alCambiarSistema)
  return () => {
    oyentes.delete(oyente)
    consulta?.removeEventListener('change', alCambiarSistema)
  }
}

const instantanea = () => `${preferencia}|${resolverTema(preferencia)}`

/** Preferencia elegida, tema efectivo y función para cambiarlo. */
export function useTema(): {
  preferencia: PreferenciaTema
  tema: Tema
  cambiar: (nueva: PreferenciaTema) => void
} {
  const [valor, tema] = useSyncExternalStore(suscribir, instantanea, () => 'sistema|claro').split(
    '|',
  ) as [PreferenciaTema, Tema]
  return { preferencia: valor, tema, cambiar: cambiarPreferenciaTema }
}
