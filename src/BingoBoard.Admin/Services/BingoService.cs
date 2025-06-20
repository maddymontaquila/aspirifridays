using BingoBoard.Admin.Models;
using BingoBoard.Admin.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BingoBoard.Admin.Services
{
    /// <summary>
    /// Implementation of bingo service using Redis for persistence
    /// </summary>
    public class BingoService : IBingoService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<BingoService> _logger;
        private readonly IClientConnectionService _clientService;
        private static readonly List<BingoSquare> _allSquares = GenerateAllSquares();

        public BingoService(IDistributedCache cache, ILogger<BingoService> logger, IClientConnectionService clientService)
        {
            _cache = cache;
            _logger = logger;
            _clientService = clientService;
        }

        public async Task<List<BingoSquare>> GetAllSquaresAsync()
        {
            return await Task.FromResult(_allSquares.ToList());
        }

        public async Task<BingoSet> GenerateRandomBingoSetAsync(string clientId)
        {
            try
            {
                // Get a random selection of 24 squares plus the free space
                var random = new Random();
                var freeSpace = _allSquares.First(s => s.Id == "free");
                var availableSquares = _allSquares.Where(s => s.Id != "free").ToList();
                var selectedSquares = availableSquares.OrderBy(x => random.Next()).Take(24).ToList();
                
                // Add the free space in the center (position 12)
                selectedSquares.Insert(12, new BingoSquare 
                { 
                    Id = freeSpace.Id, 
                    Label = freeSpace.Label, 
                    Type = freeSpace.Type,
                    IsChecked = true // Free space is always checked
                });

                var bingoSet = new BingoSet
                {
                    ClientId = clientId,
                    Squares = selectedSquares
                };

                // Store in Redis cache
                var cacheKey = $"bingo_set_{clientId}";
                var serializedSet = JsonSerializer.Serialize(bingoSet);
                await _cache.SetStringAsync(cacheKey, serializedSet, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromHours(24) // Keep for 24 hours
                });

                _logger.LogInformation("Generated new bingo set for client {ClientId}", clientId);
                return bingoSet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating bingo set for client {ClientId}", clientId);
                throw;
            }
        }

        public async Task<bool> UpdateSquareStatusAsync(string clientId, string squareId, bool isChecked)
        {
            try
            {
                var bingoSet = await GetClientBingoSetAsync(clientId);
                if (bingoSet == null) return false;

                var square = bingoSet.Squares.FirstOrDefault(s => s.Id == squareId);
                if (square == null) return false;

                square.IsChecked = isChecked;
                square.LastUpdated = DateTime.UtcNow;
                bingoSet.LastUpdated = DateTime.UtcNow;

                // Update in cache
                var cacheKey = $"bingo_set_{clientId}";
                var serializedSet = JsonSerializer.Serialize(bingoSet);
                await _cache.SetStringAsync(cacheKey, serializedSet);

                _logger.LogInformation("Updated square {SquareId} to {Status} for client {ClientId}", 
                    squareId, isChecked, clientId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating square status for client {ClientId}", clientId);
                return false;
            }
        }

        public async Task<BingoSet?> GetClientBingoSetAsync(string clientId)
        {
            try
            {
                var cacheKey = $"bingo_set_{clientId}";
                var serializedSet = await _cache.GetStringAsync(cacheKey);
                
                if (string.IsNullOrEmpty(serializedSet))
                    return null;

                return JsonSerializer.Deserialize<BingoSet>(serializedSet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bingo set for client {ClientId}", clientId);
                return null;
            }
        }

        public async Task<bool> CheckForWinAsync(string clientId)
        {
            try
            {
                var bingoSet = await GetClientBingoSetAsync(clientId);
                if (bingoSet == null) return false;

                // Convert to 5x5 grid for win checking
                var grid = new bool[5, 5];
                for (int i = 0; i < 25; i++)
                {
                    grid[i / 5, i % 5] = bingoSet.Squares[i].IsChecked;
                }

                // Check rows
                for (int row = 0; row < 5; row++)
                {
                    bool rowWin = true;
                    for (int col = 0; col < 5; col++)
                    {
                        if (!grid[row, col])
                        {
                            rowWin = false;
                            break;
                        }
                    }
                    if (rowWin) return true;
                }

                // Check columns
                for (int col = 0; col < 5; col++)
                {
                    bool colWin = true;
                    for (int row = 0; row < 5; row++)
                    {
                        if (!grid[row, col])
                        {
                            colWin = false;
                            break;
                        }
                    }
                    if (colWin) return true;
                }

                // Check diagonals
                bool diagonal1 = true, diagonal2 = true;
                for (int i = 0; i < 5; i++)
                {
                    if (!grid[i, i]) diagonal1 = false;
                    if (!grid[i, 4 - i]) diagonal2 = false;
                }

                return diagonal1 || diagonal2;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for win for client {ClientId}", clientId);
                return false;
            }
        }

        public async Task<List<BingoSet>> GetAllClientSetsAsync()
        {
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                var bingoSets = new List<BingoSet>();

                foreach (var client in clients.Where(c => !string.IsNullOrEmpty(c.CurrentBingoSetId)))
                {
                    var bingoSet = await GetClientBingoSetAsync(client.ConnectionId);
                    if (bingoSet != null)
                    {
                        bingoSets.Add(bingoSet);
                    }
                }

                return bingoSets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all client sets");
                return new List<BingoSet>();
            }
        }

        public async Task<bool> UpdateSquareGloballyAsync(string squareId, bool isChecked)
        {
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                var updateTasks = new List<Task<bool>>();

                // Store the global state
                var globalKey = $"global_square_{squareId}";
                await _cache.SetStringAsync(globalKey, isChecked.ToString());

                // Update all client bingo sets that contain this square
                foreach (var client in clients.Where(c => !string.IsNullOrEmpty(c.CurrentBingoSetId)))
                {
                    updateTasks.Add(UpdateSquareStatusAsync(client.ConnectionId, squareId, isChecked));
                }

                var results = await Task.WhenAll(updateTasks);
                var successCount = results.Count(r => r);

                _logger.LogInformation("Updated square {SquareId} globally for {SuccessCount}/{TotalCount} clients", 
                    squareId, successCount, results.Length);

                return successCount > 0; // Return true if at least one update succeeded
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating square globally");
                return false;
            }
        }

        public async Task<List<string>> GetGloballyCheckedSquaresAsync()
        {
            try
            {
                var globallyChecked = new List<string>();
                
                foreach (var square in _allSquares)
                {
                    var globalKey = $"global_square_{square.Id}";
                    var isCheckedStr = await _cache.GetStringAsync(globalKey);
                    
                    if (bool.TryParse(isCheckedStr, out bool isChecked) && isChecked)
                    {
                        globallyChecked.Add(square.Id);
                    }
                }

                return globallyChecked;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving globally checked squares");
                return new List<string>();
            }
        }

        public async Task<string> RequestSquareApprovalAsync(string clientId, string squareId, bool requestedState)
        {
            try
            {
                // Get the square details for the label
                var square = _allSquares.FirstOrDefault(s => s.Id == squareId);
                if (square == null)
                {
                    throw new ArgumentException($"Square with ID {squareId} not found");
                }

                var approval = new PendingApproval
                {
                    ClientId = clientId,
                    SquareId = squareId,
                    SquareLabel = square.Label,
                    RequestedState = requestedState
                };

                // Store in Redis cache
                var cacheKey = $"pending_approval_{approval.Id}";
                var serializedApproval = JsonSerializer.Serialize(approval);
                await _cache.SetStringAsync(cacheKey, serializedApproval, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2) // Expire after 2 hours
                });

                // Add to list of pending approvals
                var pendingListKey = "pending_approvals_list";
                var existingList = await _cache.GetStringAsync(pendingListKey);
                var approvalIds = string.IsNullOrEmpty(existingList) 
                    ? new List<string>() 
                    : JsonSerializer.Deserialize<List<string>>(existingList) ?? new List<string>();
                
                approvalIds.Add(approval.Id);
                await _cache.SetStringAsync(pendingListKey, JsonSerializer.Serialize(approvalIds));

                _logger.LogInformation("Created approval request {ApprovalId} for client {ClientId}, square {SquareId}", 
                    approval.Id, clientId, squareId);

                return approval.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating approval request for client {ClientId}", clientId);
                throw;
            }
        }

        public async Task<List<PendingApproval>> GetPendingApprovalsAsync()
        {
            try
            {
                var pendingListKey = "pending_approvals_list";
                var existingList = await _cache.GetStringAsync(pendingListKey);
                
                if (string.IsNullOrEmpty(existingList))
                    return new List<PendingApproval>();

                var approvalIds = JsonSerializer.Deserialize<List<string>>(existingList) ?? new List<string>();
                var approvals = new List<PendingApproval>();

                foreach (var approvalId in approvalIds)
                {
                    var approval = await GetPendingApprovalAsync(approvalId);
                    if (approval != null && approval.Status == ApprovalStatus.Pending)
                    {
                        approvals.Add(approval);
                    }
                }

                return approvals.OrderBy(a => a.RequestedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending approvals");
                return new List<PendingApproval>();
            }
        }

        public async Task<bool> ApproveSquareRequestAsync(string approvalId, string adminId)
        {
            try
            {
                var approval = await GetPendingApprovalAsync(approvalId);
                if (approval == null || approval.Status != ApprovalStatus.Pending)
                {
                    return false;
                }

                // Update the approval status
                approval.Status = ApprovalStatus.Approved;
                approval.ProcessedByAdmin = adminId;
                approval.ProcessedAt = DateTime.UtcNow;

                // Save updated approval
                var cacheKey = $"pending_approval_{approvalId}";
                var serializedApproval = JsonSerializer.Serialize(approval);
                await _cache.SetStringAsync(cacheKey, serializedApproval);

                // Apply the change globally to all clients
                await UpdateSquareGloballyAsync(approval.SquareId, approval.RequestedState);

                _logger.LogInformation("Approved square request {ApprovalId} by admin {AdminId}", approvalId, adminId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving square request {ApprovalId}", approvalId);
                return false;
            }
        }

        public async Task<bool> DenySquareRequestAsync(string approvalId, string adminId, string? reason = null)
        {
            try
            {
                var approval = await GetPendingApprovalAsync(approvalId);
                if (approval == null || approval.Status != ApprovalStatus.Pending)
                {
                    return false;
                }

                // Update the approval status
                approval.Status = ApprovalStatus.Denied;
                approval.ProcessedByAdmin = adminId;
                approval.ProcessedAt = DateTime.UtcNow;
                approval.DenialReason = reason;

                // Save updated approval
                var cacheKey = $"pending_approval_{approvalId}";
                var serializedApproval = JsonSerializer.Serialize(approval);
                await _cache.SetStringAsync(cacheKey, serializedApproval);

                _logger.LogInformation("Denied square request {ApprovalId} by admin {AdminId}", approvalId, adminId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error denying square request {ApprovalId}", approvalId);
                return false;
            }
        }

        public async Task<PendingApproval?> GetPendingApprovalAsync(string approvalId)
        {
            try
            {
                var cacheKey = $"pending_approval_{approvalId}";
                var serializedApproval = await _cache.GetStringAsync(cacheKey);
                
                if (string.IsNullOrEmpty(serializedApproval))
                    return null;

                return JsonSerializer.Deserialize<PendingApproval>(serializedApproval);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending approval {ApprovalId}", approvalId);
                return null;
            }
        }

        public async Task CleanupExpiredApprovalsAsync()
        {
            try
            {
                var pendingListKey = "pending_approvals_list";
                var existingList = await _cache.GetStringAsync(pendingListKey);
                
                if (string.IsNullOrEmpty(existingList))
                    return;

                var approvalIds = JsonSerializer.Deserialize<List<string>>(existingList) ?? new List<string>();
                var validApprovalIds = new List<string>();

                foreach (var approvalId in approvalIds)
                {
                    var approval = await GetPendingApprovalAsync(approvalId);
                    if (approval != null)
                    {
                        // Mark as expired if older than 2 hours and still pending
                        if (approval.Status == ApprovalStatus.Pending && 
                            approval.RequestedAt.AddHours(2) < DateTime.UtcNow)
                        {
                            approval.Status = ApprovalStatus.Expired;
                            var cacheKey = $"pending_approval_{approvalId}";
                            var serializedApproval = JsonSerializer.Serialize(approval);
                            await _cache.SetStringAsync(cacheKey, serializedApproval);
                        }

                        // Keep in list if not too old (for tracking purposes)
                        if (approval.RequestedAt.AddDays(1) > DateTime.UtcNow)
                        {
                            validApprovalIds.Add(approvalId);
                        }
                    }
                }

                // Update the list
                await _cache.SetStringAsync(pendingListKey, JsonSerializer.Serialize(validApprovalIds));

                _logger.LogInformation("Cleaned up expired approvals, {ValidCount} remain", validApprovalIds.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired approvals");
            }
        }

        /// <summary>
        /// Generate the complete list of available bingo squares
        /// </summary>
        private static List<BingoSquare> GenerateAllSquares()
        {
            return new List<BingoSquare>
            {
                new() { Id = "free", Label = "Free Space", Type = "free" },
                new() { Id = "council-of-aspirations", Label = "\"Council of Aspirations\"", Type = "quote" },
                new() { Id = "screen-share-fail", Label = "Screen share fail", Type = "oops" },
                new() { Id = "pine-mentioned", Label = "David Pine 🌲 mentioned", Type = "quote" },
                new() { Id = "multiple-options", Label = "\"Well, you can do this a few ways...\"", Type = "quote" },
                new() { Id = "app-bug", Label = "Bug found in guest's app", Type = "bug" },
                new() { Id = "scared", Label = "Someone is scared to try something", Type = "dev" },
                new() { Id = "damian-fowler-bicker", Label = "Damian and Fowler bicker", Type = "dev" },
                new() { Id = "friday-behavior", Label = "\"Friday Behavior\"", Type = "quote" },
                new() { Id = "ignore-docs", Label = "Someone ignores the docs", Type = "oops" },
                new() { Id = "damian-tbc", Label = "Damian says \"To be clear/To be specific\"", Type = "quote" },
                new() { Id = "different-opinions", Label = "Disagreement on how to do something", Type = "dev" },
                new() { Id = "error-celly", Label = "Excited to see an error", Type = "dev" },
                new() { Id = "av-issue", Label = "AV/stream issues", Type = "oops" },
                new() { Id = "new-bug", Label = "Found a new bug in Aspire", Type = "bug" },
                new() { Id = "old-bug", Label = "Hit a bug we've already filed", Type = "bug" },
                new() { Id = "maddy-swears", Label = "Maddy accidentally swears", Type = "quote" },
                new() { Id = "bathroom-break", Label = "Bathroom break", Type = "meta" },
                new() { Id = "this-wont-work", Label = "\"There's no way this works, right?\"", Type = "quote" },
                new() { Id = "did-that-work", Label = "\"Wait, did that work?!\"", Type = "quote" },
                new() { Id = "aspire-pun", Label = "Aspire pun made", Type = "meta" },
                new() { Id = "fowler-pause", Label = "Fowler says \"PAUSE\" or \"WAIT\"", Type = "quote" },
                new() { Id = "restart-something", Label = "Restarted editor/IDE", Type = "oops" },
                new() { Id = "do-it-live", Label = "\"Let's do it live\"", Type = "quote" },
                new() { Id = "refactoring", Label = "Impromptu refactoring", Type = "dev" },
                new() { Id = "port-problems", Label = "Ports being difficult", Type = "oops" },
                new() { Id = "fowler-llm", Label = "Fowler 💞 AI", Type = "meta" },
                new() { Id = "vibe-coding", Label = "Vibe coding mentioned", Type = "quote" },
                new() { Id = "bad-ai", Label = "AI autocomplete being annoying", Type = "dev" },
                new() { Id = "live-share", Label = "Accidentally kills live share", Type = "oops" },
                new() { Id = "frustration", Label = "Visible frustration", Type = "dev" },
                // Additional squares for better randomization
                new() { Id = "coffee-mention", Label = "Coffee mentioned", Type = "meta" },
                new() { Id = "github-issues", Label = "GitHub issues discussion", Type = "dev" },
                new() { Id = "demo-gods", Label = "\"Demo gods\" mentioned", Type = "quote" }
                ,
                new() { Id = "fowler-monorepo", Label = "Fowler advocates monorepo", Type = "dev" },
                new() { Id = "private-key-shared", Label = "Someone shares a private key", Type = "oops" },
                new() { Id = "one-line-add", Label = "\"It's one line, so let's add it\"", Type = "quote" },
                new() { Id = "one-day-work", Label = "\"One day, that'll work\"", Type = "quote" },
                new() { Id = "maddy-snack", Label = "Maddy eats a snack live", Type = "meta" }
            };
        }
    }
}
