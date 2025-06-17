<template>
  <div class="bingo-celebration-overlay" role="alert" aria-live="assertive">
    <button @click="handleDismiss" 
            class="close-celebration" 
            aria-label="Close celebration">×</button>
    <div class="celebration-content">
      🎉 BINGO! 🎉
      <span class="sr-only">Congratulations! You got a bingo!</span>
    </div>
  </div>
</template>

<script>
export default {
  name: 'BingoCelebrationOverlay',
  emits: ['dismiss'],
  methods: {
    handleDismiss() {
      this.$emit('dismiss')
    }
  }
}
</script>
