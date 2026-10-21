// Tipos alineados con los DTOs de la API (Facturacion.Application). Las fechas llegan en ISO 8601 UTC.

export const ESTADOS_FACTURA = ['primerrecordatorio', 'segundorecordatorio', 'desactivado'] as const

export type EstadoFactura = (typeof ESTADOS_FACTURA)[number]

export interface Cliente {
  id: string
  nombre: string
  email: string
  fechaActualizacion: string
}

export interface DatosCliente {
  nombre: string
  email: string
}

export interface Factura {
  id: string
  numero: string
  clienteId: string
  nombreCliente: string
  monto: number
  moneda: string
  fechaEmision: string
  fechaVencimiento: string
  estado: EstadoFactura
  fechaActualizacion: string
}

/** Datos de una factura nueva. Las fechas son de calendario (yyyy-MM-dd); el estado inicial lo fija la API. */
export interface DatosFacturaNueva {
  clienteId: string
  numero: string
  monto: number
  moneda: string
  fechaEmision: string
  fechaVencimiento: string
}

export interface FiltrosFacturas {
  estado?: EstadoFactura
  clienteId?: string
}

export interface MontoPorMoneda {
  moneda: string
  monto: number
}

export interface ResumenCliente {
  clienteId: string
  nombre: string
  email: string
  primerRecordatorio: number
  segundoRecordatorio: number
  desactivado: number
  totalFacturas: number
  montoPendiente: MontoPorMoneda[]
}

export interface FacturaOmitida {
  numero: string
  motivo: string
}

export interface ResultadoCliente {
  clienteId: string
  nombre: string
  correosEnviados: number
  facturasActualizadas: number
  omitidas: FacturaOmitida[]
  errores: string[]
  conflictos: string[]
}

export type TipoAviso = 'SegundoRecordatorio' | 'Desactivacion'

export type ResultadoEnvio = 'Enviado' | 'Fallido'

/** Registro del historial de envíos. */
export interface Envio {
  ejecucionId: string
  clienteId: string
  facturaId: string
  tipoAviso: TipoAviso
  destinatario: string
  fecha: string
  resultado: ResultadoEnvio
  error: string | null
}

/** Lo que se eliminó junto con un cliente. */
export interface ResultadoEliminacionCliente {
  clienteId: string
  nombre: string
  facturasEliminadas: number
  enviosEliminados: number
}

/** Lo que se eliminó junto con una o varias facturas. */
export interface ResultadoEliminacionFacturas {
  numeros: string[]
  facturasEliminadas: number
  enviosEliminados: number
}

/** Lo que se eliminó al borrar todos los datos. */
export interface ResumenBorrado {
  clientes: number
  facturas: number
  envios: number
  correos: number
}

/** Lo que se cargó como datos de ejemplo. */
export interface ResumenCargaEjemplo {
  clientes: number
  facturas: number
}

export interface ResultadoEjecucion {
  ejecucionId: string
  clientes: ResultadoCliente[]
}
