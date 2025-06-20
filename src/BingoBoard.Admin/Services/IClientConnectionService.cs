using BingoBoard.Admin.Models;

namespace BingoBoard.Admin.Services
{
    /// <summary>
    /// Service for managing connected clients
    /// </summary>
    public interface IClientConnectionService
    {
        /// <summary>
        /// Add a new connected client
        /// </summary>
        Task AddClientAsync(ConnectedClient client);

        /// <summary>
        /// Remove a disconnected client
        /// </summary>
        Task RemoveClientAsync(string connectionId);

        /// <summary>
        /// Get all currently connected clients
        /// </summary>
        Task<List<ConnectedClient>> GetAllClientsAsync();

        /// <summary>
        /// Get a specific client by connection ID
        /// </summary>
        Task<ConnectedClient?> GetClientAsync(string connectionId);

        /// <summary>
        /// Update client's last activity timestamp
        /// </summary>
        Task UpdateClientActivityAsync(string connectionId);

        /// <summary>
        /// Associate a bingo set with a client
        /// </summary>
        Task AssociateBingoSetAsync(string connectionId, string bingoSetId);
    }
}
