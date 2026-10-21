import type { EstadoFactura, TipoAviso } from '../api/tipos'
import { configuracion } from '../configuracion'

const formatosMoneda = new Map<string, Intl.NumberFormat>()

/**
 * Formatea un monto con la moneda de la propia factura (por ejemplo, COP o USD).
 * Siempre con 2 decimales: CLDR define COP sin decimales y redondearía 1.250.000,50 a 1.250.001,
 * mostrando un valor distinto al de la factura (y al del correo, que también usa 2 decimales).
 */
export function formatearMoneda(monto: number, moneda: string): string {
  let formato = formatosMoneda.get(moneda)
  if (!formato) {
    formato = new Intl.NumberFormat(configuracion.cultura, {
      style: 'currency',
      currency: moneda,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    })
    formatosMoneda.set(moneda, formato)
  }
  return formato.format(monto)
}

const formatoFecha = new Intl.DateTimeFormat(configuracion.cultura, {
  timeZone: configuracion.zonaHoraria,
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
})

/** Convierte una fecha ISO en UTC al formato dd/MM/yyyy en la zona horaria de Bogotá. */
export function formatearFecha(fechaIso: string): string {
  const partes = Object.fromEntries(
    formatoFecha.formatToParts(new Date(fechaIso)).map((parte) => [parte.type, parte.value]),
  )
  return `${partes.day}/${partes.month}/${partes.year}`
}

/** Fecha de calendario (yyyy-MM-dd) en la zona del negocio, desplazada `dias` días desde `desde`. */
export function fechaCalendario(dias = 0, desde: Date = new Date()): string {
  const fecha = new Date(desde.getTime() + dias * 24 * 60 * 60 * 1000)
  const partes = Object.fromEntries(
    formatoFecha.formatToParts(fecha).map((parte) => [parte.type, parte.value]),
  )
  return `${partes.year}-${partes.month}-${partes.day}`
}

const formatosCompactos = new Map<string, Intl.NumberFormat>()

/** Monto abreviado para indicadores (por ejemplo, "$ 12,6 M"). El valor completo va en tablas y tooltips. */
export function formatearMonedaCompacta(monto: number, moneda: string): string {
  let formato = formatosCompactos.get(moneda)
  if (!formato) {
    formato = new Intl.NumberFormat(configuracion.cultura, {
      style: 'currency',
      currency: moneda,
      notation: 'compact',
      maximumFractionDigits: 1,
    })
    formatosCompactos.set(moneda, formato)
  }
  return formato.format(monto)
}

const formatoFechaHora = new Intl.DateTimeFormat(configuracion.cultura, {
  timeZone: configuracion.zonaHoraria,
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  hourCycle: 'h23',
})

/** Fecha y hora (dd/MM/yyyy HH:mm) en la zona horaria de Bogotá. */
export function formatearFechaHora(fechaIso: string): string {
  const partes = Object.fromEntries(
    formatoFechaHora.formatToParts(new Date(fechaIso)).map((parte) => [parte.type, parte.value]),
  )
  return `${partes.day}/${partes.month}/${partes.year} ${partes.hour}:${partes.minute}`
}

const ETIQUETAS_AVISO: Record<TipoAviso, string> = {
  SegundoRecordatorio: 'Segundo recordatorio',
  Desactivacion: 'Aviso de desactivación',
}

export function etiquetaAviso(aviso: TipoAviso): string {
  return ETIQUETAS_AVISO[aviso]
}

const ETIQUETAS_ESTADO: Record<EstadoFactura, string> = {
  primerrecordatorio: 'Primer recordatorio',
  segundorecordatorio: 'Segundo recordatorio',
  desactivado: 'Desactivado',
}

export function etiquetaEstado(estado: EstadoFactura): string {
  return ETIQUETAS_ESTADO[estado]
}
