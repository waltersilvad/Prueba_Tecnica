import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// jsdom no implementa ResizeObserver, que usan las gráficas de Recharts para medir su contenedor.
class ResizeObserverDePrueba {
  observe() {}
  unobserve() {}
  disconnect() {}
}
globalThis.ResizeObserver ??= ResizeObserverDePrueba as unknown as typeof ResizeObserver

afterEach(() => cleanup())
