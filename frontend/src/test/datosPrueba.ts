import type { Cliente, Envio, ResumenCliente } from '../api/tipos'

export const clientesPrueba: Cliente[] = [
  {
    id: 'cliente-1',
    nombre: 'Comercial Andina S.A.S.',
    email: 'cartera.andina@example.com',
    fechaActualizacion: '2026-10-21T15:00:00Z',
  },
  {
    id: 'cliente-2',
    nombre: 'Distribuidora del Sur Ltda.',
    email: 'pagos.sur@example.com',
    fechaActualizacion: '2026-10-21T15:00:00Z',
  },
]

export const resumenPrueba: ResumenCliente[] = [
  {
    clienteId: 'cliente-1',
    nombre: 'Comercial Andina S.A.S.',
    email: 'cartera.andina@example.com',
    primerRecordatorio: 2,
    segundoRecordatorio: 1,
    desactivado: 1,
    totalFacturas: 4,
    montoPendiente: [{ moneda: 'COP', monto: 4_395_500.5 }],
  },
  {
    clienteId: 'cliente-2',
    nombre: 'Distribuidora del Sur Ltda.',
    email: 'pagos.sur@example.com',
    primerRecordatorio: 1,
    segundoRecordatorio: 2,
    desactivado: 0,
    totalFacturas: 3,
    montoPendiente: [{ moneda: 'COP', monto: 2_865_250.75 }],
  },
]

export const enviosPrueba: Envio[] = [
  {
    ejecucionId: 'ejecucion-2',
    clienteId: 'cliente-1',
    facturaId: 'factura-1',
    tipoAviso: 'Desactivacion',
    destinatario: 'cartera.andina@example.com',
    fecha: '2026-10-21T19:48:47Z',
    resultado: 'Enviado',
    error: null,
  },
  {
    ejecucionId: 'ejecucion-1',
    clienteId: 'cliente-1',
    facturaId: 'factura-1',
    tipoAviso: 'Desactivacion',
    destinatario: 'cartera.andina@example.com',
    fecha: '2026-10-21T19:44:42Z',
    resultado: 'Fallido',
    error: '535: 5.7.8 Authentication failed',
  },
]
