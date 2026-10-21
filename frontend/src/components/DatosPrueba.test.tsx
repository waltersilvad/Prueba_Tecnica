import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { ErrorApi } from '../api/clienteHttp'
import { api } from '../api/endpoints'
import { renderConConsultas } from '../test/utilidades'
import { DatosPrueba } from './DatosPrueba'

vi.mock('../api/endpoints')

describe('DatosPrueba', () => {
  beforeEach(() => {
    vi.spyOn(window, 'confirm').mockReturnValue(true)
  })

  it('pide confirmación antes de borrar y no hace nada si se cancela', async () => {
    vi.mocked(window.confirm).mockReturnValue(false)
    renderConConsultas(<DatosPrueba />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Borrar todos los datos' }))

    expect(window.confirm).toHaveBeenCalledOnce()
    expect(api.borrarDatos).not.toHaveBeenCalled()
  })

  it('al borrar muestra cuánto se eliminó, incluidos los correos guardados', async () => {
    vi.mocked(api.borrarDatos).mockResolvedValue({
      clientes: 3,
      facturas: 10,
      envios: 15,
      correos: 15,
    })
    renderConConsultas(<DatosPrueba />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Borrar todos los datos' }))

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Se borraron 3 clientes, 10 facturas, 15 envíos y 15 correos guardados.',
    )
  })

  it('carga los datos de ejemplo', async () => {
    vi.mocked(api.cargarEjemplo).mockResolvedValue({ clientes: 3, facturas: 10 })
    renderConConsultas(<DatosPrueba />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Cargar datos de ejemplo' }))

    expect(await screen.findByRole('status')).toHaveTextContent(
      'Se cargaron 3 clientes y 10 facturas de ejemplo.',
    )
  })

  it('si la base tiene datos, muestra el mensaje de la API (409)', async () => {
    vi.mocked(api.cargarEjemplo).mockRejectedValue(
      new ErrorApi(
        'La base ya tiene datos. Bórralos primero para cargar los datos de ejemplo.',
        409,
        'Datos.BaseConDatos',
      ),
    )
    renderConConsultas(<DatosPrueba />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Cargar datos de ejemplo' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Bórralos primero')
  })

  it('explica que solo existe en Development cuando la API responde 404', async () => {
    vi.mocked(api.borrarDatos).mockRejectedValue(new ErrorApi('Not Found', 404))
    renderConConsultas(<DatosPrueba />)

    await userEvent.setup().click(screen.getByRole('button', { name: 'Borrar todos los datos' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('solo está disponible')
  })
})
