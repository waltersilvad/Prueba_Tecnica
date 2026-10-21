import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ErrorApi } from '../api/clienteHttp'
import type { Cliente } from '../api/tipos'
import { FormularioFactura } from './FormularioFactura'

const clientes: Cliente[] = [
  {
    id: 'cliente-1',
    nombre: 'Comercial Andina S.A.S.',
    email: 'cartera.andina@example.com',
    fechaActualizacion: '2026-10-21T15:00:00Z',
  },
]

function renderizar(onGuardar = vi.fn().mockResolvedValue(undefined)) {
  render(<FormularioFactura clientes={clientes} onGuardar={onGuardar} onCancelar={vi.fn()} />)
  return { onGuardar, usuario: userEvent.setup() }
}

async function completar(usuario: ReturnType<typeof userEvent.setup>) {
  await usuario.selectOptions(screen.getByLabelText('Cliente'), 'cliente-1')
  await usuario.type(screen.getByLabelText('Número'), ' FAC-9001 ')
  await usuario.type(screen.getByLabelText('Monto'), '850000.50')
  await usuario.clear(screen.getByLabelText('Fecha de emisión'))
  await usuario.type(screen.getByLabelText('Fecha de emisión'), '2026-10-21')
  await usuario.clear(screen.getByLabelText('Fecha de vencimiento'))
  await usuario.type(screen.getByLabelText('Fecha de vencimiento'), '2026-11-20')
}

describe('FormularioFactura', () => {
  it('no pide estado: indica que la factura nace en Primer recordatorio', () => {
    renderizar()

    expect(screen.getByText('Primer recordatorio')).toBeInTheDocument()
    expect(screen.queryByLabelText(/estado/i)).not.toBeInTheDocument()
    expect(screen.getByLabelText('Moneda')).toHaveValue('COP')
  })

  it('envía los datos normalizados con el monto como número', async () => {
    const { onGuardar, usuario } = renderizar()

    await completar(usuario)
    await usuario.click(screen.getByRole('button', { name: 'Crear factura' }))

    expect(onGuardar).toHaveBeenCalledWith({
      clienteId: 'cliente-1',
      numero: 'FAC-9001',
      monto: 850_000.5,
      moneda: 'COP',
      fechaEmision: '2026-10-21',
      fechaVencimiento: '2026-11-20',
    })
  })

  it('valida en el navegador y no llama a la API con datos inválidos', async () => {
    const { onGuardar, usuario } = renderizar()

    await usuario.clear(screen.getByLabelText('Fecha de vencimiento'))
    await usuario.type(screen.getByLabelText('Fecha de vencimiento'), '2000-01-01')
    await usuario.click(screen.getByRole('button', { name: 'Crear factura' }))

    expect(onGuardar).not.toHaveBeenCalled()
    expect(screen.getByText('Selecciona el cliente.')).toBeInTheDocument()
    expect(screen.getByText('El número de factura es obligatorio.')).toBeInTheDocument()
    expect(screen.getByText('El monto debe ser mayor que cero.')).toBeInTheDocument()
    expect(
      screen.getByText('La fecha de vencimiento no puede ser anterior a la de emisión.'),
    ).toBeInTheDocument()
  })

  it('muestra junto al número el error de número repetido (409) de la API', async () => {
    const onGuardar = vi
      .fn()
      .mockRejectedValue(
        new ErrorApi(
          "Ya existe una factura con el número 'FAC-9001'.",
          409,
          'Factura.NumeroDuplicado',
        ),
      )
    const { usuario } = renderizar(onGuardar)

    await completar(usuario)
    await usuario.click(screen.getByRole('button', { name: 'Crear factura' }))

    expect(
      await screen.findByText("Ya existe una factura con el número 'FAC-9001'."),
    ).toBeInTheDocument()
    expect(screen.getByLabelText('Número')).toHaveAttribute('aria-invalid', 'true')
  })
})
