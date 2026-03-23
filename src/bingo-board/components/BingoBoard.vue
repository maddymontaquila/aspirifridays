<template>
  <div class="bingo-container">
    <div class="game-area">
      <div class="bingo-board glass-card" 
           role="grid" 
           aria-label="Bingo board - 5 by 5 grid of #AspireConf moments"
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
        <BingoCelebrationArea 
          :has-bingo="hasBingo && !showInitialCelebration"
          @download-image="downloadImage" />
        
        <div class="controls">
          <button @click="requestNewBoard" 
                  class="btn btn--primary"
                  aria-label="Get a completely new bingo board">
            <i class="bi bi-arrow-clockwise"></i>
            <span>New Board</span>
          </button>
          
          <button @click="downloadImage" 
                  :disabled="!currentBoard.length"
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
import { BingoGameLogic, KeyboardNavigation } from '../utils/bingoLogic.js'
import { BingoImageGenerator } from '../utils/imageGenerator.js'
import bingoSquares from '../data/bingoSquares.json'

// Component imports
import BingoSquare from './BingoSquare.vue'
import BingoCelebrationOverlay from './BingoCelebrationOverlay.vue'
import BingoCelebrationArea from './BingoCelebrationArea.vue'
import AspireCallout from './AspireCallout.vue'

const STORAGE_KEY = 'aspireconf-bingo'

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
      allSquares: bingoSquares,
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
      this.currentBoard = BingoGameLogic.generateBoard(this.allSquares)
      this.bingoLines = []
      this.showInitialCelebration = true
      this.saveState()
    },
    
    toggleSquare(index) {
      const square = this.currentBoard[index]
      if (square.type === 'free') return
      
      square.marked = !square.marked
      this.checkForBingo()
      this.saveState()
    },
    
    checkForBingo() {
      const previousHasBingo = this.bingoLines.length > 0
      this.bingoLines = BingoGameLogic.checkForBingo(this.currentBoard)
      
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
    
    requestNewBoard() {
      localStorage.removeItem(STORAGE_KEY)
      this.generateBoard()
    },
    
    saveState() {
      localStorage.setItem(STORAGE_KEY, JSON.stringify({
        board: this.currentBoard,
        bingoLines: this.bingoLines,
        showInitialCelebration: this.showInitialCelebration,
        timestamp: Date.now()
      }))
    },
    
    loadState() {
      const saved = localStorage.getItem(STORAGE_KEY)
      if (saved) {
        try {
          const state = JSON.parse(saved)
          const age = Date.now() - (state.timestamp || 0)
          
          // Only load state if less than 24 hours old
          if (age < 24 * 60 * 60 * 1000 && state.board && state.board.length === 25) {
            this.currentBoard = state.board
            this.bingoLines = state.bingoLines || []
            this.showInitialCelebration = state.showInitialCelebration !== undefined ? 
              state.showInitialCelebration : (this.bingoLines.length === 0)
            return true
          }
        } catch (error) {
          console.error('Failed to load saved state:', error)
        }
      }
      return false
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
    if (!this.loadState()) {
      this.generateBoard()
    }
  }
}
</script>

<style scoped>
button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}
</style>