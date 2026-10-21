import { FilePlus } from 'lucide-react'
import { useState } from 'react'
import { mensajeDeError } from '../api/clienteHttp'
import {
  ESTADOS_FACTURA,
  type DatosFacturaNueva,
  type EstadoFactura,
  type Factura,
  type FiltrosFacturas,
  type ResultadoEjecucion,
  type ResultadoEliminacionFacturas,
} from '../api/tipos'
import { BarraSeleccion } from '../components/BarraSeleccion'
import { EncabezadoVista } from '../components/EncabezadoVista'
import { Cargando, MensajeError, MensajeVacio } from '../components/EstadosVista'
import { FormularioFactura } from '../components/FormularioFactura'
import { ResultadoProceso } from '../components/ResultadoProceso'
import { TablaFacturas } from '../components/TablaFacturas'
import {
  useClientes,
  useEliminarFactura,
  useEliminarFacturas,
  useFacturas,
  useProcesarFactura,
  useProcesarFacturas,
  useProcesoEnCurso,
  useRegistrarFactura,
} from '../hooks/useFacturacion'
import { useSeleccionFacturas } from '../hooks/useSeleccionFacturas'
import { etiquetaEstado } from '../utils/formato'

export function FacturasPage() {
  const [filtros, setFiltros] = useState<FiltrosFacturas>({})
  const [creando, setCreando] = useState(false)
  const [aviso, setAviso] = useState<string>()
  const [resultado, setResultado] = useState<ResultadoEjecucion>()
  const facturas = useFacturas(filtros)
  const seleccionadas = useSeleccionFacturas(facturas.data)
  const clientes = useClientes()
  const registrar = useRegistrarFactura()
  const procesarUna = useProcesarFactura()
  const procesarVarias = useProcesarFacturas()
  const eliminarUna = useEliminarFactura()
  const eliminarVarias = useEliminarFacturas()
  const procesoEnCurso = useProcesoEnCurso()
  const errorProceso =
    procesarUna.error ?? procesarVarias.error ?? eliminarUna.error ?? eliminarVarias.error

  /** Cambiar un filtro limpia la selección: así nunca quedan seleccionadas facturas que no se ven. */
  function filtrar(cambio: Partial<FiltrosFacturas>) {
    setFiltros((actual) => ({ ...actual, ...cambio }))
    seleccionadas.limpiar()
  }

  function prepararProceso() {
    setAviso(undefined)
    setResultado(undefined)
    procesarUna.reset()
    procesarVarias.reset()
    eliminarUna.reset()
    eliminarVarias.reset()
  }

  function informarEliminacion(r: ResultadoEliminacionFacturas) {
    const facturasTexto =
      r.facturasEliminadas === 1 ? `la factura ${r.numeros[0]}` : `${r.facturasEliminadas} facturas`
    setAviso(`Se eliminó ${facturasTexto} y ${r.enviosEliminados} envíos de su historial.`)
  }

  function eliminar(factura: Factura) {
    if (
      !window.confirm(
        `¿Eliminar la factura ${factura.numero}? También se borra su historial de envíos.`,
      )
    ) {
      return
    }
    prepararProceso()
    eliminarUna.mutate(factura.id, { onSuccess: informarEliminacion })
  }

  function eliminarSeleccionadas() {
    const { cantidad } = seleccionadas
    const pregunta =
      cantidad === 1
        ? '¿Eliminar la factura seleccionada? También se borra su historial de envíos.'
        : `¿Eliminar las ${cantidad} facturas seleccionadas? También se borra su historial de envíos.`
    if (!window.confirm(pregunta)) {
      return
    }
    prepararProceso()
    eliminarVarias.mutate(seleccionadas.ids, {
      onSuccess: (r) => {
        informarEliminacion(r)
        seleccionadas.limpiar()
      },
    })
  }

  function enviarAviso(factura: Factura) {
    prepararProceso()
    procesarUna.mutate(factura.id, { onSuccess: setResultado })
  }

  function enviarSeleccionadas() {
    prepararProceso()
    procesarVarias.mutate(seleccionadas.ids, {
      onSuccess: (datos) => {
        setResultado(datos)
        seleccionadas.limpiar()
      },
    })
  }

  async function crearFactura(datos: DatosFacturaNueva) {
    const creada = await registrar.mutateAsync(datos)
    setCreando(false)
    setAviso(`Se creó la factura ${creada.numero} en Primer recordatorio.`)
  }

  return (
    <section aria-labelledby="titulo-facturas" className="space-y-6">
      <EncabezadoVista
        id="titulo-facturas"
        titulo="Facturas"
        descripcion="Envía el aviso o elimina una factura con sus botones, o marca varias para hacerlo juntas."
        accion={
          !creando && (
            <button
              type="button"
              className="boton"
              onClick={() => {
                setAviso(undefined)
                setCreando(true)
              }}
            >
              <FilePlus size={16} aria-hidden="true" />
              Nueva factura
            </button>
          )
        }
      />

      {aviso && (
        <p className="alerta-exito" role="status">
          {aviso}
        </p>
      )}

      {creando && (
        <div className="tarjeta space-y-4 motion-safe:animate-aparecer">
          <h2 className="text-base font-semibold">Nueva factura</h2>
          <FormularioFactura
            clientes={clientes.data ?? []}
            clienteInicial={filtros.clienteId}
            onGuardar={crearFactura}
            onCancelar={() => setCreando(false)}
          />
        </div>
      )}

      {errorProceso && (
        <p className="alerta-error" role="alert">
          {mensajeDeError(errorProceso)}
        </p>
      )}
      {resultado && (
        <ResultadoProceso resultado={resultado} onCerrar={() => setResultado(undefined)} />
      )}

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-[repeat(2,minmax(0,16rem))]">
        <div className="grid gap-1.5">
          <label htmlFor="filtro-estado" className="etiqueta-campo">
            Estado
          </label>
          <select
            id="filtro-estado"
            className="control"
            value={filtros.estado ?? ''}
            onChange={(e) => filtrar({ estado: (e.target.value || undefined) as EstadoFactura })}
          >
            <option value="">Todos</option>
            {ESTADOS_FACTURA.map((estado) => (
              <option key={estado} value={estado}>
                {etiquetaEstado(estado)}
              </option>
            ))}
          </select>
        </div>
        <div className="grid gap-1.5">
          <label htmlFor="filtro-cliente" className="etiqueta-campo">
            Cliente
          </label>
          <select
            id="filtro-cliente"
            className="control"
            value={filtros.clienteId ?? ''}
            onChange={(e) => filtrar({ clienteId: e.target.value || undefined })}
          >
            <option value="">Todos</option>
            {clientes.data?.map((cliente) => (
              <option key={cliente.id} value={cliente.id}>
                {cliente.nombre}
              </option>
            ))}
          </select>
        </div>
      </div>

      {facturas.isSuccess && facturas.data.length > 0 && (
        <BarraSeleccion
          cantidad={seleccionadas.cantidad}
          bloqueada={procesoEnCurso}
          enviando={procesarVarias.isPending}
          eliminando={eliminarVarias.isPending}
          onQuitarSeleccion={seleccionadas.limpiar}
          onEnviarAvisos={enviarSeleccionadas}
          onEliminar={eliminarSeleccionadas}
        />
      )}

      {facturas.isPending && <Cargando texto="Cargando facturas…" />}
      {facturas.isError && (
        <MensajeError error={facturas.error} onReintentar={() => void facturas.refetch()} />
      )}
      {facturas.isSuccess && facturas.data.length === 0 && (
        <MensajeVacio>No hay facturas que coincidan con los filtros.</MensajeVacio>
      )}
      {facturas.isSuccess && facturas.data.length > 0 && (
        // Al cambiar un filtro se conserva la tabla anterior atenuada hasta que llegan los datos nuevos.
        <div
          className={`transition-opacity duration-200 ${facturas.isPlaceholderData ? 'opacity-60' : ''}`}
        >
          <TablaFacturas
            facturas={facturas.data}
            onEnviarAviso={enviarAviso}
            onEliminar={eliminar}
            seleccion={seleccionadas.seleccion}
            facturaEnProceso={procesarUna.isPending ? procesarUna.variables : undefined}
            accionesBloqueadas={procesoEnCurso}
          />
        </div>
      )}
    </section>
  )
}
