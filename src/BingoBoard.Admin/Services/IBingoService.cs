using BingoBoard.Admin.Models;

namespace BingoBoard.Admin.Services
{
    /// <summary>
    /// Service for managing bingo squares and sets
    /// </summary>
    public interface IBingoService
    {
        /// <summary>
        /// Get all available bingo squares
        /// </summary>
        Task<List<BingoSquare>> GetAllSquaresAsync();

        /// <summary>
        /// Get a random set of 25 bingo squares for a new client
        /// </summary>
        Task<BingoSet> GenerateRandomBingoSetAsync(string clientId);

        /// <summary>
        /// Update the checked status of a bingo square for a specific client
        /// </summary>
        Task<bool> UpdateSquareStatusAsync(string clientId, string squareId, bool isChecked);

        /// <summary>
        /// Get the current bingo set for a client
        /// </summary>
        Task<BingoSet?> GetClientBingoSetAsync(string clientId);

        /// <summary>
        /// Check if a bingo set has any winning conditions
        /// </summary>
        Task<bool> CheckForWinAsync(string clientId);

        /// <summary>
        /// Get all client bingo sets for admin overview
        /// </summary>
        Task<List<BingoSet>> GetAllClientSetsAsync();

        /// <summary>
        /// Update a square globally for all clients that have it on their board
        /// </summary>
        Task<bool> UpdateSquareGloballyAsync(string squareId, bool isChecked);

        /// <summary>
        /// Update a square for admin only without broadcasting to other clients
        /// </summary>
        Task<bool> UpdateSquareForAdminAsync(string adminClientId, string squareId, bool isChecked);

        /// <summary>
        /// Get globally checked squares
        /// </summary>
        Task<List<string>> GetGloballyCheckedSquaresAsync();

        /// <summary>
        /// Client requests approval to mark a square
        /// </summary>
        Task<string> RequestSquareApprovalAsync(string clientId, string squareId, bool requestedState);

        /// <summary>
        /// Get all pending approval requests
        /// </summary>
        Task<List<PendingApproval>> GetPendingApprovalsAsync();

        /// <summary>
        /// Admin approves a square marking request
        /// </summary>
        Task<bool> ApproveSquareRequestAsync(string approvalId, string adminId);

        /// <summary>
        /// Admin denies a square marking request
        /// </summary>
        Task<bool> DenySquareRequestAsync(string approvalId, string adminId, string? reason = null);

        /// <summary>
        /// Get a specific pending approval by ID
        /// </summary>
        Task<PendingApproval?> GetPendingApprovalAsync(string approvalId);

        /// <summary>
        /// Clean up expired approval requests
        /// </summary>
        Task CleanupExpiredApprovalsAsync();

        /// <summary>
        /// Update the connection ID for an existing client's bingo set
        /// </summary>
        Task<bool> UpdateClientConnectionAsync(string oldClientId, string newClientId);

        /// <summary>
        /// Set the live mode state (true for live stream, false for free play)
        /// </summary>
        Task SetLiveModeAsync(bool isLiveMode);

        /// <summary>
        /// Get the current live mode state
        /// </summary>
        Task<bool> GetLiveModeAsync();

        /// <summary>
        /// Handle square approval request - bypasses approval in free play mode
        /// </summary>
        Task<(bool needsApproval, string? approvalId)> HandleSquareRequestAsync(string clientId, string squareId, bool requestedState);
    }
}
