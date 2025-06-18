using BingoBoard.Admin.Models;
using BingoBoard.Admin.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BingoBoard.Admin.Services
{
    /// <summary>
    /// Implementation of client connection service using Redis for persistence
    /// </summary>
    public class ClientConnectionService : IClientConnectionService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<ClientConnectionService> _logger;
        private const string ClientsKey = "connected_clients";

        public ClientConnectionService(IDistributedCache cache, ILogger<ClientConnectionService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        public async Task AddClientAsync(ConnectedClient client)
        {
            try
            {
                var clients = await GetAllClientsAsync();
                
                // Remove any existing client with the same connection ID
                clients.RemoveAll(c => c.ConnectionId == client.ConnectionId);
                
                // Add the new client
                clients.Add(client);
                
                await SaveClientsAsync(clients);
                
                _logger.LogInformation("Added client {ConnectionId} at {ConnectedAt}", 
                    client.ConnectionId, client.ConnectedAt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding client {ConnectionId}", client.ConnectionId);
                throw;
            }
        }

        public async Task RemoveClientAsync(string connectionId)
        {
            try
            {
                var clients = await GetAllClientsAsync();
                var removed = clients.RemoveAll(c => c.ConnectionId == connectionId);
                
                if (removed > 0)
                {
                    await SaveClientsAsync(clients);
                    _logger.LogInformation("Removed client {ConnectionId}", connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing client {ConnectionId}", connectionId);
                throw;
            }
        }

        public async Task<List<ConnectedClient>> GetAllClientsAsync()
        {
            try
            {
                var serializedClients = await _cache.GetStringAsync(ClientsKey);
                
                if (string.IsNullOrEmpty(serializedClients))
                    return new List<ConnectedClient>();

                var clients = JsonSerializer.Deserialize<List<ConnectedClient>>(serializedClients);
                return clients ?? new List<ConnectedClient>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all clients");
                return new List<ConnectedClient>();
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
                _logger.LogError(ex, "Error retrieving client {ConnectionId}", connectionId);
                return null;
            }
        }

        public async Task UpdateClientActivityAsync(string connectionId)
        {
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
                _logger.LogError(ex, "Error updating activity for client {ConnectionId}", connectionId);
            }
        }

        public async Task AssociateBingoSetAsync(string connectionId, string bingoSetId)
        {
            try
            {
                var clients = await GetAllClientsAsync();
                var client = clients.FirstOrDefault(c => c.ConnectionId == connectionId);
                
                if (client != null)
                {
                    client.CurrentBingoSetId = bingoSetId;
                    client.LastActivity = DateTime.UtcNow;
                    await SaveClientsAsync(clients);
                    
                    _logger.LogInformation("Associated bingo set {BingoSetId} with client {ConnectionId}", 
                        bingoSetId, connectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error associating bingo set with client {ConnectionId}", connectionId);
            }
        }

        private async Task SaveClientsAsync(List<ConnectedClient> clients)
        {
            var serializedClients = JsonSerializer.Serialize(clients);
            await _cache.SetStringAsync(ClientsKey, serializedClients, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24)
            });
        }
    }
}
