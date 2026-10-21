import { useId, useState, type FormEvent } from 'react'
import type { Cliente, DatosFacturaNueva } from '../api/tipos'
import { useEnvioFormulario } from '../hooks/useEnvioFormulario'
import { fechaCalendario } from '../utils/formato'
import { validarFactura } from '../utils/validacion'
import { Campo } from './Campo'

const CODIGO_NUMERO_DUPLICADO = 'Factura.NumeroDuplicado'
const MONEDA_POR_DEFECTO = 'COP'
const DIAS_PARA_VENCER = 30

interface Props {
  clientes: Cliente[]
  clienteInicial?: string
  onGuardar: (datos: DatosFacturaNueva) => Promise<unknown>
  onCancelar: () => void
}

/** Formulario de valores en texto: el monto se convierte a número solo al enviar. */
interface Valores extends Omit<DatosFacturaNueva, 'monto'> {
  monto: string
}

/**
 * Alta de una factura. No pide estado: la API la crea siempre en "Primer recordatorio".
 */
export function FormularioFactura({ clientes, clienteInicial = '', onGuardar, onCancelar }: Props) {
  const id = useId()
  const [valores, setValores] = useState<Valores>(() => ({
    clienteId: clienteInicial,
    numero: '',
    monto: '',
    moneda: MONEDA_POR_DEFECTO,
    fechaEmision: fechaCalendario(),
    fechaVencimiento: fechaCalendario(DIAS_PARA_VENCER),
  }))
  const { errores, errorGeneral, guardando, enviar } = useEnvioFormulario({
    validar: validarFactura,
    guardar: onGuardar,
    erroresPorCodigo: (error) =>
      error.codigo === CODIGO_NUMERO_DUPLICADO ? { numero: [error.message] } : undefined,
  })

  function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    void enviar({
      ...valores,
      numero: valores.numero.trim(),
      moneda: valores.moneda.trim().toUpperCase(),
      monto: Number(valores.monto),
    })
  }

  const cambiar = (campo: keyof Valores) => (valor: string) =>
    setValores((actual) => ({ ...actual, [campo]: valor }))

  const error = (campo: keyof DatosFacturaNueva) => errores[campo]?.[0]

  return (
    <form className="space-y-4" onSubmit={alEnviar} noValidate aria-label="Nueva factura">
      <p className="text-sm text-texto-2">
        La factura se crea en <strong>Primer recordatorio</strong>.
      </p>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <Campo id={`${id}-cliente`} etiqueta="Cliente" error={error('clienteId')}>
          {(control) => (
            <select
              {...control}
              value={valores.clienteId}
              onChange={(e) => cambiar('clienteId')(e.target.value)}
              disabled={guardando}
            >
              <option value="">Selecciona un cliente</option>
              {clientes.map((cliente) => (
                <option key={cliente.id} value={cliente.id}>
                  {cliente.nombre}
                </option>
              ))}
            </select>
          )}
        </Campo>
        <Campo id={`${id}-numero`} etiqueta="Número" error={error('numero')}>
          {(control) => (
            <input
              {...control}
              type="text"
              placeholder="FAC-9001"
              value={valores.numero}
              onChange={(e) => cambiar('numero')(e.target.value)}
              disabled={guardando}
            />
          )}
        </Campo>
        <Campo id={`${id}-monto`} etiqueta="Monto" error={error('monto')}>
          {(control) => (
            <input
              {...control}
              type="number"
              min="0"
              step="0.01"
              inputMode="decimal"
              value={valores.monto}
              onChange={(e) => cambiar('monto')(e.target.value)}
              disabled={guardando}
            />
          )}
        </Campo>
        <Campo id={`${id}-moneda`} etiqueta="Moneda" error={error('moneda')}>
          {(control) => (
            <input
              {...control}
              type="text"
              maxLength={3}
              value={valores.moneda}
              onChange={(e) => cambiar('moneda')(e.target.value.toUpperCase())}
              disabled={guardando}
            />
          )}
        </Campo>
        <Campo id={`${id}-emision`} etiqueta="Fecha de emisión" error={error('fechaEmision')}>
          {(control) => (
            <input
              {...control}
              type="date"
              value={valores.fechaEmision}
              onChange={(e) => cambiar('fechaEmision')(e.target.value)}
              disabled={guardando}
            />
          )}
        </Campo>
        <Campo
          id={`${id}-vencimiento`}
          etiqueta="Fecha de vencimiento"
          error={error('fechaVencimiento')}
        >
          {(control) => (
            <input
              {...control}
              type="date"
              value={valores.fechaVencimiento}
              onChange={(e) => cambiar('fechaVencimiento')(e.target.value)}
              disabled={guardando}
            />
          )}
        </Campo>
      </div>
      {errorGeneral && (
        <p className="alerta-error" role="alert">
          {errorGeneral}
        </p>
      )}
      <div className="flex flex-wrap gap-2">
        <button type="submit" className="boton" disabled={guardando}>
          {guardando ? 'Guardando…' : 'Crear factura'}
        </button>
        <button
          type="button"
          className="boton-secundario"
          onClick={onCancelar}
          disabled={guardando}
        >
          Cancelar
        </button>
      </div>
    </form>
  )
}
