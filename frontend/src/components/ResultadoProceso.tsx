import { X } from 'lucide-react'
import type { ResultadoEjecucion } from '../api/tipos'

interface Props {
  resultado: ResultadoEjecucion
  onCerrar: () => void
}

function Metrica({ etiqueta, valor }: { etiqueta: string; valor: number }) {
  return (
    <div className="rounded-lg bg-hundida px-3 py-2">
      <dt className="text-xs text-texto-2">{etiqueta}</dt>
      <dd className="text-xl font-semibold">{valor}</dd>
    </div>
  )
}

export function ResultadoProceso({ resultado, onCerrar }: Props) {
  const total = (campo: 'correosEnviados' | 'facturasActualizadas') =>
    resultado.clientes.reduce((suma, cliente) => suma + cliente[campo], 0)

  return (
    <section
      aria-label="Resultado del proceso"
      className="space-y-4 rounded-xl border border-borde bg-acento-suave p-5 motion-safe:animate-aparecer"
    >
      <header className="flex flex-wrap items-start justify-between gap-4">
        <div className="space-y-1">
          <h2 className="text-base font-semibold">Resultado del proceso</h2>
          <p className="text-sm text-texto-2">
            {total('correosEnviados')} correos enviados · {total('facturasActualizadas')} facturas
            actualizadas · ejecución{' '}
            <code className="font-mono">{resultado.ejecucionId.slice(0, 8)}</code>
          </p>
        </div>
        <button type="button" className="boton-secundario boton-pequeno" onClick={onCerrar}>
          <X size={14} aria-hidden="true" />
          Cerrar
        </button>
      </header>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        {resultado.clientes.map((cliente) => (
          <article
            key={cliente.clienteId}
            aria-label={cliente.nombre}
            className="tarjeta space-y-3"
          >
            <h3 className="font-semibold">{cliente.nombre}</h3>
            <dl className="grid grid-cols-3 gap-2">
              <Metrica etiqueta="Enviados" valor={cliente.correosEnviados} />
              <Metrica etiqueta="Actualizadas" valor={cliente.facturasActualizadas} />
              <Metrica etiqueta="Omitidas" valor={cliente.omitidas.length} />
            </dl>

            {cliente.omitidas.length > 0 && (
              <ul className="list-disc space-y-1 pl-5 text-sm text-texto-2">
                {cliente.omitidas.map((omitida) => (
                  <li key={omitida.numero}>
                    <strong className="text-texto">{omitida.numero}</strong>: {omitida.motivo}
                  </li>
                ))}
              </ul>
            )}

            {cliente.errores.length > 0 && (
              <ul className="list-disc space-y-1 pl-5 text-sm text-critico">
                {cliente.errores.map((error) => (
                  <li key={error}>{error}</li>
                ))}
              </ul>
            )}

            {cliente.conflictos.length > 0 && (
              <p className="text-sm text-aviso">
                Conflictos (otra ejecución las cambió): {cliente.conflictos.join(', ')}
              </p>
            )}
          </article>
        ))}
      </div>
    </section>
  )
}
