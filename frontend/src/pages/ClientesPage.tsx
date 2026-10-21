import { Mail, Pencil, Trash2, UserPlus } from 'lucide-react'
import { useState } from 'react'
import { mensajeDeError } from '../api/clienteHttp'
import type { Cliente, DatosCliente } from '../api/tipos'
import { EncabezadoVista } from '../components/EncabezadoVista'
import { Cargando, MensajeError, MensajeVacio } from '../components/EstadosVista'
import { FormularioCliente } from '../components/FormularioCliente'
import {
  useActualizarCliente,
  useClientes,
  useCrearCliente,
  useEliminarCliente,
  useProcesoEnCurso,
  useResumenClientes,
} from '../hooks/useFacturacion'

/** Iniciales para el avatar: primeras letras de las dos primeras palabras del nombre. */
function iniciales(nombre: string): string {
  return nombre
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((palabra) => palabra[0]?.toUpperCase())
    .join('')
}

export function ClientesPage() {
  const clientes = useClientes()
  const actualizar = useActualizarCliente()
  const crear = useCrearCliente()
  const eliminar = useEliminarCliente()
  const resumen = useResumenClientes()
  const procesoEnCurso = useProcesoEnCurso()
  const [editandoId, setEditandoId] = useState<string>()
  const [creando, setCreando] = useState(false)
  const [aviso, setAviso] = useState<string>()

  async function guardar(id: string, datos: DatosCliente) {
    const actualizado = await actualizar.mutateAsync({ id, datos })
    setEditandoId(undefined)
    setAviso(`Se guardaron los datos de ${actualizado.nombre}.`)
  }

  function confirmarEliminacion(cliente: Cliente) {
    const facturas = resumen.data?.find((r) => r.clienteId === cliente.id)?.totalFacturas ?? 0
    const detalle =
      facturas === 0
        ? 'No tiene facturas.'
        : `También se eliminarán sus ${facturas} ${facturas === 1 ? 'factura' : 'facturas'} y su historial de envíos.`
    if (
      !window.confirm(`¿Eliminar a ${cliente.nombre}? ${detalle} Esta acción no se puede deshacer.`)
    ) {
      return
    }

    setAviso(undefined)
    eliminar.mutate(cliente.id, {
      onSuccess: (r) =>
        setAviso(
          `Se eliminó ${r.nombre} junto con ${r.facturasEliminadas} facturas y ${r.enviosEliminados} envíos.`,
        ),
    })
  }

  async function registrar(datos: DatosCliente) {
    const creado = await crear.mutateAsync(datos)
    setCreando(false)
    setAviso(`Se registró el cliente ${creado.nombre}.`)
  }

  return (
    <section aria-labelledby="titulo-clientes" className="space-y-6">
      <EncabezadoVista
        id="titulo-clientes"
        titulo="Clientes"
        descripcion="Los avisos se envían al correo de cada cliente. Cámbialo por uno tuyo para recibirlos."
        accion={
          !creando && (
            <button
              type="button"
              className="boton"
              onClick={() => {
                setAviso(undefined)
                setEditandoId(undefined)
                setCreando(true)
              }}
            >
              <UserPlus size={16} aria-hidden="true" />
              Nuevo cliente
            </button>
          )
        }
      />

      {aviso && (
        <p className="alerta-exito" role="status">
          {aviso}
        </p>
      )}

      {eliminar.isError && (
        <p className="alerta-error" role="alert">
          {mensajeDeError(eliminar.error)}
        </p>
      )}

      {creando && (
        <div className="tarjeta space-y-4 motion-safe:animate-aparecer">
          <h2 className="text-base font-semibold">Nuevo cliente</h2>
          <FormularioCliente
            etiqueta="Nuevo cliente"
            onGuardar={registrar}
            onCancelar={() => setCreando(false)}
          />
        </div>
      )}

      {clientes.isPending && <Cargando texto="Cargando clientes…" forma="tarjetas" />}
      {clientes.isError && (
        <MensajeError error={clientes.error} onReintentar={() => void clientes.refetch()} />
      )}
      {clientes.isSuccess && clientes.data.length === 0 && (
        <MensajeVacio>No hay clientes registrados.</MensajeVacio>
      )}

      {clientes.isSuccess && clientes.data.length > 0 && (
        <ul className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
          {clientes.data.map((cliente, indice) => (
            <li
              key={cliente.id}
              className="tarjeta motion-safe:animate-aparecer"
              style={{ animationDelay: `${Math.min(indice, 8) * 50}ms` }}
            >
              {editandoId === cliente.id ? (
                <FormularioCliente
                  etiqueta={`Editar ${cliente.nombre}`}
                  inicial={{ nombre: cliente.nombre, email: cliente.email }}
                  onGuardar={(datos) => guardar(cliente.id, datos)}
                  onCancelar={() => setEditandoId(undefined)}
                />
              ) : (
                <div className="flex items-start gap-4">
                  <span
                    className="grid size-10 shrink-0 place-items-center rounded-full bg-acento-suave text-sm font-semibold text-acento"
                    aria-hidden="true"
                  >
                    {iniciales(cliente.nombre)}
                  </span>
                  <div className="min-w-0 flex-1 space-y-1">
                    <h2 className="text-base font-semibold">{cliente.nombre}</h2>
                    <p className="flex items-center gap-1.5 text-sm break-all text-texto-2">
                      <Mail size={14} className="shrink-0" aria-hidden="true" />
                      {cliente.email}
                    </p>
                  </div>
                  <div className="flex shrink-0 flex-col gap-2">
                    <button
                      type="button"
                      className="boton-secundario boton-pequeno"
                      onClick={() => {
                        setAviso(undefined)
                        setEditandoId(cliente.id)
                      }}
                      aria-label={`Editar ${cliente.nombre}`}
                    >
                      <Pencil size={14} aria-hidden="true" />
                      Editar
                    </button>
                    <button
                      type="button"
                      className="boton-peligro boton-pequeno"
                      onClick={() => confirmarEliminacion(cliente)}
                      disabled={procesoEnCurso}
                      aria-label={`Eliminar ${cliente.nombre}`}
                    >
                      <Trash2 size={14} aria-hidden="true" />
                      Eliminar
                    </button>
                  </div>
                </div>
              )}
            </li>
          ))}
        </ul>
      )}
    </section>
  )
}
