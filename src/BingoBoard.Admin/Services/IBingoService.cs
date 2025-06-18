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
        /// Get globally checked squares
        /// </summary>
        Task<List<string>> GetGloballyCheckedSquaresAsync();
    }
}
