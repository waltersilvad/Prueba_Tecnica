import { useId, useState, type FormEvent } from 'react'
import type { DatosCliente } from '../api/tipos'
import { useEnvioFormulario } from '../hooks/useEnvioFormulario'
import { validarCliente } from '../utils/validacion'
import { Campo } from './Campo'

interface Props {
  /** Nombre accesible del formulario, por ejemplo "Editar Comercial Andina" o "Nuevo cliente". */
  etiqueta: string
  inicial?: DatosCliente
  onGuardar: (datos: DatosCliente) => Promise<unknown>
  onCancelar: () => void
}

const VACIO: DatosCliente = { nombre: '', email: '' }

/** Alta o edición de nombre y correo. Valida en el navegador y muestra también los errores de la API. */
export function FormularioCliente({ etiqueta, inicial = VACIO, onGuardar, onCancelar }: Props) {
  const id = useId()
  const [datos, setDatos] = useState<DatosCliente>(inicial)
  const { errores, errorGeneral, guardando, enviar } = useEnvioFormulario({
    validar: validarCliente,
    guardar: (d: DatosCliente) => onGuardar({ nombre: d.nombre.trim(), email: d.email.trim() }),
  })

  function alEnviar(evento: FormEvent) {
    evento.preventDefault()
    void enviar(datos)
  }

  const cambiar = (campo: keyof DatosCliente) => (valor: string) =>
    setDatos((actual) => ({ ...actual, [campo]: valor }))

  return (
    <form className="space-y-4" onSubmit={alEnviar} noValidate aria-label={etiqueta}>
      <Campo id={`${id}-nombre`} etiqueta="Nombre" error={errores.nombre?.[0]}>
        {(control) => (
          <input
            {...control}
            type="text"
            value={datos.nombre}
            onChange={(e) => cambiar('nombre')(e.target.value)}
            disabled={guardando}
          />
        )}
      </Campo>
      <Campo id={`${id}-email`} etiqueta="Correo" error={errores.email?.[0]}>
        {(control) => (
          <input
            {...control}
            type="email"
            value={datos.email}
            onChange={(e) => cambiar('email')(e.target.value)}
            disabled={guardando}
          />
        )}
      </Campo>
      {errorGeneral && (
        <p className="alerta-error" role="alert">
          {errorGeneral}
        </p>
      )}
      <div className="flex flex-wrap gap-2">
        <button type="submit" className="boton" disabled={guardando}>
          {guardando ? 'Guardando…' : 'Guardar'}
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
