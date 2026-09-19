import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Публичный API — Go-шлюз (Backend/Go, по умолчанию :8080).
// C# — внутренний сервис (:5126), браузер его напрямую не вызывает.
const PROXY_TARGET = process.env.VITE_PROXY_TARGET ?? 'http://localhost:8080'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: PROXY_TARGET,
        changeOrigin: true
      },
      '/healthz': {
        target: PROXY_TARGET,
        changeOrigin: true
      }
    }
  }
})
