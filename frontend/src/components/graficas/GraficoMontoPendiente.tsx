import { useId } from 'react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  LabelList,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import type { ResumenCliente } from '../../api/tipos'
import { formatearMoneda, formatearMonedaCompacta } from '../../utils/formato'
import { useMovimientoReducido } from '../../utils/movimiento'
import { usePaleta } from '../../utils/paleta'
import { TablaDatos } from './TablaDatos'
import { TooltipGrafico } from './TooltipGrafico'

const GROSOR_BARRA = 22
const ALTO_POR_CLIENTE = 44
const ALTO_EJE = 32

interface Props {
  resumen: ResumenCliente[]
  moneda: string
}

/**
 * Magnitud de una sola serie: monto pendiente por cliente en una moneda. Un solo color y sin leyenda (el título
 * dice qué se grafica). Con varias monedas se dibuja una gráfica por moneda: nunca se mezclan escalas en un eje.
 */
export function GraficoMontoPendiente({ resumen, moneda }: Props) {
  const paleta = usePaleta()
  const movimientoReducido = useMovimientoReducido()
  const idTitulo = useId()
  const datos = resumen.map((cliente) => ({
    nombre: cliente.nombre,
    monto: cliente.montoPendiente.find((m) => m.moneda === moneda)?.monto ?? 0,
  }))

  return (
    <figure className="m-0 grid min-w-0 gap-4" aria-labelledby={idTitulo}>
      <figcaption id={idTitulo} className="font-semibold">
        Monto pendiente por cliente ({moneda})
      </figcaption>
      <ResponsiveContainer width="100%" height={datos.length * ALTO_POR_CLIENTE + ALTO_EJE}>
        <BarChart data={datos} layout="vertical" margin={{ top: 4, right: 96, bottom: 0, left: 0 }}>
          <CartesianGrid horizontal={false} stroke={paleta.rejilla} />
          <XAxis
            type="number"
            tickFormatter={(valor: number) => formatearMonedaCompacta(valor, moneda)}
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
              <TooltipGrafico
                formatear={(valor) => formatearMoneda(valor, moneda)}
                nombreSerie={() => 'Pendiente'}
              />
            }
          />
          <Bar
            dataKey="monto"
            fill={paleta.montoPendiente}
            barSize={GROSOR_BARRA}
            radius={[0, 4, 4, 0]}
            isAnimationActive={!movimientoReducido}
            animationDuration={400}
          >
            <LabelList
              dataKey="monto"
              position="right"
              formatter={(valor: unknown) => formatearMonedaCompacta(Number(valor), moneda)}
              fill={paleta.textoSecundario}
              fontSize={12}
            />
          </Bar>
        </BarChart>
      </ResponsiveContainer>
      <TablaDatos
        titulo={`Monto pendiente por cliente (${moneda})`}
        columnas={['Cliente', 'Monto pendiente']}
        filas={datos.map((dato) => [dato.nombre, formatearMoneda(dato.monto, moneda)])}
      />
    </figure>
  )
}
