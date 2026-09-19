import React from 'react'
import ReactDOM from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { ConfigProvider } from 'antd'
import ruRU from 'antd/locale/ru_RU'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import 'antd/dist/reset.css'
import 'maplibre-gl/dist/maplibre-gl.css'
import App from './App'
import { forestTheme } from './theme'

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 60_000
    }
  }
})

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <ConfigProvider locale={ruRU} theme={forestTheme}>
      <QueryClientProvider client={queryClient}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </QueryClientProvider>
    </ConfigProvider>
  </React.StrictMode>
)
