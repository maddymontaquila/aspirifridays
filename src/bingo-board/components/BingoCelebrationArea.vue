<template>
  <div v-if="hasBingo" class="bingo-banner" role="status">
    <i class="bi bi-stars" aria-hidden="true"></i>
    <span>You have bingo!</span>
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
