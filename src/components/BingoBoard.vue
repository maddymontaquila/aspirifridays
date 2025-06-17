<template>
  <div class="bingo-container">
    <div class="game-area">
      <div class="bingo-board glass-card" 
           role="grid" 
           aria-label="Bingo board - 5 by 5 grid of AspiriFridays moments"
           @keydown="handleKeydown"
           @focus="onGridFocus"
           @blur="onGridBlur"
           tabindex="0">
        
        <BingoCelebrationOverlay 
          v-if="hasBingo && showInitialCelebration"
          @dismiss="dismissCelebration" />
        
        <BingoSquare
          v-for="(square, index) in currentBoard" 
          :key="square.id"
          :square="square"
          :index="index"
          :is-focused="focusedIndex === index && gridHasFocus"
          :is-bingo-line="isPartOfBingo(index)"
          @toggle="toggleSquare" />
      </div>
      
      <div class="sidebar">
        <BingoCelebrationArea :has-bingo="hasBingo && !showInitialCelebration" />
        
        <div class="controls">
          <button @click="resetBoard" 
                  class="btn btn--primary"
                  aria-label="Reset and shuffle bingo board with new random squares">
            <i class="bi bi-arrow-clockwise"></i>
            <span>New Board</span>
          </button>
          <button @click="downloadImage" 
                  class="btn btn--accent"
                  aria-label="Download an image of the current bingo board for sharing">
            <i class="bi bi-download"></i>
            <span>Download</span>
          </button>
        </div>
        
        <AspireCallout />
      </div>
    </div>
  </div>
</template>

<script>
import bingoData from '../data/bingoSquares.json'
import { BingoGameLogic, KeyboardNavigation } from '../utils/bingoLogic.js'
import { BingoImageGenerator } from '../utils/imageGenerator.js'

// Component imports
import BingoSquare from './BingoSquare.vue'
import BingoCelebrationOverlay from './BingoCelebrationOverlay.vue'
import BingoCelebrationArea from './BingoCelebrationArea.vue'
import AspireCallout from './AspireCallout.vue'

export default {
  name: 'BingoBoard',
  components: {
    BingoSquare,
    BingoCelebrationOverlay,
    BingoCelebrationArea,
    AspireCallout
  },
  data() {
    return {
      allSquares: bingoData,
      currentBoard: [],
      bingoLines: [],
      focusedIndex: 0,
      gridHasFocus: false,
      showInitialCelebration: true
    }
  },
  computed: {
    hasBingo() {
      return this.bingoLines.length > 0
    }
  },
  methods: {
    generateBoard() {
      return BingoGameLogic.generateBoard(this.allSquares)
    },
    
    toggleSquare(index) {
      if (this.currentBoard[index].type !== 'free') {
        this.currentBoard[index].marked = !this.currentBoard[index].marked
        this.checkForBingo()
        this.saveState()
      }
    },
    
    checkForBingo() {
      const previousHasBingo = this.bingoLines.length > 0
      this.bingoLines = BingoGameLogic.checkForBingo(this.currentBoard)
      
      // If we just got a new bingo, start the celebration sequence
      if (!previousHasBingo && this.bingoLines.length > 0) {
        this.showInitialCelebration = true
        setTimeout(() => {
          this.showInitialCelebration = false
        }, 3000)
      }
    },
    
    isPartOfBingo(index) {
      return BingoGameLogic.isPartOfBingo(index, this.bingoLines)
    },
    
    resetBoard() {
      // Generate a fresh randomized board
      this.currentBoard = this.generateBoard()
      this.bingoLines = []
      this.showInitialCelebration = true
      this.saveState()
      console.log('Generated new randomized bingo board')
    },
    
    saveState() {
      localStorage.setItem('aspirifridays-bingo', JSON.stringify({
        board: this.currentBoard,
        bingoLines: this.bingoLines,
        showInitialCelebration: this.showInitialCelebration
      }))
    },
    
    loadState() {
      const saved = localStorage.getItem('aspirifridays-bingo')
      if (saved) {
        const state = JSON.parse(saved)
        this.currentBoard = state.board
        this.bingoLines = state.bingoLines || []
        // If we're loading a saved state that already has a bingo, don't show the initial celebration
        this.showInitialCelebration = this.bingoLines.length === 0
        console.log('Loaded saved bingo board from localStorage')
      } else {
        // First time visitor - generate a fresh randomized board
        this.currentBoard = this.generateBoard()
        console.log('Generated new randomized bingo board for first-time visitor')
      }
    },
    
    async downloadImage() {
      try {
        const generator = new BingoImageGenerator(
          this.currentBoard, 
          this.bingoLines, 
          this.isPartOfBingo
        )
        await generator.generateImage()
      } catch (error) {
        alert(`Sorry, there was an error generating the image: ${error.message}. Please try again.`)
      }
    },
    
    handleKeydown(event) {
      const { key } = event
      
      if (['Enter', ' '].includes(key)) {
        event.preventDefault()
        this.toggleSquare(this.focusedIndex)
        return
      }
      
      const newIndex = KeyboardNavigation.handleArrowKey(this.focusedIndex, key)
      if (newIndex !== this.focusedIndex) {
        event.preventDefault()
        this.focusedIndex = newIndex
      }
    },
    
    onGridFocus() {
      this.gridHasFocus = true
    },
    
    onGridBlur() {
      this.gridHasFocus = false
    },
    
    dismissCelebration() {
      this.showInitialCelebration = false
    }
  },
  
  mounted() {
    this.loadState()
  }
}
</script>

<style scoped>
/* Component-specific styles are handled by the global CSS modules */
</style>

<style scoped>
/* Component-specific styles are handled by the global CSS modules */
</style>