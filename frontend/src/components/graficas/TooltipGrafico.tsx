interface Fila {
  name?: string | number
  value?: number | string | readonly (number | string)[]
  color?: string
  fill?: string
}

interface Props {
  active?: boolean
  payload?: readonly Fila[]
  label?: string | number
  /** Convierte el valor numérico en texto (por ejemplo, moneda). */
  formatear?: (valor: number) => string
  /** Nombre visible de cada serie a partir de su clave. */
  nombreSerie?: (clave: string) => string
}

/**
 * Tooltip común de las gráficas: el valor va en negrita y el nombre de la serie en texto secundario, con una
 * marca del color de la serie al lado (el texto nunca toma el color de los datos).
 */
export function TooltipGrafico({ active, payload, label, formatear, nombreSerie }: Props) {
  if (!active || !payload?.length) {
    return null
  }

  return (
    <div className="rounded-lg border border-borde bg-superficie px-3 py-2 text-xs shadow-lg">
      {label !== undefined && <p className="mb-1 text-texto-2">{label}</p>}
      <ul className="space-y-0.5">
        {payload.map((fila) => {
          const valor = Number(fila.value ?? 0)
          const nombre = String(fila.name ?? '')
          return (
            <li key={nombre} className="flex items-center gap-2">
              <span
                className="h-0.5 w-3 rounded-full"
                style={{ background: fila.color ?? fila.fill }}
                aria-hidden="true"
              />
              <strong className="text-texto tabular-nums">
                {formatear ? formatear(valor) : valor}
              </strong>
              <span className="text-texto-2">{nombreSerie ? nombreSerie(nombre) : nombre}</span>
            </li>
          )
        })}
      </ul>
    </div>
  )
}
