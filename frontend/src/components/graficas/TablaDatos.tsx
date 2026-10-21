import type { ReactNode } from 'react'

interface Props {
  titulo: string
  columnas: string[]
  filas: ReactNode[][]
}

/**
 * Vista de tabla de una gráfica: todo valor que muestra la gráfica (y su tooltip) se puede leer aquí sin depender
 * del color ni del puntero.
 */
export function TablaDatos({ titulo, columnas, filas }: Props) {
  return (
    <details className="group">
      <summary className="w-fit cursor-pointer text-xs font-medium text-acento">
        Ver datos en tabla
      </summary>
      <div className="mt-3 overflow-x-auto rounded-lg border border-borde">
        <table className="tabla">
          <caption className="sr-only">{titulo}</caption>
          <thead>
            <tr>
              {columnas.map((columna, indice) => (
                <th key={columna} scope="col" className={indice > 0 ? 'text-right' : undefined}>
                  {columna}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {filas.map((fila, indiceFila) => (
              <tr key={indiceFila}>
                {fila.map((celda, indice) => (
                  <td key={indice} className={indice > 0 ? 'text-right tabular-nums' : undefined}>
                    {celda}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </details>
  )
}
