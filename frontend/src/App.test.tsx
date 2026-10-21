import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter } from 'react-router'
import { beforeEach, describe, expect, it, vi } from 'vitest'
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

describe('App', () => {
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
