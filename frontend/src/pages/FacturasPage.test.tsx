import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/endpoints'
import type { ResultadoEjecucion } from '../api/tipos'
import { clientesPrueba } from '../test/datosPrueba'
import { crearFactura, renderConConsultas } from '../test/utilidades'
import { FacturasPage } from './FacturasPage'

vi.mock('../api/endpoints')

const facturas = [
  crearFactura({ id: 'f1', numero: 'FAC-1001' }),
  crearFactura({ id: 'f2', numero: 'FAC-1002', estado: 'segundorecordatorio' }),
  crearFactura({ id: 'f3', numero: 'FAC-1004', estado: 'desactivado' }),
]

const resultado: ResultadoEjecucion = {
  ejecucionId: 'ejecucion-1',
  clientes: [
    {
      clienteId: 'cliente-1',
      nombre: 'Comercial Andina S.A.S.',
      correosEnviados: 2,
      facturasActualizadas: 2,
      omitidas: [],
      errores: [],
      conflictos: [],
    },
  ],
}

const botonEliminarLote = () =>
  screen.getByRole('button', { name: /Eliminar facturas seleccionadas/ })
const botonLote = () =>
  screen.getByRole('button', { name: /Enviar aviso de las facturas seleccionadas/ })

describe('FacturasPage: selección de facturas', () => {
  beforeEach(() => {
    vi.mocked(api.listarFacturas).mockResolvedValue(facturas)
    vi.mocked(api.listarClientes).mockResolvedValue(clientesPrueba)
  })

  it('sin selección, el envío en lote está bloqueado y los avisos individuales disponibles', async () => {
    renderConConsultas(<FacturasPage />)

    expect(await screen.findByRole('button', { name: 'Enviar aviso de FAC-1001' })).toBeEnabled()
    expect(botonLote()).toBeDisabled()
    // Las desactivadas no admiten avisos, pero sí se pueden seleccionar (para eliminarlas).
    expect(screen.getByRole('checkbox', { name: 'Seleccionar FAC-1004' })).toBeEnabled()
    expect(screen.getByRole('button', { name: 'Enviar aviso de FAC-1004' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Eliminar FAC-1004' })).toBeEnabled()
    expect(botonEliminarLote()).toBeDisabled()
  })

  it('desde la primera casilla bloquea los avisos individuales y habilita el envío en lote', async () => {
    renderConConsultas(<FacturasPage />)
    const usuario = userEvent.setup()

    await usuario.click(await screen.findByRole('checkbox', { name: 'Seleccionar FAC-1001' }))

    expect(botonLote()).toBeEnabled()
    expect(botonLote()).toHaveTextContent('(1)')
    expect(screen.getByRole('button', { name: 'Enviar aviso de FAC-1001' })).toBeDisabled()
    expect(screen.getByRole('button', { name: 'Enviar aviso de FAC-1002' })).toBeDisabled()
  })

  it('la casilla del encabezado marca todas las facturas visibles', async () => {
    renderConConsultas(<FacturasPage />)
    const usuario = userEvent.setup()

    await usuario.click(
      await screen.findByRole('checkbox', { name: 'Seleccionar todas las facturas visibles' }),
    )

    expect(screen.getByRole('checkbox', { name: 'Seleccionar FAC-1001' })).toBeChecked()
    expect(screen.getByRole('checkbox', { name: 'Seleccionar FAC-1002' })).toBeChecked()
    expect(screen.getByRole('checkbox', { name: 'Seleccionar FAC-1004' })).toBeChecked()
    expect(botonLote()).toHaveTextContent('(3)')
  })

  it('envía los avisos de la selección, muestra el resultado y limpia la selección', async () => {
    vi.mocked(api.procesarFacturas).mockResolvedValue(resultado)
    renderConConsultas(<FacturasPage />)
    const usuario = userEvent.setup()

    await usuario.click(await screen.findByRole('checkbox', { name: 'Seleccionar FAC-1001' }))
    await usuario.click(screen.getByRole('checkbox', { name: 'Seleccionar FAC-1002' }))
    await usuario.click(botonLote())

    expect(api.procesarFacturas).toHaveBeenCalledWith(['f1', 'f2'])
    const panel = await screen.findByRole('region', { name: 'Resultado del proceso' })
    expect(within(panel).getByText(/2 correos enviados/)).toBeInTheDocument()
    expect(botonLote()).toHaveTextContent('(0)')
    expect(screen.getByRole('button', { name: 'Enviar aviso de FAC-1001' })).toBeEnabled()
  })

  it('"Quitar selección" vuelve al modo individual', async () => {
    renderConConsultas(<FacturasPage />)
    const usuario = userEvent.setup()

    await usuario.click(await screen.findByRole('checkbox', { name: 'Seleccionar FAC-1001' }))
    await usuario.click(screen.getByRole('button', { name: 'Quitar selección' }))

    expect(screen.getByRole('checkbox', { name: 'Seleccionar FAC-1001' })).not.toBeChecked()
    expect(screen.getByRole('button', { name: 'Enviar aviso de FAC-1001' })).toBeEnabled()
  })

  it('en modo selección también se bloquea el botón Eliminar individual', async () => {
    renderConConsultas(<FacturasPage />)
    const usuario = userEvent.setup()

    await usuario.click(await screen.findByRole('checkbox', { name: 'Seleccionar FAC-1001' }))

    expect(screen.getByRole('button', { name: 'Eliminar FAC-1002' })).toBeDisabled()
    expect(botonEliminarLote()).toBeEnabled()
    expect(botonEliminarLote()).toHaveTextContent('(1)')
  })
})

describe('FacturasPage: eliminar facturas', () => {
  beforeEach(() => {
    vi.mocked(api.listarFacturas).mockResolvedValue(facturas)
    vi.mocked(api.listarClientes).mockResolvedValue(clientesPrueba)
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  it('elimina una factura tras confirmar y muestra qué se borró', async () => {
    vi.mocked(api.eliminarFactura).mockResolvedValue({
      numeros: ['FAC-1001'],
      facturasEliminadas: 1,
      enviosEliminados: 2,
    })
    renderConConsultas(<FacturasPage />)

    await userEvent.setup().click(await screen.findByRole('button', { name: 'Eliminar FAC-1001' }))

    expect(window.confirm).toHaveBeenCalledWith(expect.stringContaining('FAC-1001'))
    expect(api.eliminarFactura).toHaveBeenCalledWith('f1')
    expect(await screen.findByRole('status')).toHaveTextContent(
      'Se eliminó la factura FAC-1001 y 2 envíos de su historial.',
    )
  })

  it('si se cancela la confirmación no elimina nada', async () => {
    vi.mocked(window.confirm).mockReturnValue(false)
    renderConConsultas(<FacturasPage />)

    await userEvent.setup().click(await screen.findByRole('button', { name: 'Eliminar FAC-1001' }))

    expect(api.eliminarFactura).not.toHaveBeenCalled()
  })

  it('elimina la selección, incluida una desactivada, y limpia la selección', async () => {
    vi.mocked(api.eliminarFacturas).mockResolvedValue({
      numeros: ['FAC-1001', 'FAC-1004'],
      facturasEliminadas: 2,
      enviosEliminados: 0,
    })
    renderConConsultas(<FacturasPage />)
    const usuario = userEvent.setup()

    await usuario.click(await screen.findByRole('checkbox', { name: 'Seleccionar FAC-1001' }))
    await usuario.click(screen.getByRole('checkbox', { name: 'Seleccionar FAC-1004' }))
    await usuario.click(botonEliminarLote())

    expect(window.confirm).toHaveBeenCalledWith(
      expect.stringContaining('las 2 facturas seleccionadas'),
    )
    expect(api.eliminarFacturas).toHaveBeenCalledWith(['f1', 'f3'])
    expect(await screen.findByRole('status')).toHaveTextContent('Se eliminó 2 facturas')
    expect(botonEliminarLote()).toHaveTextContent('(0)')
  })
})
