/**
 * Utility functions for managing persistent client IDs
 */

const CLIENT_ID_KEY = 'aspirifridays-bingo-client-id'

/**
 * Generate a new unique client ID
 * @returns {string} A new UUID-style client ID
 */
export function generateClientId() {
  return 'client_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9)
}

/**
 * Get or create a persistent client ID that survives page refreshes
 * @returns {string} The persistent client ID
 */
export function getPersistentClientId() {
  let clientId = localStorage.getItem(CLIENT_ID_KEY)
  
  if (!clientId) {
    clientId = generateClientId()
    localStorage.setItem(CLIENT_ID_KEY, clientId)
  }
  
  return clientId
}

/**
 * Clear the persistent client ID (useful for starting fresh)
 */
export function clearPersistentClientId() {
  localStorage.removeItem(CLIENT_ID_KEY)
}

/**
 * Get the current persistent client ID without creating a new one
 * @returns {string|null} The current client ID or null if none exists
 */
export function getCurrentClientId() {
  return localStorage.getItem(CLIENT_ID_KEY)
}
