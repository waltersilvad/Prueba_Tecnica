import { useId } from 'react'
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import { ESTADOS_FACTURA, type EstadoFactura } from '../../api/tipos'
import { etiquetaEstado } from '../../utils/formato'
import { useMovimientoReducido } from '../../utils/movimiento'
import { usePaleta } from '../../utils/paleta'
import { TablaDatos } from './TablaDatos'
import { TooltipGrafico } from './TooltipGrafico'

interface Props {
  conteos: Record<EstadoFactura, number>
}

/** Parte del todo con 3 segmentos: una dona con el total al centro y una leyenda que incluye cada valor. */
export function GraficoEstados({ conteos }: Props) {
  const paleta = usePaleta()
  const movimientoReducido = useMovimientoReducido()
  const idTitulo = useId()
  const total = ESTADOS_FACTURA.reduce((suma, estado) => suma + conteos[estado], 0)
  const datos = ESTADOS_FACTURA.map((estado) => ({
    estado,
    nombre: etiquetaEstado(estado),
    cantidad: conteos[estado],
  }))
  const porcentaje = (cantidad: number) => (total === 0 ? 0 : Math.round((cantidad / total) * 100))

  return (
    <figure className="m-0 grid min-w-0 gap-4" aria-labelledby={idTitulo}>
      <figcaption id={idTitulo} className="font-semibold">
        Facturas por estado
      </figcaption>
      <div className="flex flex-wrap items-center gap-6">
        <div className="relative min-w-44 flex-1 basis-44 sm:max-w-56">
          <ResponsiveContainer width="100%" height={200}>
            <PieChart>
              <Pie
                data={datos}
                dataKey="cantidad"
                nameKey="nombre"
                innerRadius={62}
                outerRadius={90}
                startAngle={90}
                endAngle={-270}
                stroke={paleta.superficie}
                strokeWidth={2}
                isAnimationActive={!movimientoReducido}
                animationDuration={400}
              >
                {datos.map((dato) => (
                  <Cell key={dato.estado} fill={paleta.estados[dato.estado]} />
                ))}
              </Pie>
              <Tooltip content={<TooltipGrafico />} />
            </PieChart>
          </ResponsiveContainer>
          <div
            className="pointer-events-none absolute inset-0 grid place-content-center text-center"
            aria-hidden="true"
          >
            <strong className="text-3xl leading-none font-semibold">{total}</strong>
            <span className="text-xs text-tenue">facturas</span>
          </div>
        </div>
        <ul className="flex min-w-40 flex-1 flex-col gap-3 text-sm">
          {datos.map((dato) => (
            <li
              key={dato.estado}
              className="grid grid-cols-[0.75rem_1fr_auto_3.5rem] items-center gap-2"
            >
              <span
                className="size-3 shrink-0 rounded-sm"
                style={{ background: paleta.estados[dato.estado] }}
                aria-hidden="true"
              />
              <span className="text-texto-2">{dato.nombre}</span>
              <strong className="text-texto tabular-nums">{dato.cantidad}</strong>
              <span className="text-right text-tenue tabular-nums">
                {porcentaje(dato.cantidad)} %
              </span>
            </li>
          ))}
        </ul>
      </div>
      <TablaDatos
        titulo="Facturas por estado"
        columnas={['Estado', 'Facturas', 'Porcentaje']}
        filas={datos.map((dato) => [dato.nombre, dato.cantidad, `${porcentaje(dato.cantidad)} %`])}
      />
    </figure>
  )
}
