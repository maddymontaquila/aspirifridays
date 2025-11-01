import { HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr'

/**
 * SignalR service for connecting to the bingo admin hub
 */
export class SignalRService {
  constructor() {
    this.connection = null
    this.isConnected = false
    this.listeners = new Map()
  }

  /**
   * Wait for configuration from MAUI (if running in HybridWebView)
   */
  async waitForConfiguration(timeout = 5000) {
    // If we already have config, return immediately
    if (window.BACKEND_CONFIG?.adminUrl) {
      return true;
    }

    // Check if we're in a HybridWebView by looking for the HybridWebView object
    if (typeof window.HybridWebView === 'undefined') {
      // Not in HybridWebView, proceed without config
      return false;
    }
    
    // Wait for configuration with timeout
    const startTime = Date.now();
    while (Date.now() - startTime < timeout) {
      if (window.BACKEND_CONFIG?.adminUrl) {
        return true;
      }
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    
    return false;
  }

  /**
   * Initialize connection to the SignalR hub
   */
  async connect() {
    try {
      // Wait for MAUI configuration if in HybridWebView
      await this.waitForConfiguration();

      // Get the backend URL from the injected configuration (for MAUI)
      // or fallback to relative URL (for web)
      let hubUrl = 'bingohub'; // Default relative URL for web

      if (typeof window !== 'undefined' && window.BACKEND_CONFIG && window.BACKEND_CONFIG.adminUrl) {
        const baseUrl = window.BACKEND_CONFIG.adminUrl;
        hubUrl = `${baseUrl}/bingohub`;
      }

      this.connection = new HubConnectionBuilder()
        .withUrl(hubUrl, {
          skipNegotiation: true,
          transport: HttpTransportType.WebSockets
        })
        .withAutomaticReconnect()
        .configureLogging(LogLevel.Information)
        .build()

      // Set up event handlers
      this.connection.onreconnecting(() => {
        console.log('SignalR connection lost, attempting to reconnect...')
        this.isConnected = false
        this.notifyListeners('connectionStateChanged', { connected: false, reconnecting: true })
      })

      this.connection.onreconnected(() => {
        console.log('SignalR connection reestablished')
        this.isConnected = true
        this.notifyListeners('connectionStateChanged', { connected: true, reconnecting: false })
      })

      this.connection.onclose(() => {
        console.log('SignalR connection closed')
        this.isConnected = false
        this.notifyListeners('connectionStateChanged', { connected: false, reconnecting: false })
      })

      // Set up server event handlers
      this.connection.on('BingoSetReceived', (bingoSet) => {
        console.log('Received bingo set:', bingoSet)
        this.notifyListeners('bingoSetReceived', bingoSet)
      })

      this.connection.on('ExistingBingoSetReceived', (bingoSet) => {
        console.log('Received existing bingo set:', bingoSet)
        this.notifyListeners('existingBingoSetReceived', bingoSet)
      })

      this.connection.on('SquareUpdated', (update) => {
        console.log('Square updated by admin:', update)
        this.notifyListeners('squareUpdated', update)
      })

      this.connection.on('SquareUpdateConfirmed', (confirmation) => {
        console.log('Square update confirmed:', confirmation)
        this.notifyListeners('squareUpdateConfirmed', confirmation)
      })

      // New approval workflow events
      this.connection.on('ApprovalRequestSubmitted', (response) => {
        console.log('Approval request submitted:', response)
        this.notifyListeners('approvalRequestSubmitted', response)
      })

      this.connection.on('ApprovalRequestApproved', (response) => {
        console.log('Approval request approved:', response)
        this.notifyListeners('approvalRequestApproved', response)
      })

      this.connection.on('ApprovalRequestDenied', (response) => {
        console.log('Approval request denied:', response)
        this.notifyListeners('approvalRequestDenied', response)
      })

      this.connection.on('NewApprovalRequest', (request) => {
        console.log('New approval request (for admins):', request)
        this.notifyListeners('newApprovalRequest', request)
      })

      this.connection.on('ApprovalRequestProcessed', (response) => {
        console.log('Approval request processed (for admins):', response)
        this.notifyListeners('approvalRequestProcessed', response)
      })

      this.connection.on('PendingApprovalsList', (approvals) => {
        console.log('Pending approvals list:', approvals)
        this.notifyListeners('pendingApprovalsList', approvals)
      })

      this.connection.on('BingoAchieved', (data) => {
        console.log('Bingo achieved:', data)
        this.notifyListeners('bingoAchieved', data)
      })

      this.connection.on('GlobalSquareUpdate', (update) => {
        console.log('Global square update received:', update)
        this.notifyListeners('globalSquareUpdate', update)
      })

      this.connection.on('LiveModeChanged', (update) => {
        console.log('Live mode changed:', update)
        this.notifyListeners('liveModeChanged', update)
      })

      this.connection.on('Error', (error) => {
        console.error('SignalR error:', error)
        this.notifyListeners('error', error)
      })

      // Start the connection
      await this.connection.start()
      this.isConnected = true
      console.log('SignalR connection established')
      this.notifyListeners('connectionStateChanged', { connected: true, reconnecting: false })

    } catch (error) {
      console.error('Failed to connect to SignalR hub:', error)
      this.isConnected = false
      this.notifyListeners('connectionStateChanged', { connected: false, reconnecting: false })
      throw error
    }
  }

  /**
   * Disconnect from the SignalR hub
   */
  async disconnect() {
    if (this.connection) {
      await this.connection.stop()
      this.connection = null
      this.isConnected = false
    }
  }

  /**
   * Request a new bingo set from the server
   */
  async requestBingoSet(userName = null) {
    if (!this.isConnected || !this.connection) {
      throw new Error('Not connected to SignalR hub')
    }
    
    try {
      await this.connection.invoke('RequestBingoSet', userName)
    } catch (error) {
      console.error('Failed to request bingo set:', error)
      throw error
    }
  }

  /**
   * Request existing bingo set using persistent client ID
   */
  async requestExistingBingoSet(persistentClientId, userName = null) {
    if (!this.isConnected || !this.connection) {
      throw new Error('Not connected to SignalR hub')
    }
    
    try {
      await this.connection.invoke('RequestExistingBingoSet', persistentClientId, userName)
    } catch (error) {
      console.error('Failed to request existing bingo set:', error)
      throw error
    }
  }

  /**
   * Request approval to mark a square (replaces direct square updates)
   */
  async requestSquareApproval(squareId, requestedState) {
    if (!this.isConnected || !this.connection) {
      throw new Error('Not connected to SignalR hub')
    }
    
    try {
      await this.connection.invoke('RequestSquareApproval', squareId, requestedState)
    } catch (error) {
      console.error('Failed to request square approval:', error)
      throw error
    }
  }

  /**
   * Admin: Approve a square marking request
   */
  async approveSquareRequest(approvalId) {
    if (!this.isConnected || !this.connection) {
      throw new Error('Not connected to SignalR hub')
    }
    
    try {
      await this.connection.invoke('ApproveSquareRequest', approvalId)
    } catch (error) {
      console.error('Failed to approve square request:', error)
      throw error
    }
  }

  /**
   * Admin: Deny a square marking request
   */
  async denySquareRequest(approvalId, reason = null) {
    if (!this.isConnected || !this.connection) {
      throw new Error('Not connected to SignalR hub')
    }
    
    try {
      await this.connection.invoke('DenySquareRequest', approvalId, reason)
    } catch (error) {
      console.error('Failed to deny square request:', error)
      throw error
    }
  }

  /**
   * Admin: Get pending approval requests
   */
  async getPendingApprovals() {
    if (!this.isConnected || !this.connection) {
      throw new Error('Not connected to SignalR hub')
    }
    
    try {
      await this.connection.invoke('GetPendingApprovals')
    } catch (error) {
      console.error('Failed to get pending approvals:', error)
      throw error
    }
  }

  /**
   * Update a square's status (deprecated - kept for backwards compatibility)
   * @deprecated Use requestSquareApproval instead
   */
  async updateSquare(squareId, isChecked) {
    console.warn('updateSquare is deprecated. Use requestSquareApproval instead.')
    return this.requestSquareApproval(squareId, isChecked)
  }

  /**
   * Add an event listener
   */
  addEventListener(event, callback) {
    if (!this.listeners.has(event)) {
      this.listeners.set(event, [])
    }
    this.listeners.get(event).push(callback)
  }

  /**
   * Remove an event listener
   */
  removeEventListener(event, callback) {
    if (this.listeners.has(event)) {
      const callbacks = this.listeners.get(event)
      const index = callbacks.indexOf(callback)
      if (index > -1) {
        callbacks.splice(index, 1)
      }
    }
  }

  /**
   * Notify all listeners of an event
   */
  notifyListeners(event, data) {
    if (this.listeners.has(event)) {
      this.listeners.get(event).forEach(callback => {
        try {
          callback(data)
        } catch (error) {
          console.error('Error in event listener:', error)
        }
      })
    }
  }

  /**
   * Get connection status
   */
  getConnectionState() {
    return {
      connected: this.isConnected,
      reconnecting: false
    }
  }
}

// Export a singleton instance
export const signalRService = new SignalRService()
