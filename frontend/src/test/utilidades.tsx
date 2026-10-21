import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import type { Factura } from '../api/tipos'

/** Renderiza con un QueryClient nuevo por prueba y sin reintentos, para que los errores se vean al instante. */
export function renderConConsultas(interfaz: ReactElement) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  return render(<QueryClientProvider client={queryClient}>{interfaz}</QueryClientProvider>)
}

export function crearFactura(cambios: Partial<Factura> = {}): Factura {
  return {
    id: 'id-FAC-1001',
    numero: 'FAC-1001',
    clienteId: 'cliente-1',
    nombreCliente: 'Comercial Andina S.A.S.',
    monto: 1_250_000,
    moneda: 'COP',
    fechaEmision: '2026-09-01T05:00:00Z',
    fechaVencimiento: '2026-10-01T05:00:00Z',
    estado: 'primerrecordatorio',
    fechaActualizacion: '2026-10-21T15:00:00Z',
    ...cambios,
  }
}

/** Promesa que la prueba resuelve cuando quiere, para observar el estado "en curso". */
export function promesaControlada<T>() {
  let resolver!: (valor: T) => void
  let rechazar!: (error: unknown) => void
  const promesa = new Promise<T>((res, rej) => {
    resolver = res
    rechazar = rej
  })
  return { promesa, resolver, rechazar }
}
