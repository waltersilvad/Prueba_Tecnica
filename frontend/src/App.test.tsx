import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from './api/endpoints'
import { App } from './App'
import { clientesPrueba, enviosPrueba, resumenPrueba } from './test/datosPrueba'

vi.mock('./api/endpoints')

function renderizarEn(ruta: string) {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } })
  render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[ruta]}>
        <App />
      </MemoryRouter>
    </QueryClientProvider>,
  )
}

/** Límite para la primera importación de las vistas (Recharts es pesado y en CI se transforma en frío). */
const TIEMPO_PRECARGA_MS = 30_000

describe('App', () => {
  // Las vistas se cargan de forma diferida. Se importan una vez antes de las pruebas para que la primera no dependa
  // de lo rápido que la máquina transforme Recharts: `lazy()` sigue haciendo su import, pero ya lo encuentra en caché.
  beforeAll(async () => {
    await Promise.all([
      import('./pages/PanelPage'),
      import('./pages/ClientesPage'),
      import('./pages/FacturasPage'),
      import('./pages/EnviosPage'),
    ])
  }, TIEMPO_PRECARGA_MS)

  beforeEach(() => {
    vi.mocked(api.obtenerResumen).mockResolvedValue(resumenPrueba)
    vi.mocked(api.listarEnvios).mockResolvedValue(enviosPrueba)
    vi.mocked(api.listarClientes).mockResolvedValue(clientesPrueba)
    vi.mocked(api.listarFacturas).mockResolvedValue([])
  })

  it('abre el Panel como página de inicio, con la barra lateral y los datos de prueba', async () => {
    renderizarEn('/')

    expect(await screen.findByRole('heading', { name: 'Panel', level: 1 })).toBeInTheDocument()
    const navegacion = screen.getByRole('navigation', { name: 'Vistas' })
    expect(navegacion).toHaveTextContent('PanelClientesFacturasEnvíos')
    expect(screen.getByRole('link', { name: 'Panel' })).toHaveAttribute('aria-current', 'page')
    expect(
      screen.queryByRole('button', { name: 'Procesar todas las facturas' }),
    ).not.toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Borrar todos los datos' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Cargar datos de ejemplo' })).toBeInTheDocument()
    expect(screen.getByRole('radiogroup', { name: 'Tema' })).toBeInTheDocument()
  })

  it('navega entre vistas desde la barra lateral', async () => {
    renderizarEn('/panel')
    const usuario = userEvent.setup()

    await usuario.click(screen.getByRole('link', { name: 'Envíos' }))

    expect(
      await screen.findByRole('heading', { name: 'Historial de envíos', level: 1 }),
    ).toBeInTheDocument()
    expect(screen.getByRole('link', { name: 'Envíos' })).toHaveAttribute('aria-current', 'page')
  })

  it('la antigua ruta /estado redirige al Panel', async () => {
    renderizarEn('/estado')

    expect(await screen.findByRole('heading', { name: 'Panel', level: 1 })).toBeInTheDocument()
  })
})
