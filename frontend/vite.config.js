import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/auth': {
        target: 'http://127.0.0.1:5028',
        changeOrigin: true,
      },
      '/banners': {
        target: 'http://127.0.0.1:5028',
        changeOrigin: true,
      },
      '/comics': {
        target: 'http://127.0.0.1:5028',
        changeOrigin: true,
      },
      '/categories': {
        target: 'http://127.0.0.1:5028',
        changeOrigin: true,
      },
      '/translations': {
        target: 'http://127.0.0.1:5028',
        changeOrigin: true,
      },
      '/chapters': {
        target: 'http://127.0.0.1:5028',
        changeOrigin: true,
      },
      '/missions': {
        target: 'http://127.0.0.1:5028',
        changeOrigin: true,
      },
      '/notifications': {
        target: 'http://127.0.0.1:5028',
        changeOrigin: true,
      },
      '/comments': { target: 'http://127.0.0.1:5028', changeOrigin: true },
      '/favorites': { target: 'http://127.0.0.1:5028', changeOrigin: true },
      '/follows': { target: 'http://127.0.0.1:5028', changeOrigin: true },
      '/reading-history': { target: 'http://127.0.0.1:5028', changeOrigin: true },
      '/payments': { target: 'http://127.0.0.1:5028', changeOrigin: true },
      '/withdraws': { target: 'http://127.0.0.1:5028', changeOrigin: true },
    },
  },
})
