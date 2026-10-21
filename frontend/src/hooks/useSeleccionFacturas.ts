import { useState } from 'react'
import type { Factura } from '../api/tipos'
import type { SeleccionFacturas } from '../components/TablaFacturas'

/**
 * Facturas marcadas con las casillas de la tabla. "Marcar todas" solo marca las visibles, así nunca queda
 * seleccionada una factura que el usuario no ve.
 */
export function useSeleccionFacturas(visibles: Factura[] | undefined) {
  const [ids, setIds] = useState<ReadonlySet<string>>(new Set())

  function limpiar() {
    setIds(new Set())
  }

  const seleccion: SeleccionFacturas = {
    ids,
    alCambiar: (factura, marcada) =>
      setIds((actual) => {
        const nueva = new Set(actual)
        if (marcada) {
          nueva.add(factura.id)
        } else {
          nueva.delete(factura.id)
        }
        return nueva
      }),
    alCambiarTodas: (marcar) => setIds(marcar ? new Set(visibles?.map((f) => f.id)) : new Set()),
  }

  return { ids: [...ids], cantidad: ids.size, seleccion, limpiar }
}
