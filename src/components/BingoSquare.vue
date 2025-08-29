<template>
  <div 
    :class="['bingo-square', { 
      'marked': square.marked, 
      'free-space': square.type === 'free',
      'bingo-line': isBingoLine,
      'focused': isFocused
    }]"
    :role="square.type === 'free' ? 'gridcell' : 'button'"
    :aria-pressed="square.type !== 'free' ? square.marked : undefined"
    :aria-label="ariaLabel"
    :tabindex="isFocused ? 0 : -1"
    @click="handleClick"
    @keydown.enter.prevent="handleClick"
    @keydown.space.prevent="handleClick"
  >
    <div class="square-content">
      <img v-if="square.type === 'free'" 
           src="/assets/aspire-logo-256.png" 
           alt=".NET Aspire Logo" 
           class="aspire-logo">
      <span v-else class="square-text">
        {{ square.label }}
      </span>
    </div>
  </div>
</template>

<script>
import { BingoGameLogic } from '../utils/bingoLogic.js'

export default {
  name: 'BingoSquare',
  props: {
    square: {
      type: Object,
      required: true
    },
    index: {
      type: Number,
      required: true
    },
    isFocused: {
      type: Boolean,
      default: false
    },
    isBingoLine: {
      type: Boolean,
      default: false
    }
  },
  emits: ['toggle'],
  computed: {
    ariaLabel() {
      return BingoGameLogic.getSquareAriaLabel(this.square, this.index)
    }
  },
  methods: {
    handleClick() {
      this.$emit('toggle', this.index)
    }
  }
}
</script>
