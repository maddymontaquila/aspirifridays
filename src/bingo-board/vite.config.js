import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { readFileSync } from 'fs'

// Read Vite version from package.json
const packageJson = JSON.parse(readFileSync('./package.json', 'utf-8'))
const viteVersion = packageJson.devDependencies.vite.replace('^', '')

export default defineConfig({
  plugins: [vue()],
  define: {
    // Make version info available at build time
    'import.meta.env.VITE_COMMIT_SHA': JSON.stringify(process.env.VITE_COMMIT_SHA || process.env.COMMIT_SHA || 'dev'),
    'import.meta.env.VITE_DOTNET_VERSION': JSON.stringify(process.env.VITE_DOTNET_VERSION || process.env.DOTNET_VERSION || '10'),
    'import.meta.env.VITE_ASPIRE_VERSION': JSON.stringify(process.env.VITE_ASPIRE_VERSION || process.env.ASPIRE_VERSION || 'dev'),
    'import.meta.env.VITE_VERSION': JSON.stringify(viteVersion)
  },
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