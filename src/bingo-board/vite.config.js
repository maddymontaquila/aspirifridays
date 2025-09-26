import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  server: {
    proxy: {
      // Proxy SignalR hub to the admin service
      '/bingohub': {
        target: process.env.services__boardadmin__https__0 || process.env.services__boardadmin__http__0,
        changeOrigin: true,
        secure: false,
        ws: true // Enable WebSocket proxying for SignalR
      }
    }
  }
})