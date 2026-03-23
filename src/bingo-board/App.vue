<template>
  <div class="app">
    <a href="#main-content" class="skip-link">Skip to main content</a>
    <header>
      <h1>#AspireConf Bingo</h1>
      <p class="subheading">Watch live on <a href="https://www.youtube.com/watch?v=6j61K9Sna2M" target="_blank" rel="noopener noreferrer">YouTube</a> · <a href="https://conf.aspire.dev" target="_blank" rel="noopener noreferrer">conf.aspire.dev</a></p>
    </header>
    <main id="main-content">
      <BingoBoard />
    </main>
    <footer class="version-footer">
      <a href="https://github.com/maddymontaquila/aspirifridays/tree/aspireconf" target="_blank">aspireconf</a> ·
      <a href="https://aspire.dev" target="_blank">Aspire 13.2</a> ·
      <a href="https://vitejs.dev" target="_blank">Vite 6.3</a>
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
    return {}
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