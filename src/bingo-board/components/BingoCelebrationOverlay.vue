<template>
  <div class="bingo-celebration-overlay" role="alert" aria-live="assertive">
    <div class="confetti" aria-hidden="true">
      <span v-for="n in 18" :key="n" class="confetti__piece"></span>
    </div>
    <div class="celebration-content">
      <p class="celebration-content__title">Bingo!</p>
      <p class="celebration-content__text">You completed a line.</p>
      <button type="button" class="btn btn--light" @click="handleDismiss">
        Keep playing
      </button>
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
