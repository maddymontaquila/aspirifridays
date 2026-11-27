<template>
  <div class="app">
    <a href="#main-content" class="skip-link">Skip to main content</a>

    <header class="site-header">
      <a class="brand" href="https://aspire.dev" target="_blank" rel="noopener noreferrer">
        <img src="/assets/aspire-logo-256.png" alt="Aspire" class="brand__mark" width="36" height="36">
        <span class="brand__divider" aria-hidden="true"></span>
        <h1 class="brand__title">AspiriFridays Bingo</h1>
      </a>

      <div class="header-actions">
        <a class="header-link" href="https://youtube.com/@aspiredotdev" target="_blank" rel="noopener noreferrer">
          <i class="bi bi-youtube" aria-hidden="true"></i>
          <span>Watch the stream</span>
        </a>
        <button type="button"
                class="icon-button"
                :aria-label="theme === 'dark' ? 'Switch to light theme' : 'Switch to dark theme'"
                :title="theme === 'dark' ? 'Light theme' : 'Dark theme'"
                @click="toggleTheme">
          <i :class="theme === 'dark' ? 'bi bi-sun' : 'bi bi-moon-stars'" aria-hidden="true"></i>
        </button>
      </div>
    </header>

    <main id="main-content">
      <BingoBoard />
    </main>

    <footer class="site-footer">
      <nav class="site-footer__links" aria-label="Aspire community">
        <a href="https://aspire.dev" target="_blank" rel="noopener noreferrer">
          <img src="/assets/aspire-logo-256.png" alt="" width="18" height="18">
          aspire.dev
        </a>
        <a href="https://aka.ms/aspire-discord" target="_blank" rel="noopener noreferrer">
          <i class="bi bi-discord" aria-hidden="true"></i>
          Discord
        </a>
        <a href="https://github.com/dotnet/aspire" target="_blank" rel="noopener noreferrer">
          <i class="bi bi-github" aria-hidden="true"></i>
          GitHub
        </a>
      </nav>
      <p class="site-footer__version">
        <a :href="commitUrl" target="_blank" rel="noopener noreferrer">{{ commitHash }}</a>
        <span aria-hidden="true">·</span>
        <a href="https://dot.net" target="_blank" rel="noopener noreferrer">.NET {{ dotnetVersion }}</a>
        <span aria-hidden="true">·</span>
        <a href="https://aspire.dev" target="_blank" rel="noopener noreferrer">Aspire {{ aspireVersion }}</a>
        <span aria-hidden="true">·</span>
        <a href="https://vitejs.dev" target="_blank" rel="noopener noreferrer">Vite {{ viteVersion }}</a>
      </p>
    </footer>
  </div>
</template>

<script>
import BingoBoard from './components/BingoBoard.vue'

const THEME_KEY = 'aspirifridays-theme'

export default {
  name: 'App',
  components: {
    BingoBoard
  },
  data() {
    const fullSha = import.meta.env.VITE_COMMIT_SHA || 'dev'
    return {
      theme: document.documentElement.dataset.theme === 'light' ? 'light' : 'dark',
      commitHash: fullSha.length >= 7 && fullSha !== 'dev' ? fullSha.substring(0, 7) : 'dev',
      commitUrl: fullSha.length > 0 && fullSha !== 'dev'
        ? `https://github.com/maddymontaquila/aspirifridays/commit/${fullSha}`
        : 'https://github.com/maddymontaquila/aspirifridays',
      dotnetVersion: import.meta.env.VITE_DOTNET_VERSION || 'dev',
      aspireVersion: import.meta.env.VITE_ASPIRE_VERSION || 'dev',
      viteVersion: import.meta.env.VITE_VERSION || 'dev'
    }
  },
  async mounted() {
    await this.loadVersionInfo()
  },
  methods: {
    toggleTheme() {
      this.theme = this.theme === 'dark' ? 'light' : 'dark'
      document.documentElement.dataset.theme = this.theme
      document.querySelector('meta[name="theme-color"]')?.setAttribute('content', this.theme === 'light' ? '#f6f4fb' : '#0f0d1d')
      try {
        localStorage.setItem(THEME_KEY, this.theme)
      } catch {
        // Storage can be unavailable in private browsing; the theme still applies for this visit.
      }
    },
    async loadVersionInfo() {
      try {
        const response = await fetch('/api/version-info', {
          headers: {
            Accept: 'application/json'
          }
        })

        if (!response.ok) {
          throw new Error(`Version endpoint returned ${response.status}`)
        }

        const versionInfo = await response.json()
        this.commitHash = versionInfo.commitHash || this.commitHash
        this.commitUrl = versionInfo.commitUrl || this.commitUrl
        this.dotnetVersion = versionInfo.dotnetVersion || this.dotnetVersion
        this.aspireVersion = versionInfo.aspireVersion || this.aspireVersion
        this.viteVersion = versionInfo.viteVersion || this.viteVersion
      } catch (error) {
        console.warn('Failed to load runtime version info.', error)
      }
    }
  }
}
</script>
