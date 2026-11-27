<template>
  <div class="bingo-container">
    <div v-if="!isConnected || isReconnecting || isLoading || error" class="connection-status" role="status" aria-live="polite">
      <div v-if="isLoading" class="status-message">
        <i class="bi bi-arrow-repeat spinning" aria-hidden="true"></i>
        <span>Connecting…</span>
      </div>
      <div v-else-if="!isConnected && isReconnecting" class="status-message">
        <i class="bi bi-arrow-repeat spinning" aria-hidden="true"></i>
        <span>Reconnecting…</span>
      </div>
      <div v-else-if="!isConnected" class="status-message status-message--error">
        <i class="bi bi-wifi-off" aria-hidden="true"></i>
        <span>Connection lost. Refresh the page to rejoin.</span>
      </div>
      <div v-if="error" class="status-message status-message--error">
        <i class="bi bi-exclamation-circle" aria-hidden="true"></i>
        <span>{{ error }}</span>
      </div>
    </div>

    <div class="game-area">
      <section class="board-panel" aria-label="Your bingo card">
        <div class="board-letters" aria-hidden="true">
          <span>B</span><span>I</span><span>N</span><span>G</span><span>O</span>
        </div>

        <div class="bingo-board"
             ref="board"
             :class="{ 'disabled': !isConnected || isLoading, 'has-bingo': hasBingo }"
             role="grid"
             aria-label="Bingo board, 5 by 5 grid of AspiriFridays moments"
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

          <template v-if="!currentBoard.length">
            <div v-for="index in 25"
                 :key="`loading-${index}`"
                 class="bingo-square bingo-square--skeleton"
                 aria-hidden="true">
              <span></span>
            </div>
          </template>
        </div>
      </section>

      <aside class="sidebar" aria-label="Game controls">
        <div class="status-card" :class="isLiveMode ? 'is-live' : 'is-free'">
          <div class="status-card__head">
            <span class="status-pill">
              <span class="status-pill__dot" aria-hidden="true"></span>
              {{ isLiveMode ? 'Live now' : 'Free play' }}
            </span>
            <span v-if="currentBoard.length" class="mark-count">
              <strong>{{ markedCount }}</strong> of {{ markableCount }} marked
            </span>
          </div>
          <p class="status-card__text">
            {{ isLiveMode
              ? 'The host confirms each square you mark.'
              : 'No stream right now, so mark squares whenever you like.'
            }}
          </p>

          <button v-if="isLiveMode"
                  type="button"
                  @click="requestCatchUp"
                  :disabled="!isConnected || isCatchingUp"
                  class="btn btn--ghost btn--block"
                  aria-label="Sync your board with squares the host has already confirmed">
            <i :class="isCatchingUp ? 'bi bi-arrow-repeat spinning' : 'bi bi-arrow-repeat'" aria-hidden="true"></i>
            <span>{{ isCatchingUp ? 'Syncing…' : 'Sync with the stream' }}</span>
          </button>
        </div>

        <BingoCelebrationArea :has-bingo="hasBingo && !showInitialCelebration" />

        <div class="controls">
          <button type="button"
                  @click="requestNewBoard"
                  :disabled="!isConnected || isLoading"
                  class="btn btn--primary">
            <i class="bi bi-shuffle" aria-hidden="true"></i>
            <span>New board</span>
          </button>

          <button type="button"
                  @click="downloadImage"
                  :disabled="!currentBoard.length"
                  class="btn btn--secondary">
            <i class="bi bi-download" aria-hidden="true"></i>
            <span>Save image</span>
          </button>
        </div>
      </aside>
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

export default {
  name: 'BingoBoard',
  components: {
    BingoSquare,
    BingoCelebrationOverlay,
    BingoCelebrationArea
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
      isLiveMode: false, // Default to free play mode, will be updated from server
      liveModeMessage: '',
      isCatchingUp: false // Track catch-up request state
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
    },
    markableCount() {
      return this.currentBoard.filter(square => square.type !== 'free').length
    },
    markedCount() {
      return this.currentBoard.filter(square => square.type !== 'free' && square.marked).length
    },
    boardLabels() {
      return this.currentBoard.map(square => square.label).join('|')
    }
  },
  watch: {
    boardLabels() {
      this.$nextTick(() => this.scheduleTextFit())
    }
  },
  methods: {
    scheduleTextFit() {
      cancelAnimationFrame(this.textFitFrame)
      this.textFitFrame = requestAnimationFrame(() => this.fitSquareText())
    },

    // Hyphenate only the tiles where a whole word would overflow; elsewhere
    // soft hyphens stay dormant so balanced wrapping doesn't split words needlessly.
    fitSquareText() {
      const board = this.$refs.board
      if (!board) return

      const labels = [...board.querySelectorAll('.square-text')]
      board.classList.add('is-measuring')
      const overflowing = labels.map(label => label.scrollWidth > label.clientWidth + 1)
      board.classList.remove('is-measuring')
      labels.forEach((label, index) => label.classList.toggle('needs-hyphens', overflowing[index]))
    },

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
        
        // Stream session event listeners
        signalRService.addEventListener('streamSessionStarted', this.onStreamSessionStarted)
        signalRService.addEventListener('streamSessionEnded', this.onStreamSessionEnded)
        signalRService.addEventListener('boardReset', this.onBoardReset)
        signalRService.addEventListener('catchUpCompleted', this.onCatchUpCompleted)

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
    },

    // Stream session event handlers
    onStreamSessionStarted(update) {
      console.log('Stream session started:', update)
      this.isLiveMode = true
      this.pendingSquares.clear()
      this.showGlobalUpdateNotification(update.message || 'Stream session started!')
    },

    onStreamSessionEnded(update) {
      console.log('Stream session ended:', update)
      this.isLiveMode = false
      this.pendingSquares.clear()
      this.showGlobalUpdateNotification(update.message || 'Stream session ended!')
    },

    onBoardReset(update) {
      console.log('Board reset:', update)
      
      // Reset all marked squares (except free space)
      this.currentBoard.forEach(square => {
        if (square.type !== 'free') {
          square.marked = false
        }
      })
      
      // Clear pending squares
      this.pendingSquares.clear()
      
      // Reset bingo state
      this.bingoLines = []
      this.showInitialCelebration = true
      
      // Save the reset state
      this.saveState()
      
      this.showGlobalUpdateNotification(update.message || 'Board has been reset')
    },

    // Catch-up functionality
    async requestCatchUp() {
      if (!this.isConnected || this.isCatchingUp) {
        return
      }

      try {
        this.isCatchingUp = true
        await signalRService.requestCatchUp()
        console.log('Catch-up request sent')
      } catch (error) {
        console.error('Failed to request catch-up:', error)
        this.error = `Failed to catch up: ${error.message}`
        this.isCatchingUp = false
      }
    },

    onCatchUpCompleted(response) {
      console.log('Catch-up completed:', response)
      this.isCatchingUp = false
      
      // Update the bingo set with caught-up squares (SignalR sends PascalCase)
      const bingoSet = response.BingoSet || response.bingoSet
      if (bingoSet) {
        this.currentBingoSet = bingoSet
        this.currentBoard = this.convertServerBingoSetToBoard(bingoSet)
        this.checkForBingo()
        this.saveState()
      }
      
      // Show success notification
      const count = response.UpdatedSquaresCount || response.updatedSquaresCount || 0
      const message = response.Message || response.message || `Synced ${count} squares!`
      this.showGlobalUpdateNotification(message)
    }
  },
  
  async mounted() {
    this.boardResizeObserver = new ResizeObserver(() => this.scheduleTextFit())
    this.boardResizeObserver.observe(this.$refs.board)
    document.fonts?.ready.then(() => this.scheduleTextFit())
    await this.initializeSignalR()
  },

  async beforeUnmount() {
    this.boardResizeObserver?.disconnect()
    cancelAnimationFrame(this.textFitFrame)

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
    
    // Clean up stream session event listeners
    signalRService.removeEventListener('streamSessionStarted', this.onStreamSessionStarted)
    signalRService.removeEventListener('streamSessionEnded', this.onStreamSessionEnded)
    signalRService.removeEventListener('boardReset', this.onBoardReset)
    signalRService.removeEventListener('catchUpCompleted', this.onCatchUpCompleted)
    
    // Don't disconnect SignalR as other components might be using it
  }
}
</script>
