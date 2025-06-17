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
@import url('https://fonts.googleapis.com/css2?family=Outfit:wght@400;600;700&family=Rubik:wght@400;500;600&display=swap');

.app {
  min-height: 100vh;
  background: linear-gradient(135deg, #2c256b 0%, #1a1a2e 50%, #0f0f23 100%);
  color: white;
  font-family: 'Rubik', sans-serif;
  padding: 20px;
}

h1 {
  font-family: 'Outfit', sans-serif;
  font-size: 3rem;
  color: #9B5DE5;
  text-align: center;
  margin-bottom: 0.5rem;
  text-shadow: 2px 2px 4px rgba(0,0,0,0.5);
}

.subheading {
  font-family: 'Rubik', sans-serif;
  font-size: 1rem;
  color: rgba(255, 255, 255, 0.7);
  text-align: center;
  margin-bottom: 1rem;
  font-weight: 400;
}

.subheading a {
  color: #00F5FF;
  text-decoration: none;
  transition: color 0.3s ease;
}

.subheading a:hover {
  color: #FF1493;
  text-decoration: underline;
}

.skip-link {
  position: absolute;
  top: -40px;
  left: 6px;
  background: #00F5FF;
  color: #2c256b;
  padding: 8px;
  border-radius: 4px;
  text-decoration: none;
  font-weight: bold;
  z-index: 1000;
  transition: top 0.3s;
}

.skip-link:focus {
  top: 6px;
}
</style>