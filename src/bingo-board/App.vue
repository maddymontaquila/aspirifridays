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
  </div>
</template>

<script>
import BingoBoard from './components/BingoBoard.vue'

export default {
  name: 'App',
  components: {
    BingoBoard
  }
}
</script>

<style>
/* App-specific styles are now handled by the global CSS modules */
</style>