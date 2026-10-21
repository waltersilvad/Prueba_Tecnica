import type { ErroresCampo } from '../api/clienteHttp'
import type { DatosCliente, DatosFacturaNueva } from '../api/tipos'

// Validación rápida en el navegador. La API aplica la regla definitiva y sus errores también se muestran.
// El patrón coincide con el $jsonSchema de Mongo para no rechazar en el front lo que la API acepta.
const PATRON_EMAIL = /^[^\s@<>]+@[^\s@<>]+$/
const PATRON_MONEDA = /^[A-Z]{3}$/

export function validarCliente({ nombre, email }: DatosCliente): ErroresCampo {
  const errores: ErroresCampo = {}

  if (!nombre.trim()) {
    errores.nombre = ['El nombre es obligatorio.']
  }

  if (!email.trim()) {
    errores.email = ['El correo es obligatorio.']
  } else if (!PATRON_EMAIL.test(email.trim())) {
    errores.email = ['El correo no tiene un formato válido.']
  }

  return errores
}

export function validarFactura(datos: DatosFacturaNueva): ErroresCampo {
  const errores: ErroresCampo = {}

  if (!datos.clienteId) {
    errores.clienteId = ['Selecciona el cliente.']
  }
  if (!datos.numero.trim()) {
    errores.numero = ['El número de factura es obligatorio.']
  }
  if (!(datos.monto > 0)) {
    errores.monto = ['El monto debe ser mayor que cero.']
  }
  if (!PATRON_MONEDA.test(datos.moneda)) {
    errores.moneda = ['La moneda debe ser un código de 3 letras en mayúsculas (por ejemplo, COP).']
  }
  if (!datos.fechaEmision) {
    errores.fechaEmision = ['La fecha de emisión es obligatoria.']
  }
  if (!datos.fechaVencimiento) {
    errores.fechaVencimiento = ['La fecha de vencimiento es obligatoria.']
  } else if (datos.fechaEmision && datos.fechaVencimiento < datos.fechaEmision) {
    errores.fechaVencimiento = ['La fecha de vencimiento no puede ser anterior a la de emisión.']
  }

  return errores
}
