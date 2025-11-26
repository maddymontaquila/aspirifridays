<template>
  <div class="bingo-container">
    <!-- Connection Status -->
    <div v-if="!isConnected || isReconnecting || isLoading || error" class="connection-status">
      <div v-if="isLoading" class="status-message loading">
        <i class="bi bi-arrow-clockwise spinning"></i>
        <span>Connecting to server...</span>
      </div>
      <div v-else-if="!isConnected && isReconnecting" class="status-message reconnecting">
        <i class="bi bi-arrow-clockwise spinning"></i>
        <span>Reconnecting to server...</span>
      </div>
      <div v-else-if="!isConnected" class="status-message disconnected">
        <i class="bi bi-exclamation-triangle"></i>
        <span>Disconnected from server</span>
      </div>
      <div v-if="error" class="status-message error">
        <i class="bi bi-exclamation-circle"></i>
        <span>{{ error }}</span>
      </div>
    </div>

    <div class="game-area">
      <div class="bingo-board glass-card" 
           :class="{ 'disabled': !isConnected || isLoading }"
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
          :is-pending="pendingSquares.has(square.id)"
          :disabled="!isConnected || isLoading"
          @toggle="toggleSquare" />
      </div>
      
      <div class="sidebar">
        <BingoCelebrationArea :has-bingo="hasBingo && !showInitialCelebration" />
        
        <div class="controls">
          <button @click="requestNewBoard" 
                  :disabled="!isConnected || isLoading"
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
        
        <!-- Live Mode Indicator -->
        <div class="mode-indicator" :class="{ 'live-mode': isLiveMode, 'free-play-mode': !isLiveMode }">
          <div class="mode-header">
            <i :class="isLiveMode ? 'bi bi-broadcast-pin' : 'bi bi-play-circle'"></i>
            <span class="mode-title">{{ isLiveMode ? 'Live Stream Active' : 'Free Play Mode' }}</span>
          </div>
          <p class="mode-description">
            {{ isLiveMode 
              ? 'Squares require admin approval before being marked'
              : 'No live stream in progress - mark squares freely!' 
            }}
          </p>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import { BingoGameLogic, KeyboardNavigation } from '../utils/bingoLogic.js'
import { BingoImageGenerator } from '../utils/imageGenerator.js'
import { signalRService } from '../services/signalrService.js'
import { getPersistentClientId, clearPersistentClientId } from '../utils/clientId.js'

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
      currentBoard: [],
      bingoLines: [],
      focusedIndex: 0,
      gridHasFocus: false,
      showInitialCelebration: true,
      connectionState: { connected: false, reconnecting: false },
      currentBingoSet: null,
      isLoading: false,
      error: null,
      pendingSquares: new Set(), // Track squares that are pending approval
      persistentClientId: null,
      isLiveMode: true, // Default to live mode, will be updated from server
      liveModeMessage: ''
    }
  },
  computed: {
    hasBingo() {
      return this.bingoLines.length > 0
    },
    isConnected() {
      return this.connectionState.connected
    },
    isReconnecting() {
      return this.connectionState.reconnecting
    }
  },
  methods: {
    /**
     * Initialize SignalR connection and event handlers
     */
    async initializeSignalR() {
      try {
        this.isLoading = true
        this.error = null

        // Get or create persistent client ID
        this.persistentClientId = getPersistentClientId()
        console.log('Using persistent client ID:', this.persistentClientId)

        // Set up event listeners
        signalRService.addEventListener('connectionStateChanged', this.onConnectionStateChanged)
        signalRService.addEventListener('bingoSetReceived', this.onBingoSetReceived)
        signalRService.addEventListener('existingBingoSetReceived', this.onExistingBingoSetReceived)
        signalRService.addEventListener('squareUpdated', this.onSquareUpdated)
        signalRService.addEventListener('squareUpdateConfirmed', this.onSquareUpdateConfirmed)
        signalRService.addEventListener('bingoAchieved', this.onBingoAchieved)
        signalRService.addEventListener('globalSquareUpdate', this.onGlobalSquareUpdate)
        signalRService.addEventListener('error', this.onSignalRError)
        
        // Approval workflow event listeners
        signalRService.addEventListener('approvalRequestSubmitted', this.onApprovalRequestSubmitted)
        signalRService.addEventListener('approvalRequestApproved', this.onApprovalRequestApproved)
        signalRService.addEventListener('approvalRequestDenied', this.onApprovalRequestDenied)
        
        // Live mode event listener
        signalRService.addEventListener('liveModeChanged', this.onLiveModeChanged)

        // Connect to SignalR hub
        await signalRService.connect()
        
        // Load cached state for immediate display (if available)
        this.loadStateAsCache()
        
        // Always request current state from server (authoritative source)
        await this.requestExistingBingoSet()
      } catch (error) {
        console.error('Failed to initialize SignalR:', error)
        this.error = `Failed to connect to server: ${error.message}`
      } finally {
        this.isLoading = false
      }
    },

    /**
     * Request a new bingo set from the server
     */
    async requestNewBingoSet() {
      try {
        this.isLoading = true
        this.error = null
        await signalRService.requestBingoSet(this.persistentClientId)
      } catch (error) {
        console.error('Failed to request new bingo set:', error)
        this.error = `Failed to get new bingo board: ${error.message}`
        this.isLoading = false
      }
    },

    /**
     * Request existing bingo set from the server using persistent client ID
     */
    async requestExistingBingoSet() {
      try {
        this.isLoading = true
        this.error = null
        await signalRService.requestExistingBingoSet(this.persistentClientId)
      } catch (error) {
        console.error('Failed to request existing bingo set:', error)
        this.error = `Failed to get bingo board: ${error.message}`
        this.isLoading = false
      }
    },

    /**
     * Convert server bingo set to local board format
     */
    convertServerBingoSetToBoard(serverBingoSet) {
      return serverBingoSet.squares.map(square => ({
        id: square.id || square.Id,
        label: square.label || square.Label,
        type: square.type || square.Type,
        marked: square.isChecked || square.IsChecked || false
      }));
    },

    /**
     * Toggle a square and request approval from admin or update directly in free play mode
     */
    async toggleSquare(index) {
      const square = this.currentBoard[index]
      if (square.type === 'free') {
        return // Free squares can't be toggled
      }

      // Don't allow toggling if already pending
      if (this.pendingSquares.has(square.id)) {
        console.log('Square is already pending approval:', square.id)
        return
      }

      try {
        const newMarkedState = !square.marked
        
        if (!this.isLiveMode) {
          // Free play mode - update immediately and locally
          square.marked = newMarkedState
          this.checkForBingo()
          this.saveState()
          
          console.log(`Free play mode: Updated square ${square.id} to ${newMarkedState}`)
          
          // Show a brief confirmation message
          this.showGlobalUpdateNotification(`${square.label} ${newMarkedState ? 'checked' : 'unchecked'} (Free Play Mode)`)
        } else {
          // Live mode - add to pending and request approval
          this.pendingSquares.add(square.id)
          
          // Request approval from admin
          await signalRService.requestSquareApproval(square.id, newMarkedState)
          
          console.log(`Requested approval for square ${square.id}: ${newMarkedState ? 'check' : 'uncheck'}`)
        }
        
      } catch (error) {
        console.error('Failed to request square approval:', error)
        // Remove from pending if request failed
        this.pendingSquares.delete(square.id)
        this.error = `Failed to request approval: ${error.message}`
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
    
    requestNewBoard() {
      // Clear the persistent client ID to get a completely fresh start
      clearPersistentClientId()
      this.persistentClientId = getPersistentClientId()
      console.log('Requesting new board with fresh persistent client ID:', this.persistentClientId)
      
      // Clear local storage for the board state
      localStorage.removeItem('aspirifridays-bingo')
      
      // Request a completely new bingo set from the server
      this.requestNewBingoSet()
    },
    
    saveState() {
      if (this.currentBingoSet && this.persistentClientId) {
        localStorage.setItem('aspirifridays-bingo', JSON.stringify({
          bingoSet: this.currentBingoSet,
          board: this.currentBoard,
          bingoLines: this.bingoLines,
          showInitialCelebration: this.showInitialCelebration,
          persistentClientId: this.persistentClientId,
          timestamp: Date.now()
        }))
      }
    },
    
    loadStateAsCache() {
      // Load saved state as immediate cache while waiting for server response
      const saved = localStorage.getItem('aspirifridays-bingo')
      if (saved) {
        try {
          const state = JSON.parse(saved)
          const age = Date.now() - (state.timestamp || 0)
          
          // Only load state if it's less than 24 hours old and matches current persistent client ID
          if (age < 24 * 60 * 60 * 1000 && state.bingoSet && state.board && 
              state.persistentClientId === this.persistentClientId) {
            this.currentBingoSet = state.bingoSet
            this.currentBoard = state.board
            this.bingoLines = state.bingoLines || []
            this.showInitialCelebration = state.showInitialCelebration !== undefined ? 
              state.showInitialCelebration : (this.bingoLines.length === 0)
            console.log('Loaded cached bingo board from localStorage (waiting for server update)')
            return true
          }
        } catch (error) {
          console.error('Failed to load cached state:', error)
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
    },

    // SignalR event handlers
    onConnectionStateChanged(state) {
      this.connectionState = state
      // Don't automatically request board on reconnection since we handle it in initialization
      // This prevents duplicate requests
    },

    onBingoSetReceived(bingoSet) {
      console.log('Received new bingo set:', bingoSet)
      this.currentBingoSet = bingoSet
      this.currentBoard = this.convertServerBingoSetToBoard(bingoSet)
      this.bingoLines = []
      this.showInitialCelebration = true
      this.isLoading = false
      this.error = null
      this.checkForBingo()
      this.saveState()
    },

    onExistingBingoSetReceived(bingoSet) {
      console.log('Received existing bingo set from server:', bingoSet)
      this.currentBingoSet = bingoSet
      this.currentBoard = this.convertServerBingoSetToBoard(bingoSet)
      this.isLoading = false
      this.error = null
      this.checkForBingo()
      
      // For existing sets, don't automatically show celebration since user might have seen it before
      // Let the bingo check determine if celebration should be shown based on current win state
      if (this.bingoLines.length > 0) {
        this.showInitialCelebration = false // Don't show initial celebration for existing wins
      }
      
      // Save the server state to localStorage for next time
      this.saveState()
    },

    onSquareUpdated(update) {
      // Admin updated a square - find and update it
      const squareIndex = this.currentBoard.findIndex(square => square.id === update.squareId)
      if (squareIndex !== -1) {
        this.currentBoard[squareIndex].marked = update.isChecked
        this.checkForBingo()
        this.saveState()
      }
    },

    onSquareUpdateConfirmed(confirmation) {
      // Our square update was confirmed by the server (used in free play mode)
      const squareIndex = this.currentBoard.findIndex(square => square.id === confirmation.squareId)
      if (squareIndex !== -1) {
        // Ensure our local state matches the server
        this.currentBoard[squareIndex].marked = confirmation.isChecked
        this.checkForBingo()
        this.saveState()
        
        // In free play mode, show a subtle confirmation
        if (!this.isLiveMode && confirmation.message) {
          console.log('Free play confirmation:', confirmation.message)
        }
      }
    },

    onBingoAchieved(data) {
      // Someone achieved bingo - could be us or another player
      console.log('Bingo achieved!', data)
      // We'll rely on our local bingo checking for our own celebration
    },

    onGlobalSquareUpdate(update) {
      console.log('[Client] Global square update received:', update)
      
      // Show notification to user
      if (update.message) {
        console.log('[Client] Showing notification:', update.message)
        this.showGlobalUpdateNotification(update.message)
      }
      
      // Find and update the square if it exists in our board
      const squareIndex = this.currentBoard.findIndex(square => square.id === update.squareId)
      if (squareIndex !== -1) {
        console.log(`[Client] Updating square ${update.squareId} at index ${squareIndex} from ${this.currentBoard[squareIndex].marked} to ${update.isChecked}`)
        this.currentBoard[squareIndex].marked = update.isChecked
        this.checkForBingo()
        this.saveState()
      } else {
        console.log(`[Client] Square ${update.squareId} not found in current board`)
      }
    },

    showGlobalUpdateNotification(message) {
      // Create a temporary notification element
      const notification = document.createElement('div')
      notification.className = 'global-update-notification'
      notification.innerHTML = `
        <i class="bi bi-megaphone"></i>
        <span>${message}</span>
      `
      
      // Add to document
      document.body.appendChild(notification)
      
      // Remove after 5 seconds
      setTimeout(() => {
        if (notification.parentNode) {
          notification.parentNode.removeChild(notification)
        }
      }, 5000)
    },

    onSignalRError(error) {
      console.error('SignalR error:', error)
      this.error = `Server error: ${error}`
    },

    // Approval workflow event handlers
    onApprovalRequestSubmitted(response) {
      console.log('Approval request submitted:', response)
      // Square is now pending - UI should show pending state
      // The pending state is already handled by adding to pendingSquares in toggleSquare
      // Only relevant in live mode
      if (this.isLiveMode) {
        console.log('Approval request submitted in live mode')
      }
    },

    onApprovalRequestApproved(response) {
      console.log('Approval request approved:', response)
      
      // Remove from pending squares
      this.pendingSquares.delete(response.squareId)
      
      // Update the square's marked state
      const squareIndex = this.currentBoard.findIndex(square => square.id === response.squareId)
      if (squareIndex !== -1) {
        this.currentBoard[squareIndex].marked = response.newState
        this.checkForBingo()
        this.saveState()
        console.log(`Square ${response.squareId} approved and marked as ${response.newState}`)
      }
      
      // Show success notification
      this.showGlobalUpdateNotification(`Your request to ${response.newState ? 'check' : 'uncheck'} "${response.squareLabel}" was approved!`)
    },

    onApprovalRequestDenied(response) {
      console.log('Approval request denied:', response)
      
      // Remove from pending squares
      this.pendingSquares.delete(response.squareId)
      
      // Show denial notification with reason if provided
      let message = `Your request to ${response.requestedState ? 'check' : 'uncheck'} "${response.squareLabel}" was denied.`
      if (response.reason) {
        message += ` Reason: ${response.reason}`
      }
      this.showGlobalUpdateNotification(message)
    },

    // Live mode event handler
    onLiveModeChanged(update) {
      console.log('Live mode changed:', update)
      
      this.isLiveMode = update.isLiveMode
      this.liveModeMessage = update.message
      
      // Clear pending squares when switching to free play mode
      if (!this.isLiveMode) {
        this.pendingSquares.clear()
      }
      
      // Show the mode change notification
      this.showGlobalUpdateNotification(update.message)
      
      console.log(`Mode changed to: ${this.isLiveMode ? 'Live Stream' : 'Free Play'}`)
    }
  },
  
  async mounted() {
    await this.initializeSignalR()
  },

  async beforeUnmount() {
    // Clean up SignalR event listeners
    signalRService.removeEventListener('connectionStateChanged', this.onConnectionStateChanged)
    signalRService.removeEventListener('bingoSetReceived', this.onBingoSetReceived)
    signalRService.removeEventListener('existingBingoSetReceived', this.onExistingBingoSetReceived)
    signalRService.removeEventListener('squareUpdated', this.onSquareUpdated)
    signalRService.removeEventListener('squareUpdateConfirmed', this.onSquareUpdateConfirmed)
    signalRService.removeEventListener('bingoAchieved', this.onBingoAchieved)
    signalRService.removeEventListener('globalSquareUpdate', this.onGlobalSquareUpdate)
    signalRService.removeEventListener('error', this.onSignalRError)
    
    // Clean up approval workflow event listeners
    signalRService.removeEventListener('approvalRequestSubmitted', this.onApprovalRequestSubmitted)
    signalRService.removeEventListener('approvalRequestApproved', this.onApprovalRequestApproved)
    signalRService.removeEventListener('approvalRequestDenied', this.onApprovalRequestDenied)
    
    // Clean up live mode event listener
    signalRService.removeEventListener('liveModeChanged', this.onLiveModeChanged)
    
    // Don't disconnect SignalR as other components might be using it
  }
}
</script>

<style scoped>
/* Connection status styles */
.connection-status {
  position: fixed;
  top: 1rem;
  right: 1rem;
  z-index: 1000;
  max-width: 300px;
}

.status-message {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  font-weight: 500;
  margin-bottom: 0.5rem;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
}

.status-message.loading {
  background-color: #e0f2fe;
  color: #0277bd;
  border: 1px solid #81d4fa;
}

.status-message.reconnecting {
  background-color: #fff3e0;
  color: #ef6c00;
  border: 1px solid #ffcc02;
}

.status-message.disconnected {
  background-color: #ffebee;
  color: #c62828;
  border: 1px solid #ef5350;
}

.status-message.error {
  background-color: #ffebee;
  color: #c62828;
  border: 1px solid #ef5350;
}

.spinning {
  animation: spin 1s linear infinite;
}

@keyframes spin {
  from { transform: rotate(0deg); }
  to { transform: rotate(360deg); }
}

.bingo-board.disabled {
  opacity: 0.6;
  pointer-events: none;
}

button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Global notification styles */
.global-update-notification {
  position: fixed;
  top: 1rem;
  right: 1rem;
  z-index: 1000;
  background-color: #fff3cd;
  color: #856404;
  padding: 0.75rem 1.25rem;
  border: 1px solid #ffeeba;
  border-radius: 0.5rem;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 0.5rem;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
  animation: slideIn 0.3s ease-out, slideOut 0.3s ease-in 4.7s;
}

@keyframes slideIn {
  from {
    transform: translateX(100%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

@keyframes slideOut {
  from {
    transform: translateX(0);
    opacity: 1;
  }
  to {
    transform: translateX(100%);
    opacity: 0;
  }
}

/* Mode indicator styles */
.mode-indicator {
  background: var(--glass-bg);
  backdrop-filter: var(--glass-blur);
  border: var(--glass-border);
  border-radius: 1rem;
  padding: 1rem;
  margin-bottom: 1.5rem;
  text-align: center;
}

.mode-indicator.live-mode {
  border-color: #dc3545;
  background: rgba(220, 53, 69, 0.1);
}

.mode-indicator.free-play-mode {
  border-color: #198754;
  background: rgba(25, 135, 84, 0.1);
}

.mode-header {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  margin-bottom: 0.5rem;
}

.mode-title {
  font-weight: 600;
  font-size: 0.9rem;
}

.live-mode .mode-title {
  color: #dc3545;
}

.free-play-mode .mode-title {
  color: #198754;
}

.mode-description {
  font-size: 0.8rem;
  color: var(--text-color);
  opacity: 0.8;
  margin: 0;
  line-height: 1.3;
}

.live-mode .bi-broadcast-pin {
  color: #dc3545;
  animation: pulse 2s infinite;
}

.free-play-mode .bi-play-circle {
  color: #198754;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: 0.6;
  }
}
</style>