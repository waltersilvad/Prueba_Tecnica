import { CircleCheck, CircleX } from 'lucide-react'
import { useState } from 'react'
import type { ResultadoEnvio } from '../api/tipos'
import { EncabezadoVista } from '../components/EncabezadoVista'
import { Cargando, MensajeError, MensajeVacio } from '../components/EstadosVista'
import { useClientes, useEnvios, useFacturas } from '../hooks/useFacturacion'
import { etiquetaAviso, formatearFechaHora } from '../utils/formato'

/** Resultado del envío: color de estado reservado, siempre acompañado de ícono y texto. */
function ResultadoBadge({ resultado }: { resultado: ResultadoEnvio }) {
  const enviado = resultado === 'Enviado'
  const Icono = enviado ? CircleCheck : CircleX
  return (
    <span
      className={`inline-flex items-center gap-1.5 font-semibold whitespace-nowrap ${enviado ? 'text-exito' : 'text-critico'}`}
    >
      <Icono size={16} aria-hidden="true" />
      {resultado}
    </span>
  )
}

/**
 * Historial de envíos. Cinco columnas que se ajustan al ancho (los textos largos pasan a la línea siguiente),
 * así la tabla nunca necesita desplazamiento horizontal; en pantallas angostas cada fila es una tarjeta.
 */
export function EnviosPage() {
  const [clienteId, setClienteId] = useState<string>()
  const envios = useEnvios(clienteId)
  const clientes = useClientes()
  const facturas = useFacturas({})

  const nombreCliente = new Map(clientes.data?.map((c) => [c.id, c.nombre]))
  const numeroFactura = new Map(facturas.data?.map((f) => [f.id, f.numero]))

  return (
    <section aria-labelledby="titulo-envios" className="space-y-6">
      <EncabezadoVista
        id="titulo-envios"
        titulo="Historial de envíos"
        descripcion="Cada intento de envío, del más reciente al más antiguo. Un envío fallido no cambia el estado de la factura."
      />

      <div className="grid gap-4 sm:grid-cols-[minmax(0,18rem)]">
        <div className="grid gap-1.5">
          <label htmlFor="filtro-envios-cliente" className="etiqueta-campo">
            Cliente
          </label>
          <select
            id="filtro-envios-cliente"
            className="control"
            value={clienteId ?? ''}
            onChange={(e) => setClienteId(e.target.value || undefined)}
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

      {envios.isPending && <Cargando texto="Cargando envíos…" />}
      {envios.isError && (
        <MensajeError error={envios.error} onReintentar={() => void envios.refetch()} />
      )}
      {envios.isSuccess && envios.data.length === 0 && (
        <MensajeVacio>
          Todavía no hay envíos. Procesa las facturas para generar avisos.
        </MensajeVacio>
      )}

      {envios.isSuccess && envios.data.length > 0 && (
        <div
          className={`tabla-contenedor tabla-responsiva-contenedor transition-opacity duration-200 ${envios.isPlaceholderData ? 'opacity-60' : ''}`}
        >
          <table className="tabla tabla-responsiva">
            <thead>
              <tr>
                <th scope="col">Fecha</th>
                <th scope="col">Factura</th>
                <th scope="col">Aviso</th>
                <th scope="col">Destinatario</th>
                <th scope="col">Resultado</th>
              </tr>
            </thead>
            <tbody>
              {envios.data.map((envio, indice) => (
                <tr key={`${envio.ejecucionId}-${envio.facturaId}-${indice}`}>
                  <td data-label="Fecha" className="whitespace-nowrap tabular-nums">
                    {formatearFechaHora(envio.fecha)}
                  </td>
                  <td data-label="Factura">
                    <span className="block font-mono">
                      {numeroFactura.get(envio.facturaId) ?? '—'}
                    </span>
                    <span className="block text-xs text-texto-2">
                      {nombreCliente.get(envio.clienteId) ?? '—'}
                    </span>
                  </td>
                  <td data-label="Aviso">{etiquetaAviso(envio.tipoAviso)}</td>
                  <td data-label="Destinatario" className="break-all">
                    {envio.destinatario}
                  </td>
                  <td data-label="Resultado">
                    <ResultadoBadge resultado={envio.resultado} />
                    {envio.error && (
                      <span className="mt-1 block max-w-xs text-xs break-words text-texto-2">
                        {envio.error}
                      </span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
