import { useId } from 'react'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { ESTADOS_FACTURA, type EstadoFactura, type ResumenCliente } from '../../api/tipos'
import { etiquetaEstado } from '../../utils/formato'
import { useMovimientoReducido } from '../../utils/movimiento'
import { usePaleta } from '../../utils/paleta'
import { TablaDatos } from './TablaDatos'
import { TooltipGrafico } from './TooltipGrafico'

const GROSOR_BARRA = 22
const ALTO_POR_CLIENTE = 44
const ALTO_EJE = 32

const CAMPO: Record<EstadoFactura, keyof ResumenCliente> = {
  primerrecordatorio: 'primerRecordatorio',
  segundorecordatorio: 'segundoRecordatorio',
  desactivado: 'desactivado',
}

/** Barras horizontales apiladas: cuántas facturas tiene cada cliente en cada estado. */
export function GraficoEstadoPorCliente({ resumen }: { resumen: ResumenCliente[] }) {
  const paleta = usePaleta()
  const movimientoReducido = useMovimientoReducido()
  const idTitulo = useId()
  const datos = resumen.map((cliente) => ({
    nombre: cliente.nombre,
    ...Object.fromEntries(ESTADOS_FACTURA.map((estado) => [estado, cliente[CAMPO[estado]]])),
  }))
  const ultimo = ESTADOS_FACTURA[ESTADOS_FACTURA.length - 1]

  return (
    <figure className="m-0 grid min-w-0 gap-4" aria-labelledby={idTitulo}>
      <figcaption id={idTitulo} className="font-semibold">
        Estado de las facturas por cliente
      </figcaption>
      <ul className="flex flex-wrap gap-x-4 gap-y-2 text-xs">
        {ESTADOS_FACTURA.map((estado) => (
          <li key={estado} className="flex items-center gap-2">
            <span
              className="size-3 shrink-0 rounded-sm"
              style={{ background: paleta.estados[estado] }}
              aria-hidden="true"
            />
            <span className="text-texto-2">{etiquetaEstado(estado)}</span>
          </li>
        ))}
      </ul>
      <ResponsiveContainer width="100%" height={datos.length * ALTO_POR_CLIENTE + ALTO_EJE}>
        <BarChart data={datos} layout="vertical" margin={{ top: 4, right: 16, bottom: 0, left: 0 }}>
          <CartesianGrid horizontal={false} stroke={paleta.rejilla} />
          <XAxis
            type="number"
            allowDecimals={false}
            tick={{ fill: paleta.textoTenue, fontSize: 12 }}
            axisLine={{ stroke: paleta.rejilla }}
            tickLine={false}
          />
          <YAxis
            type="category"
            dataKey="nombre"
            width={170}
            tick={{ fill: paleta.textoSecundario, fontSize: 12 }}
            axisLine={false}
            tickLine={false}
          />
          <Tooltip
            cursor={{ fill: paleta.rejilla, opacity: 0.4 }}
            content={
              <TooltipGrafico nombreSerie={(clave) => etiquetaEstado(clave as EstadoFactura)} />
            }
          />
          {ESTADOS_FACTURA.map((estado) => (
            <Bar
              key={estado}
              dataKey={estado}
              stackId="estados"
              fill={paleta.estados[estado]}
              stroke={paleta.superficie}
              strokeWidth={2}
              barSize={GROSOR_BARRA}
              radius={estado === ultimo ? [0, 4, 4, 0] : 0}
              isAnimationActive={!movimientoReducido}
              animationDuration={400}
            />
          ))}
        </BarChart>
      </ResponsiveContainer>
      <TablaDatos
        titulo="Estado de las facturas por cliente"
        columnas={['Cliente', ...ESTADOS_FACTURA.map(etiquetaEstado), 'Total']}
        filas={resumen.map((cliente) => [
          cliente.nombre,
          ...ESTADOS_FACTURA.map((estado) => cliente[CAMPO[estado]] as number),
          cliente.totalFacturas,
        ])}
      />
    </figure>
  )
}
