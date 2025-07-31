import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  define: {
    // Expose Aspire service environment variables to the client
    'admin_http': JSON.stringify(process.env.services__board_admin__http__0),
    'admin_https': JSON.stringify(process.env.services__board_admin__https__0),
  }
})