import '@testing-library/jest-dom/vitest'
import { cleanup, configure } from '@testing-library/react'
import { afterEach } from 'vitest'

// Las vistas se cargan de forma diferida: con la máquina ocupada (por ejemplo, en CI) importar una vista puede pasar
// del segundo que esperan por defecto los findBy*. 3 s evita fallos intermitentes sin ocultar errores reales.
configure({ asyncUtilTimeout: 3000 })

// jsdom no implementa ResizeObserver, que usan las gráficas de Recharts para medir su contenedor.
class ResizeObserverDePrueba {
  observe() {}
  unobserve() {}
  disconnect() {}
}
globalThis.ResizeObserver ??= ResizeObserverDePrueba as unknown as typeof ResizeObserver

afterEach(() => cleanup())
