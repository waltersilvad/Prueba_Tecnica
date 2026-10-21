import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/endpoints'
import { clientesPrueba, resumenPrueba } from '../test/datosPrueba'
import { renderConConsultas } from '../test/utilidades'
import { ClientesPage } from './ClientesPage'

vi.mock('../api/endpoints')

describe('ClientesPage: eliminar cliente', () => {
  beforeEach(() => {
    vi.mocked(api.listarClientes).mockResolvedValue(clientesPrueba)
    vi.mocked(api.obtenerResumen).mockResolvedValue(resumenPrueba)
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  it('la confirmación avisa cuántas facturas se eliminarán', async () => {
    vi.mocked(window.confirm).mockReturnValue(false)
    renderConConsultas(<ClientesPage />)
    const usuario = userEvent.setup()
    await screen.findByText('Comercial Andina S.A.S.')
    // Espera a que el resumen esté disponible para contar sus facturas.
    await vi.waitFor(() => expect(api.obtenerResumen).toHaveBeenCalled())

    await usuario.click(screen.getByRole('button', { name: 'Eliminar Comercial Andina S.A.S.' }))

    expect(window.confirm).toHaveBeenCalledWith(expect.stringContaining('sus 4 facturas'))
    expect(api.eliminarCliente).not.toHaveBeenCalled()
  })

  it('al confirmar elimina el cliente y muestra qué se borró', async () => {
    vi.mocked(api.eliminarCliente).mockResolvedValue({
      clienteId: 'cliente-1',
      nombre: 'Comercial Andina S.A.S.',
      facturasEliminadas: 4,
      enviosEliminados: 3,
    })
    renderConConsultas(<ClientesPage />)
    const usuario = userEvent.setup()

    await usuario.click(
      await screen.findByRole('button', { name: 'Eliminar Comercial Andina S.A.S.' }),
    )

    expect(api.eliminarCliente).toHaveBeenCalledWith('cliente-1')
    expect(await screen.findByRole('status')).toHaveTextContent(
      'Se eliminó Comercial Andina S.A.S. junto con 4 facturas y 3 envíos.',
    )
  })
})
