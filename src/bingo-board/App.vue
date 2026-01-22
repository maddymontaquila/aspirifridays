<template>
  <div class="app">
    <a href="#main-content" class="skip-link">Skip to main content</a>
    <header>
      <h1>AspiriFridays Bingo</h1>
      <p class="subheading">Follow along on <a href="https://youtube.com/@aspiredotdev" target="_blank" rel="noopener noreferrer">youtube.com/@aspiredotdev</a></p>
    </header>
    <main id="main-content">
      <BingoBoard />
    </main>
    <footer class="version-footer">
      <a :href="commitUrl" target="_blank">{{ commitHash }}</a> ·
      <a href="https://dot.net" target="_blank">.NET {{ dotnetVersion }}</a> ·
      <a href="https://aspire.dev" target="_blank">Aspire {{ aspireVersion }}</a> ·
      <a href="https://vitejs.dev" target="_blank">Vite {{ viteVersion }}</a>
    </footer>
  </div>
</template>

<script>
import BingoBoard from './components/BingoBoard.vue'

export default {
  name: 'App',
  components: {
    BingoBoard
  },
  data() {
    const fullSha = import.meta.env.VITE_COMMIT_SHA || ''
    return {
      commitHash: fullSha.length >= 7 ? fullSha.substring(0, 7) : 'dev',
      commitUrl: fullSha.length > 0 
        ? `https://github.com/maddymontaquila/aspirifridays/commit/${fullSha}`
        : 'https://github.com/maddymontaquila/aspirifridays',
      dotnetVersion: import.meta.env.VITE_DOTNET_VERSION || 'dev',
      aspireVersion: import.meta.env.VITE_ASPIRE_VERSION || 'dev',
      viteVersion: import.meta.env.VITE_VERSION || 'dev'
    }
  }
}
</script>

<style>
.version-footer {
  margin-top: 2rem;
  padding: 1rem 0;
  text-align: center;
  font-size: 0.75rem;
}

.version-footer a {
  color: rgba(255, 255, 255, 0.5);
  text-decoration: none;
}

.version-footer a:hover {
  color: rgba(255, 255, 255, 0.8);
  text-decoration: underline;
}
</style>