import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { api } from '../api/endpoints'
import { enviosPrueba, resumenPrueba } from '../test/datosPrueba'
import { crearFactura, renderConConsultas } from '../test/utilidades'
import { PanelPage } from './PanelPage'

vi.mock('../api/endpoints')

function renderizar() {
  vi.mocked(api.obtenerResumen).mockResolvedValue(resumenPrueba)
  vi.mocked(api.listarEnvios).mockResolvedValue(enviosPrueba)
  renderConConsultas(<PanelPage />)
}

describe('PanelPage', () => {
  it('muestra los indicadores calculados con los datos de la API', async () => {
    renderizar()

    const facturas = await screen.findByRole('article', { name: 'Facturas' })
    expect(within(facturas).getByText('7')).toBeInTheDocument()
    expect(within(facturas).getByText('2 clientes')).toBeInTheDocument()

    const pendientes = screen.getByRole('article', { name: 'Pendientes de aviso' })
    expect(within(pendientes).getByText('6')).toBeInTheDocument()
    expect(within(pendientes).getByText('1 desactivadas')).toBeInTheDocument()

    const monto = screen.getByRole('article', { name: 'Monto pendiente' })
    expect(within(monto).getByText(/7\.260\.751,25/)).toBeInTheDocument()

    const avisos = screen.getByRole('article', { name: 'Avisos enviados' })
    expect(await within(avisos).findByText('1')).toBeInTheDocument()
    expect(within(avisos).getByText(/1 fallidos/)).toBeInTheDocument()
  })

  it('la leyenda de facturas por estado incluye cada valor, sin depender del color', async () => {
    renderizar()

    const grafico = await screen.findByRole('figure', { name: 'Facturas por estado' })
    const leyenda = within(grafico).getAllByRole('list')[0]
    expect(within(leyenda).getByText('Primer recordatorio').parentElement).toHaveTextContent(
      'Primer recordatorio343 %',
    )
    expect(within(leyenda).getByText('Desactivado').parentElement).toHaveTextContent(
      'Desactivado114 %',
    )
  })

  it('cada gráfica ofrece sus datos en una tabla', async () => {
    renderizar()
    const usuario = userEvent.setup()

    const grafico = await screen.findByRole('figure', {
      name: 'Monto pendiente por cliente (COP)',
    })
    await usuario.click(within(grafico).getByText('Ver datos en tabla'))

    const tabla = within(grafico).getByRole('table')
    expect(
      within(tabla).getByRole('row', { name: /Comercial Andina S\.A\.S\./ }),
    ).toHaveTextContent(/4\.395\.500,50/)
    expect(screen.getAllByText('Ver datos en tabla')).toHaveLength(3)
  })

  it('con un cliente elegido muestra sus números, sus facturas y sus envíos', async () => {
    renderizar()
    vi.mocked(api.listarFacturas).mockResolvedValue([crearFactura({ numero: 'FAC-1001' })])
    const usuario = userEvent.setup()

    await usuario.selectOptions(await screen.findByLabelText('Ver'), 'Comercial Andina S.A.S.')

    const facturas = screen.getByRole('article', { name: 'Facturas' })
    expect(within(facturas).getByText('4')).toBeInTheDocument()
    expect(within(facturas).getByText('cartera.andina@example.com')).toBeInTheDocument()
    expect(api.listarEnvios).toHaveBeenLastCalledWith('cliente-1')
    expect(await screen.findByText('Facturas de Comercial Andina S.A.S.')).toBeInTheDocument()
    expect(await screen.findByRole('cell', { name: 'FAC-1001' })).toBeInTheDocument()
    expect(screen.getByText('Últimos envíos')).toBeInTheDocument()
    // Con un solo cliente no se muestran las gráficas que comparan clientes.
    expect(
      screen.queryByRole('figure', { name: 'Estado de las facturas por cliente' }),
    ).not.toBeInTheDocument()
  })

  it('muestra un mensaje con reintento si la API falla', async () => {
    vi.mocked(api.obtenerResumen).mockRejectedValue(new Error('API caída'))
    vi.mocked(api.listarEnvios).mockResolvedValue([])
    renderConConsultas(<PanelPage />)

    expect(await screen.findByRole('alert')).toHaveTextContent('API caída')
    expect(screen.getByRole('button', { name: 'Reintentar' })).toBeInTheDocument()
  })
})
