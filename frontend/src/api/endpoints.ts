import { solicitar } from './clienteHttp'
import type {
  Cliente,
  DatosCliente,
  DatosFacturaNueva,
  Envio,
  Factura,
  FiltrosFacturas,
  ResultadoEjecucion,
  ResultadoEliminacionCliente,
  ResultadoEliminacionFacturas,
  ResumenBorrado,
  ResumenCargaEjemplo,
  ResumenCliente,
} from './tipos'

function consulta(parametros: Record<string, string | undefined>): string {
  const definidos = Object.entries(parametros).filter((par): par is [string, string] => !!par[1])
  return definidos.length > 0 ? `?${new URLSearchParams(definidos).toString()}` : ''
}

/** Operaciones de la API. Es la única puerta de salida HTTP del frontend. */
export const api = {
  listarClientes: () => solicitar<Cliente[]>('/api/clientes'),

  crearCliente: (datos: DatosCliente) =>
    solicitar<Cliente>('/api/clientes', { method: 'POST', body: JSON.stringify(datos) }),

  eliminarCliente: (id: string) =>
    solicitar<ResultadoEliminacionCliente>(`/api/clientes/${encodeURIComponent(id)}`, {
      method: 'DELETE',
    }),

  actualizarCliente: (id: string, datos: DatosCliente) =>
    solicitar<Cliente>(`/api/clientes/${encodeURIComponent(id)}`, {
      method: 'PUT',
      body: JSON.stringify(datos),
    }),

  listarFacturas: (filtros: FiltrosFacturas = {}) =>
    solicitar<Factura[]>(`/api/facturas${consulta({ ...filtros })}`),

  registrarFactura: (datos: DatosFacturaNueva) =>
    solicitar<Factura>('/api/facturas', { method: 'POST', body: JSON.stringify(datos) }),

  eliminarFactura: (id: string) =>
    solicitar<ResultadoEliminacionFacturas>(`/api/facturas/${encodeURIComponent(id)}`, {
      method: 'DELETE',
    }),

  eliminarFacturas: (facturaIds: string[]) =>
    solicitar<ResultadoEliminacionFacturas>('/api/facturas/eliminar', {
      method: 'POST',
      body: JSON.stringify({ facturaIds }),
    }),

  procesarFacturas: (facturaIds: string[]) =>
    solicitar<ResultadoEjecucion>('/api/facturas/procesar', {
      method: 'POST',
      body: JSON.stringify({ facturaIds }),
    }),

  procesarFactura: (id: string) =>
    solicitar<ResultadoEjecucion>(`/api/facturas/${encodeURIComponent(id)}/procesar`, {
      method: 'POST',
    }),

  obtenerResumen: () => solicitar<ResumenCliente[]>('/api/resumen/clientes'),

  listarEnvios: (clienteId?: string) => solicitar<Envio[]>(`/api/envios${consulta({ clienteId })}`),

  borrarDatos: () => solicitar<ResumenBorrado>('/api/datos/borrar', { method: 'POST' }),

  cargarEjemplo: () => solicitar<ResumenCargaEjemplo>('/api/datos/ejemplo', { method: 'POST' }),
}
