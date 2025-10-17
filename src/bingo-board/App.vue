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
      <div class="version-info">{{ versionString }}</div>
    </footer>
  </div>
</template>

<script>
import BingoBoard from './components/BingoBoard.vue'
import { getVersionString } from './utils/versionInfo.js'

export default {
  name: 'App',
  components: {
    BingoBoard
  },
  data() {
    return {
      versionString: getVersionString()
    }
  }
}
</script>

<style>
/* App-specific styles are now handled by the global CSS modules */

.version-footer {
  margin-top: 2rem;
  padding: 1rem 0;
  text-align: center;
  border-top: 1px solid rgba(255, 255, 255, 0.2);
}

.version-info {
  font-size: 0.75rem;
  color: rgba(255, 255, 255, 0.6);
  font-family: 'Courier New', monospace;
}
</style>