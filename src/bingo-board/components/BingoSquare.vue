<template>
  <div
    :class="['bingo-square', {
      'marked': square.marked,
      'free-space': square.type === 'free',
      'bingo-line': isBingoLine,
      'focused': isFocused,
      'disabled': disabled,
      'pending': isPending,
      'is-long': labelLength > 26,
      'is-xlong': labelLength > 36
    }]"
    :style="isBingoLine ? { '--line-delay': `${(index % 5) * 60 + Math.floor(index / 5) * 60}ms` } : null"
    :role="square.type === 'free' ? 'gridcell' : 'button'"
    :aria-pressed="square.type !== 'free' ? square.marked : undefined"
    :aria-label="ariaLabel"
    :aria-disabled="disabled"
    :aria-busy="isPending || undefined"
    :tabindex="isFocused ? 0 : -1"
    @click="handleClick"
    @keydown.enter.prevent="handleClick"
    @keydown.space.prevent="handleClick"
  >
    <template v-if="square.type === 'free'">
      <img src="/assets/aspire-logo-256.png" alt="" class="free-space__logo">
      <span class="free-space__label">Free</span>
    </template>
    <span v-else class="square-text">{{ displayLabel }}</span>

    <span v-if="isPending" class="square-badge square-badge--pending" aria-hidden="true">
      <i class="bi bi-hourglass-split"></i>
    </span>
    <span v-else-if="square.marked && square.type !== 'free'" class="square-badge" aria-hidden="true">
      <i class="bi bi-check-lg"></i>
    </span>
  </div>
</template>

<script>
import { BingoGameLogic } from '../utils/bingoLogic.js'
import { softHyphenate } from '../utils/softHyphenate.js'

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
    },
    isPending: {
      type: Boolean,
      default: false
    },
    disabled: {
      type: Boolean,
      default: false
    }
  },
  emits: ['toggle'],
  computed: {
    labelLength() {
      return (this.square.label || '').length
    },
    displayLabel() {
      return softHyphenate(this.square.label)
    },
    ariaLabel() {
      const label = BingoGameLogic.getSquareAriaLabel(this.square, this.index)
      return this.isPending ? `${label}, waiting for the host to confirm` : label
    }
  },
  methods: {
    handleClick() {
      if (!this.disabled && !this.isPending) {
        this.$emit('toggle', this.index)
      }
    }
  }
}
</script>
