import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import { ErrorApi } from '../api/clienteHttp'
import type { Cliente } from '../api/tipos'
import { FormularioCliente } from './FormularioCliente'

const cliente: Cliente = {
  id: 'cliente-1',
  nombre: 'Comercial Andina S.A.S.',
  email: 'cartera.andina@example.com',
  fechaActualizacion: '2026-10-21T15:00:00Z',
}

function renderizar(onGuardar = vi.fn().mockResolvedValue(undefined)) {
  const onCancelar = vi.fn()
  render(
    <FormularioCliente
      etiqueta={`Editar ${cliente.nombre}`}
      inicial={{ nombre: cliente.nombre, email: cliente.email }}
      onGuardar={onGuardar}
      onCancelar={onCancelar}
    />,
  )
  return { onGuardar, onCancelar, usuario: userEvent.setup() }
}

describe('FormularioCliente', () => {
  it('parte de los datos actuales del cliente', () => {
    renderizar()

    expect(screen.getByLabelText('Nombre')).toHaveValue('Comercial Andina S.A.S.')
    expect(screen.getByLabelText('Correo')).toHaveValue('cartera.andina@example.com')
  })

  it('envía nombre y correo sin espacios sobrantes', async () => {
    const { onGuardar, usuario } = renderizar()

    await usuario.clear(screen.getByLabelText('Correo'))
    await usuario.type(screen.getByLabelText('Correo'), '  evaluador@example.com ')
    await usuario.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(onGuardar).toHaveBeenCalledWith({
      nombre: 'Comercial Andina S.A.S.',
      email: 'evaluador@example.com',
    })
  })

  it('valida en el navegador y no llama a la API con datos inválidos', async () => {
    const { onGuardar, usuario } = renderizar()

    await usuario.clear(screen.getByLabelText('Nombre'))
    await usuario.clear(screen.getByLabelText('Correo'))
    await usuario.type(screen.getByLabelText('Correo'), 'correo-invalido')
    await usuario.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(onGuardar).not.toHaveBeenCalled()
    expect(screen.getByText('El nombre es obligatorio.')).toBeInTheDocument()
    expect(screen.getByText('El correo no tiene un formato válido.')).toBeInTheDocument()
    expect(screen.getByLabelText('Correo')).toHaveAttribute('aria-invalid', 'true')
  })

  it('muestra junto al campo los errores de validación que devuelve la API', async () => {
    const onGuardar = vi.fn().mockRejectedValue(
      new ErrorApi('Los datos enviados no son válidos.', 400, undefined, {
        email: ['El correo ya está en uso.'],
      }),
    )
    const { usuario } = renderizar(onGuardar)

    await usuario.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByText('El correo ya está en uso.')).toBeInTheDocument()
    expect(screen.getByLabelText('Correo')).toHaveAttribute('aria-invalid', 'true')
  })

  it('muestra un error general cuando la API falla sin errores de campo', async () => {
    const onGuardar = vi
      .fn()
      .mockRejectedValue(new ErrorApi("No existe un cliente con id 'cliente-1'.", 404))
    const { usuario } = renderizar(onGuardar)

    await usuario.click(screen.getByRole('button', { name: 'Guardar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent(
      "No existe un cliente con id 'cliente-1'.",
    )
    expect(screen.getByRole('button', { name: 'Guardar' })).toBeEnabled()
  })

  it('sirve también para crear: arranca vacío y exige ambos campos', async () => {
    const onGuardar = vi.fn().mockResolvedValue(undefined)
    render(
      <FormularioCliente etiqueta="Nuevo cliente" onGuardar={onGuardar} onCancelar={vi.fn()} />,
    )
    const usuario = userEvent.setup()

    expect(screen.getByRole('form', { name: 'Nuevo cliente' })).toBeInTheDocument()
    expect(screen.getByLabelText('Nombre')).toHaveValue('')

    await usuario.click(screen.getByRole('button', { name: 'Guardar' }))
    expect(onGuardar).not.toHaveBeenCalled()
    expect(screen.getByText('El correo es obligatorio.')).toBeInTheDocument()

    await usuario.type(screen.getByLabelText('Nombre'), 'Industrias Pacífico')
    await usuario.type(screen.getByLabelText('Correo'), 'pagos@pacifico.example.com')
    await usuario.click(screen.getByRole('button', { name: 'Guardar' }))
    expect(onGuardar).toHaveBeenCalledWith({
      nombre: 'Industrias Pacífico',
      email: 'pagos@pacifico.example.com',
    })
  })

  it('permite cancelar la edición', async () => {
    const { onCancelar, usuario } = renderizar()

    await usuario.click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(onCancelar).toHaveBeenCalledOnce()
  })
})
