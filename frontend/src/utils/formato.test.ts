import { describe, expect, it } from 'vitest'
import { etiquetaEstado, formatearFecha, formatearMoneda } from './formato'

describe('formatearMoneda', () => {
  it('usa separadores de es-CO y la moneda indicada', () => {
    expect(formatearMoneda(1_250_000.5, 'COP')).toMatch(/1\.250\.000,50/)
    expect(formatearMoneda(1_250_000.5, 'COP')).toContain('$')
  })

  it('respeta la moneda de cada factura', () => {
    expect(formatearMoneda(10, 'USD')).toMatch(/US\$|USD/)
  })
})

describe('formatearFecha', () => {
  it('muestra la fecha en formato dd/MM/yyyy', () => {
    expect(formatearFecha('2026-10-01T05:00:00Z')).toBe('01/10/2026')
  })

  it('convierte de UTC a la hora de Bogotá antes de tomar el día', () => {
    // 03:00 UTC del 1 de octubre son las 22:00 del 30 de septiembre en Bogotá (UTC-5).
    expect(formatearFecha('2026-10-01T03:00:00Z')).toBe('30/09/2026')
  })
})

describe('etiquetaEstado', () => {
  it('traduce los valores exactos de la base a texto legible', () => {
    expect(etiquetaEstado('primerrecordatorio')).toBe('Primer recordatorio')
    expect(etiquetaEstado('segundorecordatorio')).toBe('Segundo recordatorio')
    expect(etiquetaEstado('desactivado')).toBe('Desactivado')
  })
})
