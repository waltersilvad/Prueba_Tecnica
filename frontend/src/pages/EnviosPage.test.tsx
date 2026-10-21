import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { api } from '../api/endpoints'
import { clientesPrueba, enviosPrueba } from '../test/datosPrueba'
import { crearFactura, renderConConsultas } from '../test/utilidades'
import { EnviosPage } from './EnviosPage'

vi.mock('../api/endpoints')

function renderizar() {
  vi.mocked(api.listarEnvios).mockResolvedValue(enviosPrueba)
  vi.mocked(api.listarClientes).mockResolvedValue(clientesPrueba)
  vi.mocked(api.listarFacturas).mockResolvedValue([
    crearFactura({ id: 'factura-1', numero: 'PROBANDO' }),
  ])
  renderConConsultas(<EnviosPage />)
}

describe('EnviosPage', () => {
  it('muestra cada envío con fecha en Bogotá, cliente, factura, aviso y resultado', async () => {
    renderizar()

    const filas = (await screen.findAllByRole('row')).slice(1)
    expect(filas).toHaveLength(2)

    const enviado = within(filas[0])
    expect(enviado.getByText('21/10/2026 14:48')).toBeInTheDocument()
    expect(await enviado.findByText('Comercial Andina S.A.S.')).toBeInTheDocument()
    expect(await enviado.findByText('PROBANDO')).toBeInTheDocument()
    expect(enviado.getByText('Aviso de desactivación')).toBeInTheDocument()
    expect(enviado.getByText('Enviado')).toBeInTheDocument()

    const fallido = within(filas[1])
    expect(fallido.getByText('Fallido')).toBeInTheDocument()
    expect(fallido.getByText('535: 5.7.8 Authentication failed')).toBeInTheDocument()
  })

  it('cada celda lleva su encabezado para mostrarse como tarjeta en pantallas angostas', async () => {
    renderizar()

    const fila = (await screen.findAllByRole('row'))[1]
    const etiquetas = within(fila)
      .getAllByRole('cell')
      .map((celda) => celda.getAttribute('data-label'))
    expect(etiquetas).toEqual(['Fecha', 'Factura', 'Aviso', 'Destinatario', 'Resultado'])
  })

  it('filtra el historial por cliente', async () => {
    renderizar()
    const usuario = userEvent.setup()
    await screen.findAllByRole('row')

    await usuario.selectOptions(
      await screen.findByLabelText('Cliente'),
      await screen.findByRole('option', { name: 'Distribuidora del Sur Ltda.' }),
    )

    expect(api.listarEnvios).toHaveBeenLastCalledWith('cliente-2')
  })

  it('explica qué hacer cuando todavía no hay envíos', async () => {
    vi.mocked(api.listarEnvios).mockResolvedValue([])
    vi.mocked(api.listarClientes).mockResolvedValue([])
    vi.mocked(api.listarFacturas).mockResolvedValue([])
    renderConConsultas(<EnviosPage />)

    expect(await screen.findByText(/Todavía no hay envíos/)).toBeInTheDocument()
  })
})
