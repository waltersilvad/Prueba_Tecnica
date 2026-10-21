import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { CLAVE_TEMA, cambiarPreferenciaTema } from '../utils/tema'
import { SelectorTema } from './SelectorTema'

/** Simula la preferencia de color del sistema operativo. */
function sistemaEnOscuro(oscuro: boolean) {
  vi.stubGlobal(
    'matchMedia',
    vi.fn().mockImplementation((consulta: string) => ({
      matches: oscuro && consulta === '(prefers-color-scheme: dark)',
      media: consulta,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
    })),
  )
}

describe('SelectorTema', () => {
  afterEach(() => {
    cambiarPreferenciaTema('sistema')
    window.localStorage.clear()
    vi.unstubAllGlobals()
  })

  it('ofrece Claro, Oscuro y Sistema, con Sistema marcado por defecto', () => {
    render(<SelectorTema />)

    const opciones = screen.getAllByRole('radio')
    expect(opciones.map((o) => o.textContent)).toEqual(['Claro', 'Oscuro', 'Sistema'])
    expect(screen.getByRole('radio', { name: 'Sistema' })).toHaveAttribute('aria-checked', 'true')
  })

  it('al elegir Oscuro aplica el tema oscuro y recuerda la elección', async () => {
    render(<SelectorTema />)

    await userEvent.setup().click(screen.getByRole('radio', { name: 'Oscuro' }))

    expect(document.documentElement.dataset.theme).toBe('dark')
    expect(window.localStorage.getItem(CLAVE_TEMA)).toBe('oscuro')
    expect(screen.getByRole('radio', { name: 'Oscuro' })).toHaveAttribute('aria-checked', 'true')
    expect(screen.getByRole('radio', { name: 'Sistema' })).toHaveAttribute('aria-checked', 'false')
  })

  it('con Sistema sigue la preferencia del sistema operativo', async () => {
    sistemaEnOscuro(true)
    render(<SelectorTema />)
    const usuario = userEvent.setup()

    await usuario.click(screen.getByRole('radio', { name: 'Claro' }))
    expect(document.documentElement.dataset.theme).toBe('light')

    await usuario.click(screen.getByRole('radio', { name: 'Sistema' }))
    expect(document.documentElement.dataset.theme).toBe('dark')
  })
})
