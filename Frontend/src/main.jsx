import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { App as AntApp, ConfigProvider } from 'antd'
import ruRU from 'antd/locale/ru_RU'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { setWorkerUrl } from 'maplibre-gl'
import workerUrl from 'maplibre-gl/dist/maplibre-gl-worker.mjs?worker&url'
import 'antd/dist/reset.css'
import 'maplibre-gl/dist/maplibre-gl.css'
import App from './App'
import ErrorBoundary from './components/ErrorBoundary'
import { forestTheme } from './theme'

// MapLibre v6 ESM: воркер нужно указать вручную для бандлера (Vite).
setWorkerUrl(workerUrl)

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 30_000
    }
  }
})

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <ConfigProvider locale={ruRU} theme={forestTheme}>
      <AntApp>
        <ErrorBoundary>
          <QueryClientProvider client={queryClient}>
            <BrowserRouter>
              <App />
            </BrowserRouter>
          </QueryClientProvider>
        </ErrorBoundary>
      </AntApp>
    </ConfigProvider>
  </React.StrictMode>
)
