<template>
  <div class="bingo-celebration-area">
    <div v-if="hasBingo" class="bingo-celebration" role="alert" aria-live="polite">
      🎉 BINGO! 🎉
    </div>
  </div>
</template>

<script>
export default {
  name: 'BingoCelebrationArea',
  props: {
    hasBingo: {
      type: Boolean,
      default: false
    }
  }
}
</script>
