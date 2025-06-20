import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  define: {
    // Expose Aspire service environment variables to the client
    'import.meta.env.services__board_admin__http__0': JSON.stringify(process.env.services__board_admin__http__0),
    'import.meta.env.services__board_admin__https__0': JSON.stringify(process.env.services__board_admin__https__0),
  }
})