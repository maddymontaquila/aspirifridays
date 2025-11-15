using BingoBoard.Admin.Models;
using BingoBoard.Admin.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BingoBoard.Admin.Services;

/// <summary>
/// Implementation of client connection service using Redis for persistence
/// </summary>
public class ClientConnectionService(IDistributedCache cache, ILogger<ClientConnectionService> logger) : IClientConnectionService
{
    private const string ClientsKey = "connected_clients";
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task AddClientAsync(ConnectedClient client)
        {
            await _semaphore.WaitAsync();
            try
            {
                var clients = await GetAllClientsAsync();
                
                // Remove any existing client with the same connection ID
                clients.RemoveAll(c => c.ConnectionId == client.ConnectionId);
                
                // Add the new client
                clients.Add(client);
                
                await SaveClientsAsync(clients);
                
                logger.LogInformation("Added client {ConnectionId} at {ConnectedAt}", 
                    client.ConnectionId, client.ConnectedAt);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding client {ConnectionId}", client.ConnectionId);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task RemoveClientAsync(string connectionId)
        {
            await _semaphore.WaitAsync();
            try
            {
                // First, get the persistent client ID to clean up reverse mapping
                var persistentClientId = await GetPersistentClientIdAsync(connectionId);
                
                var clients = await GetAllClientsAsync();
                var removed = clients.RemoveAll(c => c.ConnectionId == connectionId);
                
                if (removed > 0)
                {
                    await SaveClientsAsync(clients);
                    
                    // Clean up the bidirectional mapping
                    var connectionToPersistentKey = $"connection_to_persistent_{connectionId}";
                    await cache.RemoveAsync(connectionToPersistentKey);
                    
                    if (!string.IsNullOrEmpty(persistentClientId))
                    {
                        var persistentToConnectionKey = $"persistent_to_connection_{persistentClientId}";
                        await cache.RemoveAsync(persistentToConnectionKey);
                    }
                    
                    logger.LogInformation("Removed client {ConnectionId} and cleaned up mappings", connectionId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing client {ConnectionId}", connectionId);
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task<List<ConnectedClient>> GetAllClientsAsync()
        {
            try
            {
                var serializedClients = await cache.GetStringAsync(ClientsKey);
                
                if (string.IsNullOrEmpty(serializedClients))
                    return [];

                var clients = JsonSerializer.Deserialize<List<ConnectedClient>>(serializedClients);
                return clients ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving all clients");
                return [];
            }
        }

        public async Task<ConnectedClient?> GetClientAsync(string connectionId)
        {
            try
            {
                var clients = await GetAllClientsAsync();
                return clients.FirstOrDefault(c => c.ConnectionId == connectionId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving client {ConnectionId}", connectionId);
                return null;
            }
        }

        public async Task UpdateClientActivityAsync(string connectionId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var clients = await GetAllClientsAsync();
                var client = clients.FirstOrDefault(c => c.ConnectionId == connectionId);
                
                if (client != null)
                {
                    client.LastActivity = DateTime.UtcNow;
                    await SaveClientsAsync(clients);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error updating activity for client {ConnectionId}", connectionId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task AssociateBingoSetAsync(string connectionId, string bingoSetId)
        {
            await _semaphore.WaitAsync();
            try
            {
                var clients = await GetAllClientsAsync();
                var client = clients.FirstOrDefault(c => c.ConnectionId == connectionId);
                
                if (client != null)
                {
                    client.CurrentBingoSetId = bingoSetId;
                    client.LastActivity = DateTime.UtcNow;
                    await SaveClientsAsync(clients);
                    
                    logger.LogInformation("Associated bingo set {BingoSetId} with client {ConnectionId}", 
                        bingoSetId, connectionId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error associating bingo set with client {ConnectionId}", connectionId);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task MapConnectionToPersistentClientAsync(string connectionId, string persistentClientId)
        {
            try
            {
                // Store the mapping in both directions for efficient lookup
                var connectionToPersistentKey = $"connection_to_persistent_{connectionId}";
                var persistentToConnectionKey = $"persistent_to_connection_{persistentClientId}";
                
                await cache.SetStringAsync(connectionToPersistentKey, persistentClientId, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(24)
                });
                
                await cache.SetStringAsync(persistentToConnectionKey, connectionId, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(24)
                });

                logger.LogInformation("Mapped connection {ConnectionId} to persistent client {PersistentClientId}", 
                    connectionId, persistentClientId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error mapping connection {ConnectionId} to persistent client {PersistentClientId}", 
                    connectionId, persistentClientId);
            }
        }

        public async Task<string?> GetPersistentClientIdAsync(string connectionId)
        {
            try
            {
                var mappingKey = $"connection_to_persistent_{connectionId}";
                return await cache.GetStringAsync(mappingKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting persistent client ID for connection {ConnectionId}", connectionId);
                return null;
            }
        }

        public async Task<string?> GetConnectionIdFromPersistentClientAsync(string persistentClientId)
        {
            try
            {
                // Use the direct reverse mapping for efficiency
                var persistentToConnectionKey = $"persistent_to_connection_{persistentClientId}";
                return await cache.GetStringAsync(persistentToConnectionKey);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting connection ID for persistent client {PersistentClientId}", persistentClientId);
                return null;
            }
        }

        private async Task SaveClientsAsync(List<ConnectedClient> clients)
        {
            var serializedClients = JsonSerializer.Serialize(clients);
            await cache.SetStringAsync(ClientsKey, serializedClients, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24)
            });
        }
    }
