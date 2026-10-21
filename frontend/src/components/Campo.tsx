import type { ReactNode } from 'react'

export interface PropsControl {
  id: string
  className: string
  'aria-invalid': boolean
  'aria-describedby'?: string
}

interface Props {
  id: string
  etiqueta: string
  error?: string
  /** Recibe los atributos (estilo y accesibilidad) que debe llevar el control (input o select). */
  children: (control: PropsControl) => ReactNode
}

/** Etiqueta + control + mensaje de error, con el error asociado al control para lectores de pantalla. */
export function Campo({ id, etiqueta, error, children }: Props) {
  const idError = `${id}-error`
  return (
    <div className="grid gap-1.5">
      <label htmlFor={id} className="etiqueta-campo">
        {etiqueta}
      </label>
      {children({
        id,
        className: 'control',
        'aria-invalid': !!error,
        'aria-describedby': error ? idError : undefined,
      })}
      {error && (
        <span id={idError} className="text-xs text-critico">
          {error}
        </span>
      )}
    </div>
  )
}
