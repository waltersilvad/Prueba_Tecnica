import { CircleCheck, CircleX, FileText, Hourglass, Send, Wallet } from 'lucide-react'
import { useState, type ReactNode } from 'react'
import {
  ESTADOS_FACTURA,
  type Envio,
  type EstadoFactura,
  type MontoPorMoneda,
  type ResumenCliente,
} from '../api/tipos'
import { EncabezadoVista } from '../components/EncabezadoVista'
import { Cargando, MensajeError, MensajeVacio } from '../components/EstadosVista'
import { GraficoEstadoPorCliente } from '../components/graficas/GraficoEstadoPorCliente'
import { GraficoEstados } from '../components/graficas/GraficoEstados'
import { GraficoMontoPendiente } from '../components/graficas/GraficoMontoPendiente'
import { TablaFacturas } from '../components/TablaFacturas'
import { TarjetaIndicador } from '../components/TarjetaIndicador'
import { useEnvios, useFacturas, useResumenClientes } from '../hooks/useFacturacion'
import {
  etiquetaAviso,
  formatearFechaHora,
  formatearMoneda,
  formatearMonedaCompacta,
} from '../utils/formato'

const ENVIOS_RECIENTES = 5

/** Suma los montos pendientes por moneda, de mayor a menor. */
function sumarPorMoneda(montos: MontoPorMoneda[]): MontoPorMoneda[] {
  const totales = new Map<string, number>()
  for (const { moneda, monto } of montos) {
    totales.set(moneda, (totales.get(moneda) ?? 0) + monto)
  }
  return [...totales]
    .map(([moneda, monto]) => ({ moneda, monto }))
    .sort((a, b) => b.monto - a.monto)
}

function contarPorEstado(clientes: ResumenCliente[]): Record<EstadoFactura, number> {
  const sumar = (campo: 'primerRecordatorio' | 'segundoRecordatorio' | 'desactivado') =>
    clientes.reduce((suma, cliente) => suma + cliente[campo], 0)
  return {
    primerrecordatorio: sumar('primerRecordatorio'),
    segundorecordatorio: sumar('segundoRecordatorio'),
    desactivado: sumar('desactivado'),
  }
}

/** Tarjeta con entrada escalonada. */
function TarjetaAnimada({
  orden,
  className = '',
  children,
}: {
  orden: number
  className?: string
  children: ReactNode
}) {
  return (
    <div
      className={`tarjeta motion-safe:animate-aparecer ${className}`}
      style={{ animationDelay: `${orden * 80}ms` }}
    >
      {children}
    </div>
  )
}

export function PanelPage() {
  const [clienteId, setClienteId] = useState<string>()
  const resumen = useResumenClientes()
  const envios = useEnvios(clienteId)

  // Si el cliente elegido ya no existe (por ejemplo, se eliminó), se vuelve a "Todos".
  const cliente = resumen.data?.find((c) => c.clienteId === clienteId)

  return (
    <section aria-labelledby="titulo-panel" className="space-y-6">
      <EncabezadoVista
        id="titulo-panel"
        titulo="Panel"
        descripcion={
          cliente
            ? `Cartera y avisos de ${cliente.nombre}.`
            : 'Estado de la cartera y de los avisos enviados.'
        }
        accion={
          resumen.isSuccess &&
          resumen.data.length > 0 && (
            <div className="grid w-full gap-1.5 sm:w-72">
              <label htmlFor="panel-cliente" className="etiqueta-campo">
                Ver
              </label>
              <select
                id="panel-cliente"
                className="control"
                value={cliente?.clienteId ?? ''}
                onChange={(e) => setClienteId(e.target.value || undefined)}
              >
                <option value="">Todos los clientes</option>
                {resumen.data.map((c) => (
                  <option key={c.clienteId} value={c.clienteId}>
                    {c.nombre}
                  </option>
                ))}
              </select>
            </div>
          )
        }
      />

      {resumen.isPending && <Cargando texto="Cargando panel…" forma="panel" />}
      {resumen.isError && (
        <MensajeError error={resumen.error} onReintentar={() => void resumen.refetch()} />
      )}
      {resumen.isSuccess && resumen.data.length === 0 && (
        <MensajeVacio>
          No hay clientes registrados. Crea uno en Clientes o carga los datos de ejemplo desde la
          barra lateral.
        </MensajeVacio>
      )}
      {resumen.isSuccess && resumen.data.length > 0 && (
        // La clave reinicia la animación de entrada al cambiar de cliente.
        <div key={cliente?.clienteId ?? 'todos'}>
          <Indicadores clientes={cliente ? [cliente] : resumen.data} envios={envios.data} />
          {cliente ? (
            <DetalleCliente cliente={cliente} envios={envios.data} />
          ) : (
            <GraficosGenerales clientes={resumen.data} />
          )}
        </div>
      )}
    </section>
  )
}

function Indicadores({ clientes, envios }: { clientes: ResumenCliente[]; envios?: Envio[] }) {
  const conteos = contarPorEstado(clientes)
  const total = ESTADOS_FACTURA.reduce((suma, estado) => suma + conteos[estado], 0)
  const montos = sumarPorMoneda(clientes.flatMap((c) => c.montoPendiente))
  const principal = montos[0]
  const enviados = envios?.filter((e) => e.resultado === 'Enviado').length
  const fallidos = envios?.filter((e) => e.resultado === 'Fallido').length ?? 0
  const unCliente = clientes.length === 1 ? clientes[0] : undefined

  return (
    <div className="mb-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
      <TarjetaIndicador
        orden={0}
        etiqueta="Facturas"
        valor={total}
        icono={FileText}
        detalle={unCliente ? unCliente.email : `${clientes.length} clientes`}
      />
      <TarjetaIndicador
        orden={1}
        etiqueta="Pendientes de aviso"
        valor={conteos.primerrecordatorio + conteos.segundorecordatorio}
        icono={Hourglass}
        detalle={`${conteos.desactivado} desactivadas`}
      />
      <TarjetaIndicador
        orden={2}
        etiqueta="Monto pendiente"
        valor={principal ? formatearMonedaCompacta(principal.monto, principal.moneda) : '—'}
        icono={Wallet}
        detalle={montos.map((m) => formatearMoneda(m.monto, m.moneda)).join(' · ') || 'Sin saldo'}
      />
      <TarjetaIndicador
        orden={3}
        etiqueta="Avisos enviados"
        valor={enviados ?? '—'}
        icono={Send}
        detalle={
          fallidos > 0 ? (
            <span className="inline-flex items-center gap-1 font-semibold text-critico">
              <CircleX size={14} aria-hidden="true" /> {fallidos} fallidos
            </span>
          ) : (
            'Desde el último borrado de datos'
          )
        }
      />
    </div>
  )
}

function GraficosGenerales({ clientes }: { clientes: ResumenCliente[] }) {
  const montos = sumarPorMoneda(clientes.flatMap((c) => c.montoPendiente))
  return (
    <div className="grid gap-4 lg:grid-cols-5">
      <TarjetaAnimada orden={4} className="lg:col-span-2">
        <GraficoEstados conteos={contarPorEstado(clientes)} />
      </TarjetaAnimada>
      <TarjetaAnimada orden={5} className="lg:col-span-3">
        <GraficoEstadoPorCliente resumen={clientes} />
      </TarjetaAnimada>
      {montos.map(({ moneda }, indice) => (
        <TarjetaAnimada key={moneda} orden={6 + indice} className="lg:col-span-5">
          <GraficoMontoPendiente resumen={clientes} moneda={moneda} />
        </TarjetaAnimada>
      ))}
    </div>
  )
}

/**
 * Vista de un solo cliente. Las gráficas "por cliente" no se muestran: con un cliente serían barras de una sola
 * barra, que no comparan nada (los indicadores ya dicen esos números). En su lugar, sus facturas y sus envíos.
 */
function DetalleCliente({ cliente, envios }: { cliente: ResumenCliente; envios?: Envio[] }) {
  const facturas = useFacturas({ clienteId: cliente.clienteId })
  const numeroFactura = new Map(facturas.data?.map((f) => [f.id, f.numero]))
  const recientes = envios?.slice(0, ENVIOS_RECIENTES) ?? []

  return (
    <div className="grid gap-4 lg:grid-cols-5">
      <TarjetaAnimada orden={4} className="lg:col-span-2">
        <GraficoEstados conteos={contarPorEstado([cliente])} />
      </TarjetaAnimada>

      <TarjetaAnimada orden={5} className="space-y-4 lg:col-span-3">
        <h2 className="font-semibold">Últimos envíos</h2>
        {recientes.length === 0 ? (
          <p className="text-sm text-texto-2">Todavía no se le han enviado avisos.</p>
        ) : (
          <ul className="divide-y divide-rejilla">
            {recientes.map((envio, indice) => (
              <li
                key={`${envio.ejecucionId}-${envio.facturaId}-${indice}`}
                className="flex flex-wrap items-start justify-between gap-x-4 gap-y-1 py-3 first:pt-0 last:pb-0"
              >
                <div className="min-w-0 space-y-0.5">
                  <p className="text-sm font-medium">
                    {etiquetaAviso(envio.tipoAviso)} ·{' '}
                    <span className="font-mono">{numeroFactura.get(envio.facturaId) ?? '—'}</span>
                  </p>
                  <p className="text-xs text-texto-2 tabular-nums">
                    {formatearFechaHora(envio.fecha)}
                  </p>
                </div>
                <span
                  className={`inline-flex items-center gap-1.5 text-sm font-semibold ${envio.resultado === 'Enviado' ? 'text-exito' : 'text-critico'}`}
                >
                  {envio.resultado === 'Enviado' ? (
                    <CircleCheck size={16} aria-hidden="true" />
                  ) : (
                    <CircleX size={16} aria-hidden="true" />
                  )}
                  {envio.resultado}
                </span>
              </li>
            ))}
          </ul>
        )}
      </TarjetaAnimada>

      <div className="space-y-4 lg:col-span-5">
        <h2 className="font-semibold">Facturas de {cliente.nombre}</h2>
        {facturas.isPending && <Cargando texto="Cargando facturas…" />}
        {facturas.isSuccess && facturas.data.length === 0 && (
          <MensajeVacio>Este cliente no tiene facturas.</MensajeVacio>
        )}
        {facturas.isSuccess && facturas.data.length > 0 && (
          <TablaFacturas facturas={facturas.data} />
        )}
      </div>
    </div>
  )
}
