<template>
  <div class="bingo-container">
    <div class="game-area">
      <div class="bingo-board" 
           role="grid" 
           aria-label="Bingo board - 5 by 5 grid of AspiriFridays moments"
           @keydown="handleKeydown"
           @focus="onGridFocus"
           @blur="onGridBlur"
           tabindex="0">
        
        <div v-if="hasBingo && showInitialCelebration" class="bingo-celebration-overlay" role="alert" aria-live="assertive">
          <button @click="dismissCelebration" class="close-celebration" aria-label="Close celebration">×</button>
          <div class="celebration-content">
            🎉 BINGO! 🎉
            <span class="sr-only">Congratulations! You got a bingo!</span>
          </div>
        </div>
        
        <div 
          v-for="(square, index) in currentBoard" 
          :key="square.id"
          :class="['bingo-square', { 
            'marked': square.marked, 
            'free-space': square.type === 'free',
            'bingo-line': isPartOfBingo(index),
            'focused': focusedIndex === index && gridHasFocus
          }]"
          :role="square.type === 'free' ? 'gridcell' : 'button'"
          :aria-pressed="square.type !== 'free' ? square.marked : undefined"
          :aria-label="getSquareAriaLabel(square, index)"
          :tabindex="focusedIndex === index ? 0 : -1"
          @click="toggleSquare(index)"
          @keydown.enter.prevent="toggleSquare(index)"
          @keydown.space.prevent="toggleSquare(index)"
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
      </div>
      
      <div class="sidebar">
        <div class="bingo-celebration-area">
          <div v-if="hasBingo && !showInitialCelebration" class="bingo-celebration" role="alert" aria-live="polite">
            🎉 BINGO! 🎉
          </div>
        </div>
        
        <div class="controls">
          <button @click="resetBoard" 
                  class="reset-btn"
                  aria-label="Reset and shuffle bingo board with new random squares">
            <i class="bi bi-arrow-clockwise"></i>
            <span>New Board</span>
          </button>
          <button @click="downloadImage" 
                  class="download-btn"
                  aria-label="Download an image of the current bingo board for sharing">
            <i class="bi bi-download"></i>
            <span>Download</span>
          </button>
        </div>
        
        <div class="aspire-callout">
          <h3>New to Aspire?</h3>
          <p>Build observable, production-ready distributed applications with .NET Aspire!</p>
          <a href="https://aka.ms/dotnet/aspire" target="_blank" rel="noopener noreferrer" class="get-started-btn">
            Get Started
          </a>
          
          <div class="social-links">
            <span>Follow us:</span>
            <a href="https://youtube.com/@aspiredotdev" target="_blank" rel="noopener noreferrer" aria-label="YouTube">
              <i class="bi bi-youtube social-icon"></i>
            </a>
            <a href="https://aka.ms/aspire-discord" target="_blank" rel="noopener noreferrer" aria-label="Discord">
              <i class="bi bi-discord social-icon"></i>
            </a>
            <a href="https://github.com/dotnet/aspire" target="_blank" rel="noopener noreferrer" aria-label="GitHub">
              <i class="bi bi-github social-icon"></i>
            </a>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import bingoData from '../data/bingoSquares.json'

export default {
  name: 'BingoBoard',
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
      const freeSpace = this.allSquares.find(s => s.type === 'free')
      const otherSquares = this.allSquares.filter(s => s.type !== 'free')
      
      const shuffled = [...otherSquares].sort(() => Math.random() - 0.5)
      const selected = shuffled.slice(0, 24)
      
      const board = []
      for (let i = 0; i < 25; i++) {
        if (i === 12) {
          board.push({ ...freeSpace, marked: true })
        } else {
          const squareIndex = i < 12 ? i : i - 1
          board.push({ ...selected[squareIndex], marked: false })
        }
      }
      
      return board
    },
    
    toggleSquare(index) {
      if (this.currentBoard[index].type !== 'free') {
        this.currentBoard[index].marked = !this.currentBoard[index].marked
        this.checkForBingo()
        this.saveState()
      }
    },
    
    checkForBingo() {
      const lines = [
        [0, 1, 2, 3, 4],
        [5, 6, 7, 8, 9],
        [10, 11, 12, 13, 14],
        [15, 16, 17, 18, 19],
        [20, 21, 22, 23, 24],
        [0, 5, 10, 15, 20],
        [1, 6, 11, 16, 21],
        [2, 7, 12, 17, 22],
        [3, 8, 13, 18, 23],
        [4, 9, 14, 19, 24],
        [0, 6, 12, 18, 24],
        [4, 8, 12, 16, 20]
      ]
      
      const previousHasBingo = this.bingoLines.length > 0
      this.bingoLines = lines.filter(line => 
        line.every(index => this.currentBoard[index].marked)
      )
      
      // If we just got a new bingo, start the celebration sequence
      if (!previousHasBingo && this.bingoLines.length > 0) {
        this.showInitialCelebration = true
        setTimeout(() => {
          this.showInitialCelebration = false
        }, 3000)
      }
    },
    
    isPartOfBingo(index) {
      return this.bingoLines.some(line => line.includes(index))
    },
    
    resetBoard() {
      this.currentBoard = this.generateBoard()
      this.bingoLines = []
      this.showInitialCelebration = true
      this.saveState()
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
      } else {
        this.currentBoard = this.generateBoard()
      }
    },
    
    async downloadImage() {
      try {
        const canvas = document.createElement('canvas')
        const ctx = canvas.getContext('2d')
        const scale = 2 // Higher resolution
        
        // Set canvas size
        canvas.width = 600 * scale
        canvas.height = 600 * scale
        ctx.scale(scale, scale)
        
        // Background gradient
        const gradient = ctx.createLinearGradient(0, 0, 600, 600)
        gradient.addColorStop(0, '#2c256b')
        gradient.addColorStop(0.5, '#1a1a2e')
        gradient.addColorStop(1, '#0f0f23')
        ctx.fillStyle = gradient
        ctx.fillRect(0, 0, 600, 600)
        
        // Title
        ctx.fillStyle = '#9B5DE5'
        ctx.font = 'bold 36px Outfit, sans-serif'
        ctx.textAlign = 'center'
        ctx.fillText('AspiriFridays Bingo', 300, 50)
        
        // Bingo status
        if (this.hasBingo) {
          ctx.fillStyle = '#FF1493'
          ctx.font = 'bold 24px Rubik, sans-serif'
          ctx.fillText('🎉 BINGO! 🎉', 300, 85)
        }
        
        // Grid background with rounded corners and proper styling
        const gridSize = 400
        const gridX = (600 - gridSize) / 2
        const gridY = 120
        const squareSize = (gridSize - 40) / 5 // Account for gaps
        const gap = 8
        
        // Grid container background
        ctx.fillStyle = 'rgba(155, 93, 229, 0.1)'
        this.roundRect(ctx, gridX - 10, gridY - 10, gridSize + 20, gridSize + 20, 15)
        ctx.fill()
        
        // Load local .NET Aspire logo for free space
        let aspireImg = null
        try {
          aspireImg = new Image()
          
          await new Promise((resolve, reject) => {
            const timeout = setTimeout(() => reject(new Error('Logo load timeout')), 3000)
            aspireImg.onload = () => {
              clearTimeout(timeout)
              resolve()
            }
            aspireImg.onerror = () => {
              clearTimeout(timeout)
              reject(new Error('Logo load failed'))
            }
            aspireImg.src = '/assets/aspire-logo-256.png'
          })
        } catch (logoError) {
          console.warn('Could not load Aspire logo, using fallback:', logoError)
          aspireImg = null
        }
        
        // Draw squares with proper spacing and rounded corners
        for (let i = 0; i < 25; i++) {
          const row = Math.floor(i / 5)
          const col = i % 5
          const x = gridX + 10 + col * (squareSize + gap)
          const y = gridY + 10 + row * (squareSize + gap)
          const square = this.currentBoard[i]
          
          // Square background with rounded corners
          ctx.save()
          
          if (square.marked) {
            if (square.type === 'free') {
              const squareGradient = ctx.createLinearGradient(x, y, x + squareSize, y + squareSize)
              squareGradient.addColorStop(0, '#FF1493')
              squareGradient.addColorStop(1, '#FF69B4')
              ctx.fillStyle = squareGradient
            } else {
              const squareGradient = ctx.createLinearGradient(x, y, x + squareSize, y + squareSize)
              squareGradient.addColorStop(0, '#9B5DE5')
              squareGradient.addColorStop(1, '#7C3AED')
              ctx.fillStyle = squareGradient
            }
          } else if (square.type === 'free') {
            const squareGradient = ctx.createLinearGradient(x, y, x + squareSize, y + squareSize)
            squareGradient.addColorStop(0, '#FF1493')
            squareGradient.addColorStop(1, '#FF69B4')
            ctx.fillStyle = squareGradient
          } else {
            ctx.fillStyle = 'rgba(255, 255, 255, 0.1)'
          }
          
          this.roundRect(ctx, x, y, squareSize, squareSize, 10)
          ctx.fill()
          
          // Square border with rounded corners
          ctx.strokeStyle = this.isPartOfBingo(i) ? '#FF1493' : 'rgba(155, 93, 229, 0.3)'
          ctx.lineWidth = 2
          this.roundRect(ctx, x, y, squareSize, squareSize, 10)
          ctx.stroke()
          
          // Bingo glow effect
          if (this.isPartOfBingo(i)) {
            ctx.shadowColor = '#FF1493'
            ctx.shadowBlur = 10
            ctx.strokeStyle = '#FF1493'
            this.roundRect(ctx, x, y, squareSize, squareSize, 10)
            ctx.stroke()
          }
          
          ctx.restore()
          
          // Content
          if (square.type === 'free') {
            if (aspireImg) {
              // Draw .NET Aspire logo if loaded successfully
              const logoSize = squareSize * 0.6
              const logoX = x + (squareSize - logoSize) / 2
              const logoY = y + (squareSize - logoSize) / 2
              ctx.drawImage(aspireImg, logoX, logoY, logoSize, logoSize)
            } else {
              // Fallback to text if logo failed to load
              ctx.fillStyle = 'white'
              ctx.font = 'bold 16px Rubik, sans-serif'
              ctx.textAlign = 'center'
              ctx.fillText('FREE', x + squareSize/2, y + squareSize/2 - 4)
              ctx.font = 'bold 12px Rubik, sans-serif'
              ctx.fillText('SPACE', x + squareSize/2, y + squareSize/2 + 12)
            }
          } else {
            // Text with proper styling
            ctx.fillStyle = square.marked ? 'white' : 'rgba(255, 255, 255, 0.9)'
            ctx.font = '11px Rubik, sans-serif'
            ctx.textAlign = 'center'
            
            // Wrap text with better spacing
            const words = square.label.split(' ')
            const lines = []
            let currentLine = words[0]
            
            for (let w = 1; w < words.length; w++) {
              const testLine = currentLine + ' ' + words[w]
              const metrics = ctx.measureText(testLine)
              if (metrics.width > squareSize - 16) {
                lines.push(currentLine)
                currentLine = words[w]
              } else {
                currentLine = testLine
              }
            }
            lines.push(currentLine)
            
            const lineHeight = 13
            const startY = y + squareSize/2 - (lines.length - 1) * lineHeight/2 + 4
            
            lines.forEach((line, index) => {
              ctx.fillText(line, x + squareSize/2, startY + index * lineHeight)
            })
          }
        }
        
        // Date and watermark
        ctx.fillStyle = 'rgba(255, 255, 255, 0.5)'
        ctx.font = '12px Rubik, sans-serif'
        ctx.textAlign = 'center'
        
        const today = new Date().toLocaleDateString('en-US', { 
          year: 'numeric', 
          month: 'long', 
          day: 'numeric' 
        })
        ctx.fillText(today, 300, 560)
        ctx.fillText('youtube.com/@aspiredotdev', 300, 580)
        
        // Convert to blob and download
        canvas.toBlob((blob) => {
          const url = URL.createObjectURL(blob)
          const a = document.createElement('a')
          a.href = url
          a.download = `aspirifridays-bingo-${new Date().toISOString().split('T')[0]}.png`
          document.body.appendChild(a)
          a.click()
          document.body.removeChild(a)
          URL.revokeObjectURL(url)
        }, 'image/png')
        
      } catch (error) {
        console.error('Error generating image:', error)
        console.error('Error details:', {
          name: error.name,
          message: error.message,
          stack: error.stack
        })
        alert(`Sorry, there was an error generating the image: ${error.message}. Please try again.`)
      }
    },
    
    roundRect(ctx, x, y, width, height, radius) {
      ctx.beginPath()
      ctx.moveTo(x + radius, y)
      ctx.lineTo(x + width - radius, y)
      ctx.quadraticCurveTo(x + width, y, x + width, y + radius)
      ctx.lineTo(x + width, y + height - radius)
      ctx.quadraticCurveTo(x + width, y + height, x + width - radius, y + height)
      ctx.lineTo(x + radius, y + height)
      ctx.quadraticCurveTo(x, y + height, x, y + height - radius)
      ctx.lineTo(x, y + radius)
      ctx.quadraticCurveTo(x, y, x + radius, y)
      ctx.closePath()
    },
    
    handleKeydown(event) {
      const { key } = event
      let newIndex = this.focusedIndex
      
      switch (key) {
        case 'ArrowRight':
          event.preventDefault()
          newIndex = (this.focusedIndex % 5 === 4) ? this.focusedIndex - 4 : this.focusedIndex + 1
          break
        case 'ArrowLeft':
          event.preventDefault()
          newIndex = (this.focusedIndex % 5 === 0) ? this.focusedIndex + 4 : this.focusedIndex - 1
          break
        case 'ArrowDown':
          event.preventDefault()
          newIndex = (this.focusedIndex + 5) % 25
          break
        case 'ArrowUp':
          event.preventDefault()
          newIndex = (this.focusedIndex - 5 + 25) % 25
          break
        case 'Home':
          event.preventDefault()
          newIndex = 0
          break
        case 'End':
          event.preventDefault()
          newIndex = 24
          break
        case 'Enter':
        case ' ':
          event.preventDefault()
          this.toggleSquare(this.focusedIndex)
          return
      }
      
      this.focusedIndex = newIndex
    },
    
    getSquareAriaLabel(square, index) {
      const row = Math.floor(index / 5) + 1
      const col = (index % 5) + 1
      const position = `Row ${row}, Column ${col}`
      
      if (square.type === 'free') {
        return `${position}, Free Space with .NET Aspire logo`
      }
      
      const status = square.marked ? 'selected' : 'not selected'
      return `${position}, ${square.label}, ${status}`
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
.bingo-container {
  max-width: 600px;
  margin: 0 auto;
  padding: 20px;
  position: relative;
}

.game-area {
  display: flex;
  gap: 40px;
  align-items: flex-start;
  justify-content: center;
  max-width: 900px;
  margin: 0 auto;
}

.bingo-board {
  display: grid;
  grid-template-columns: repeat(5, 1fr);
  gap: 8px;
  background: rgba(155, 93, 229, 0.1);
  padding: 20px;
  border-radius: 15px;
  box-shadow: 0 8px 32px rgba(155, 93, 229, 0.3);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(155, 93, 229, 0.2);
  width: 600px;
  flex-shrink: 0;
  position: relative;
}

.sidebar {
  display: flex;
  flex-direction: column;
  gap: 30px;
  min-width: 250px;
}

.controls {
  display: flex;
  flex-direction: column;
  gap: 15px;
  margin-top: 20px;
}

.bingo-square {
  aspect-ratio: 1;
  background: rgba(255, 255, 255, 0.1);
  border: 2px solid rgba(155, 93, 229, 0.3);
  border-radius: 10px;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  padding: 10px;
  transition: all 0.3s ease;
  backdrop-filter: blur(5px);
  position: relative;
  overflow: hidden;
}

.bingo-square:focus {
  outline: 3px solid #00F5FF;
  outline-offset: 2px;
}

.bingo-square.focused {
  border-color: #00F5FF;
  box-shadow: 0 0 0 2px #00F5FF;
}

.bingo-square:hover {
  background: rgba(155, 93, 229, 0.2);
  transform: scale(1.05);
  box-shadow: 0 4px 20px rgba(155, 93, 229, 0.4);
}

.bingo-square.marked {
  background: linear-gradient(135deg, #9B5DE5, #7C3AED);
  border-color: #9B5DE5;
  color: white;
}

.bingo-square.free-space {
  background: linear-gradient(135deg, #FF1493, #FF69B4);
  border-color: #FF1493;
  color: white;
}

.bingo-square.bingo-line {
  animation: bingo-glow 2s infinite alternate;
}

.square-content {
  text-align: center;
  font-size: 0.85rem;
  font-weight: 500;
  line-height: 1.2;
}

.aspire-logo {
  width: 60%;
  height: auto;
  max-width: 80px;
}

.square-text {
  display: block;
  word-wrap: break-word;
  hyphens: auto;
  position: relative;
  font-weight: 400;
}

.sr-only {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

.reset-btn, .download-btn {
  padding: 15px 20px;
  border: none;
  border-radius: 12px;
  font-family: 'Outfit', sans-serif;
  font-weight: 600;
  font-size: 1.1rem;
  cursor: pointer;
  transition: all 0.3s ease;
  backdrop-filter: blur(10px);
  display: flex;
  align-items: center;
  gap: 10px;
  min-width: 160px;
  justify-content: center;
}

.reset-btn i, .download-btn i {
  font-size: 1.3rem;
}

.reset-btn {
  background: linear-gradient(135deg, #9B5DE5, #7C3AED);
  color: white;
}

.download-btn {
  background: linear-gradient(135deg, #00FFFF, #40E0D0);
  color: #2c256b;
}

.reset-btn:hover, .download-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 25px rgba(155, 93, 229, 0.3);
}

.aspire-callout {
  padding: 25px;
  background: rgba(155, 93, 229, 0.1);
  border-radius: 15px;
  text-align: center;
  backdrop-filter: blur(10px);
  border: 1px solid rgba(155, 93, 229, 0.2);
}

.aspire-callout h3 {
  font-family: 'Outfit', sans-serif;
  font-size: 1.5rem;
  font-weight: 600;
  color: #9B5DE5;
  margin-bottom: 10px;
}

.aspire-callout p {
  font-family: 'Rubik', sans-serif;
  font-size: 1rem;
  color: rgba(255, 255, 255, 0.8);
  margin-bottom: 20px;
  line-height: 1.4;
}

.get-started-btn {
  display: inline-block;
  padding: 12px 30px;
  background: linear-gradient(135deg, #9B5DE5, #7C3AED);
  color: white;
  text-decoration: none;
  border-radius: 25px;
  font-family: 'Outfit', sans-serif;
  font-weight: 600;
  transition: all 0.3s ease;
  margin-bottom: 20px;
}

.get-started-btn:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 25px rgba(155, 93, 229, 0.4);
}

.social-links {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 15px;
  margin-top: 20px;
}

.social-links span {
  font-family: 'Rubik', sans-serif;
  color: rgba(255, 255, 255, 0.7);
  font-size: 0.9rem;
}

.social-links a {
  text-decoration: none;
  transition: transform 0.3s ease;
  display: inline-block;
}

.social-links a:hover {
  transform: scale(1.1);
}

.social-icon {
  font-size: 1.5rem;
  color: rgba(255, 255, 255, 0.7);
  transition: all 0.3s ease;
}

.social-icon:hover {
  color: #9B5DE5;
  transform: scale(1.1);
}

.bingo-celebration-overlay {
  position: absolute;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
  text-align: center;
  font-size: 4rem;
  font-family: 'Outfit', sans-serif;
  font-weight: 600;
  color: #FF1493;
  text-shadow: 3px 3px 6px rgba(0,0,0,0.8);
  background: rgba(0,0,0,0.7);
  border-radius: 15px;
  backdrop-filter: blur(10px);
}

.bingo-celebration-overlay .celebration-content {
  animation: celebration-pulse 0.6s ease-in-out infinite alternate;
}

.close-celebration {
  position: absolute;
  top: 10px;
  right: 15px;
  background: none;
  border: none;
  color: rgba(255, 255, 255, 0.8);
  font-size: 2rem;
  font-weight: bold;
  cursor: pointer;
  padding: 0;
  width: 30px;
  height: 30px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  transition: all 0.3s ease;
}

.close-celebration:hover {
  background: rgba(255, 255, 255, 0.1);
  color: white;
  transform: scale(1.1);
}

.bingo-celebration-area {
  height: 80px;
  margin-bottom: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.bingo-celebration {
  text-align: center;
  font-size: 1.8rem;
  font-family: 'Outfit', sans-serif;
  font-weight: 600;
  color: #FF1493;
  text-shadow: 2px 2px 4px rgba(0,0,0,0.5);
  padding: 15px;
  background: rgba(255, 20, 147, 0.1);
  border-radius: 12px;
  border: 1px solid rgba(255, 20, 147, 0.2);
  backdrop-filter: blur(10px);
}

@keyframes bingo-glow {
  0% { box-shadow: 0 0 20px rgba(255, 20, 147, 0.5); }
  100% { box-shadow: 0 0 30px rgba(255, 20, 147, 0.8); }
}

@keyframes celebration-pulse {
  0% { transform: scale(1); }
  100% { transform: scale(1.1); }
}

@media (max-width: 768px) {
  .bingo-container {
    padding: 10px;
  }
  
  .game-area {
    flex-direction: column;
    align-items: center;
  }
  
  .bingo-board {
    gap: 4px;
    padding: 15px;
    width: 100%;
    max-width: 400px;
  }
  
  .sidebar {
    width: 100%;
    max-width: 400px;
    gap: 20px;
  }
  
  .controls {
    flex-direction: row;
    justify-content: center;
    margin-top: 20px;
  }
  
  .reset-btn, .download-btn {
    min-width: 120px;
    font-size: 0.9rem;
  }
  
  .square-content {
    font-size: 0.7rem;
  }
  
  .aspire-logo {
    width: 50%;
    max-width: 60px;
  }
  
  .bingo-celebration-overlay {
    font-size: 2.5rem;
    padding: 15px 25px;
  }
  
  .bingo-celebration-area {
    height: 60px;
    margin-bottom: 15px;
  }
  
  .bingo-celebration {
    font-size: 1.4rem;
    padding: 12px;
  }
}
</style>