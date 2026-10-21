import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { crearFactura } from '../test/utilidades'
import { TablaFacturas } from './TablaFacturas'

describe('TablaFacturas', () => {
  it('muestra cliente, número, monto, vencimiento y estado de cada factura', () => {
    render(
      <TablaFacturas
        facturas={[
          crearFactura(),
          crearFactura({
            id: 'id-FAC-2002',
            numero: 'FAC-2002',
            nombreCliente: 'Distribuidora del Sur Ltda.',
            monto: 1_575_250.75,
            fechaVencimiento: '2026-09-20T05:00:00Z',
            estado: 'segundorecordatorio',
          }),
        ]}
      />,
    )

    const filas = screen.getAllByRole('row').slice(1)
    expect(filas).toHaveLength(2)

    const primera = within(filas[0])
    expect(primera.getByText('Comercial Andina S.A.S.')).toBeInTheDocument()
    expect(primera.getByText('FAC-1001')).toBeInTheDocument()
    expect(primera.getByText(/1\.250\.000,00/)).toBeInTheDocument()
    expect(primera.getByText('01/10/2026')).toBeInTheDocument()
    expect(primera.getByText('Primer recordatorio')).toBeInTheDocument()

    const segunda = within(filas[1])
    expect(segunda.getByText(/1\.575\.250,75/)).toBeInTheDocument()
    expect(segunda.getByText('20/09/2026')).toBeInTheDocument()
    expect(segunda.getByText('Segundo recordatorio')).toHaveAttribute(
      'title',
      'segundorecordatorio',
    )
  })

  it('con onEnviarAviso, cada fila permite procesar solo esa factura', async () => {
    const onEnviarAviso = vi.fn()
    const pendiente = crearFactura()
    render(<TablaFacturas facturas={[pendiente]} onEnviarAviso={onEnviarAviso} />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Enviar aviso de FAC-1001' }))

    expect(onEnviarAviso).toHaveBeenCalledWith(pendiente)
  })

  it('no permite enviar avisos a facturas desactivadas', () => {
    render(
      <TablaFacturas
        facturas={[crearFactura({ estado: 'desactivado' })]}
        onEnviarAviso={vi.fn()}
      />,
    )

    expect(screen.getByRole('button', { name: 'Enviar aviso de FAC-1001' })).toBeDisabled()
  })

  it('muestra el progreso en la factura que se está procesando y bloquea las demás', () => {
    render(
      <TablaFacturas
        facturas={[crearFactura(), crearFactura({ id: 'id-FAC-1002', numero: 'FAC-1002' })]}
        onEnviarAviso={vi.fn()}
        facturaEnProceso="id-FAC-1001"
        accionesBloqueadas
      />,
    )

    expect(screen.getByRole('button', { name: 'Enviar aviso de FAC-1001' })).toHaveTextContent(
      'Enviando…',
    )
    expect(screen.getByRole('button', { name: 'Enviar aviso de FAC-1002' })).toBeDisabled()
  })

  it('tiene encabezados de columna accesibles', () => {
    render(<TablaFacturas facturas={[crearFactura()]} />)

    const encabezados = screen.getAllByRole('columnheader').map((th) => th.textContent)
    expect(encabezados).toEqual(['Cliente', 'Número', 'Monto', 'Vencimiento', 'Estado'])
  })
})
