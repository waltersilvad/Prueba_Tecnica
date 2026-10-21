import {
  keepPreviousData,
  useIsMutating,
  useMutation,
  useQuery,
  useQueryClient,
} from '@tanstack/react-query'
import { api } from '../api/endpoints'
import type { DatosCliente, DatosFacturaNueva, FiltrosFacturas } from '../api/tipos'

export const claves = {
  clientes: ['clientes'] as const,
  facturas: (filtros: FiltrosFacturas) => ['facturas', filtros] as const,
  resumen: ['resumen'] as const,
  envios: (clienteId?: string) => ['envios', clienteId ?? 'todos'] as const,
}

/** Mutaciones que ocupan el bloqueo de ejecución de la API: mientras una corre, las demás se deshabilitan. */
const PROCESOS = {
  procesarFactura: 'procesar-factura',
  procesarSeleccion: 'procesar-seleccion',
  eliminarCliente: 'eliminar-cliente',
  eliminarFactura: 'eliminar-factura',
  eliminarFacturas: 'eliminar-facturas',
  borrarDatos: 'borrar-datos',
  cargarEjemplo: 'cargar-ejemplo',
}

export function useClientes() {
  return useQuery({ queryKey: claves.clientes, queryFn: () => api.listarClientes() })
}

export function useFacturas(filtros: FiltrosFacturas) {
  return useQuery({
    queryKey: claves.facturas(filtros),
    queryFn: () => api.listarFacturas(filtros),
    // Al cambiar el filtro se conservan los datos anteriores mientras llegan los nuevos (sin parpadeo).
    placeholderData: keepPreviousData,
  })
}

export function useResumenClientes() {
  return useQuery({ queryKey: claves.resumen, queryFn: () => api.obtenerResumen() })
}

export function useEnvios(clienteId?: string) {
  return useQuery({
    queryKey: claves.envios(clienteId),
    queryFn: () => api.listarEnvios(clienteId),
    placeholderData: keepPreviousData,
  })
}

/** Indica si hay un proceso que usa el bloqueo de la API (avisos, eliminación o datos de prueba) en curso. */
export function useProcesoEnCurso(): boolean {
  const procesos = Object.values(PROCESOS)
  return (
    useIsMutating({ predicate: (m) => procesos.includes(String(m.options.mutationKey?.[0])) }) > 0
  )
}

/** Tras cualquier cambio se invalidan todas las consultas: los datos de todas las vistas están relacionados. */
function useInvalidarTodo() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries()
}

export function useCrearCliente() {
  const invalidarTodo = useInvalidarTodo()
  return useMutation({
    mutationFn: (datos: DatosCliente) => api.crearCliente(datos),
    onSuccess: invalidarTodo,
  })
}

export function useEliminarCliente() {
  const invalidarTodo = useInvalidarTodo()
  return useMutation({
    mutationKey: [PROCESOS.eliminarCliente],
    mutationFn: (id: string) => api.eliminarCliente(id),
    onSuccess: invalidarTodo,
  })
}

export function useActualizarCliente() {
  const invalidarTodo = useInvalidarTodo()
  return useMutation({
    mutationFn: ({ id, datos }: { id: string; datos: DatosCliente }) =>
      api.actualizarCliente(id, datos),
    onSuccess: invalidarTodo,
  })
}

export function useRegistrarFactura() {
  const invalidarTodo = useInvalidarTodo()
  return useMutation({
    mutationFn: (datos: DatosFacturaNueva) => api.registrarFactura(datos),
    onSuccess: invalidarTodo,
  })
}

export function useProcesarFactura() {
  const invalidarTodo = useInvalidarTodo()
  return useMutation({
    mutationKey: [PROCESOS.procesarFactura],
    mutationFn: (id: string) => api.procesarFactura(id),
    onSettled: invalidarTodo,
  })
}

export function useEliminarFactura() {
  const invalidarTodo = useInvalidarTodo()
  return useMutation({
    mutationKey: [PROCESOS.eliminarFactura],
    mutationFn: (id: string) => api.eliminarFactura(id),
    onSuccess: invalidarTodo,
  })
}

export function useEliminarFacturas() {
  const invalidarTodo = useInvalidarTodo()
  return useMutation({
    mutationKey: [PROCESOS.eliminarFacturas],
    mutationFn: (facturaIds: string[]) => api.eliminarFacturas(facturaIds),
    onSuccess: invalidarTodo,
  })
}

export function useProcesarFacturas() {
  const invalidarTodo = useInvalidarTodo()
  return useMutation({
    mutationKey: [PROCESOS.procesarSeleccion],
    mutationFn: (facturaIds: string[]) => api.procesarFacturas(facturaIds),
    onSettled: invalidarTodo,
  })
}

export function useBorrarDatos() {
  const invalidarTodo = useInvalidarTodo()
  return useMutation({
    mutationKey: [PROCESOS.borrarDatos],
    mutationFn: () => api.borrarDatos(),
    onSuccess: invalidarTodo,
  })
}

export function useCargarEjemplo() {
  const invalidarTodo = useInvalidarTodo()
  return useMutation({
    mutationKey: [PROCESOS.cargarEjemplo],
    mutationFn: () => api.cargarEjemplo(),
    onSuccess: invalidarTodo,
  })
}
