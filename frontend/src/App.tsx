import { lazy, Suspense } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router'
import { BarraLateral } from './components/BarraLateral'
import { Cargando } from './components/EstadosVista'

// Carga diferida por vista: cada una se descarga al abrirla. Recharts (lo más pesado) solo viaja con el Panel.
const PanelPage = lazy(() => import('./pages/PanelPage').then((m) => ({ default: m.PanelPage })))
const ClientesPage = lazy(() =>
  import('./pages/ClientesPage').then((m) => ({ default: m.ClientesPage })),
)
const FacturasPage = lazy(() =>
  import('./pages/FacturasPage').then((m) => ({ default: m.FacturasPage })),
)
const EnviosPage = lazy(() => import('./pages/EnviosPage').then((m) => ({ default: m.EnviosPage })))

export function App() {
  const ubicacion = useLocation()

  return (
    <div className="min-h-screen lg:grid lg:grid-cols-[16rem_minmax(0,1fr)]">
      <BarraLateral />
      <main className="mx-auto w-full max-w-6xl space-y-6 px-4 py-6 sm:px-6 lg:px-8 lg:py-8">
        <Suspense fallback={<Cargando />}>
          {/* La clave por ruta reinicia la animación de entrada al cambiar de vista (no al refrescar datos). */}
          <div key={ubicacion.pathname} className="motion-safe:animate-aparecer">
            <Routes>
              <Route path="/" element={<Navigate to="/panel" replace />} />
              <Route path="/panel" element={<PanelPage />} />
              <Route path="/clientes" element={<ClientesPage />} />
              <Route path="/facturas" element={<FacturasPage />} />
              <Route path="/envios" element={<EnviosPage />} />
              {/* La antigua vista "Estado por cliente" ahora es parte del Panel. */}
              <Route path="/estado" element={<Navigate to="/panel" replace />} />
              <Route path="*" element={<Navigate to="/panel" replace />} />
            </Routes>
          </div>
        </Suspense>
      </main>
    </div>
  )
}
