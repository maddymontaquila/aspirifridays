using BingoBoard.Admin.Models;
using BingoBoard.Admin.Services;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace BingoBoard.Admin.Services;

/// <summary>
/// Implementation of bingo service using Redis for persistence and file for squares
/// </summary>
public class BingoService(IDistributedCache cache, ILogger<BingoService> logger, IClientConnectionService clientService, IWebHostEnvironment env) : IBingoService
{
    private static readonly List<BingoSquare> _allSquares = GenerateAllSquares();
    private static readonly string DataFilePath = "Data/bingo-squares.json";

    public async Task<List<BingoSquare>> GetAllSquaresAsync()
    {
        try
        {
            // Try to load from file first
            var filePath = Path.Combine(env.ContentRootPath, DataFilePath);
            
            if (File.Exists(filePath))
            {
                var json = await File.ReadAllTextAsync(filePath);
                var fileSquares = JsonSerializer.Deserialize<List<BingoSquareFromFile>>(json, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
                });
                
                if (fileSquares != null && fileSquares.Any())
                {
                    logger.LogInformation("Loaded {Count} squares from file", fileSquares.Count);
                    return fileSquares.Select(s => new BingoSquare
                    {
                        Id = s.Id,
                        Label = s.Label,
                        Type = s.Type ?? "default"
                    }).ToList();
                }
            }
            
            // Fallback to static list if file doesn't exist or is empty
            logger.LogWarning("File not found or empty, falling back to static list");
            return _allSquares.ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error loading squares from file, falling back to static list");
            return _allSquares.ToList();
        }
    }

    private record BingoSquareFromFile
    {
        public string Id { get; init; } = string.Empty;
        public string Label { get; init; } = string.Empty;
        public string? Type { get; init; }
    }

    public async Task<BingoSet> GenerateRandomBingoSetAsync(string clientId)
    {
        try
        {
            // Load all squares from database
            var allSquares = await GetAllSquaresAsync();
            
            // Get a random selection of 24 squares plus the free space
            var random = new Random();
            var freeSpace = allSquares.FirstOrDefault(s => s.Id == "free");
            
            // If no free space exists, create one
            if (freeSpace == null)
            {
                freeSpace = new BingoSquare
                {
                    Id = "free",
                    Label = "Free Space",
                    Type = "free"
                };
            }
            
            var availableSquares = allSquares.Where(s => s.Id != "free").ToList();
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
            await cache.SetStringAsync(cacheKey, serializedSet, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24) // Keep for 24 hours
            });

            logger.LogInformation("Generated new bingo set for client {ClientId} with {Count} total squares", clientId, allSquares.Count);
            return bingoSet;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating bingo set for client {ClientId}", clientId);
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
            await cache.SetStringAsync(cacheKey, serializedSet);

            logger.LogInformation("Updated square {SquareId} to {Status} for client {ClientId}",
                squareId, isChecked, clientId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating square status for client {ClientId}", clientId);
            return false;
        }
    }

    private async Task UpdateSquareStatusAsync(string clientId, int squareId, bool isChecked)
    {
        try
        {
            var existingSet = await GetClientBingoSetAsync(clientId);
            if (existingSet?.Squares != null)
            {
                var square = existingSet.Squares.FirstOrDefault(s => s.Id == squareId.ToString());
                if (square != null)
                {
                    square.IsChecked = isChecked;

                    // Save updated client board state
                    var cacheKey = $"bingo_set_{clientId}";
                    var serializedSet = JsonSerializer.Serialize(existingSet);
                    var cacheOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                    };
                    await cache.SetStringAsync(cacheKey, serializedSet, cacheOptions);

                    logger.LogInformation("Updated square {SquareId} status to {IsChecked} for client {ClientId}",
                        squareId, isChecked, clientId);
                }
                else
                {
                    logger.LogWarning("Square {SquareId} not found for client {ClientId}", squareId, clientId);
                }
            }
            else
            {
                logger.LogWarning("No bingo set found for client {ClientId}", clientId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating square status for client {ClientId}", clientId);
        }
    }

    public async Task<BingoSet?> GetClientBingoSetAsync(string clientId)
    {
        try
        {
            var cacheKey = $"bingo_set_{clientId}";
            var serializedSet = await cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(serializedSet))
                return null;

            return JsonSerializer.Deserialize<BingoSet>(serializedSet);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving bingo set for client {ClientId}", clientId);
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
            logger.LogError(ex, "Error checking for win for client {ClientId}", clientId);
            return false;
        }
    }

    public async Task<List<BingoSet>> GetAllClientSetsAsync()
    {
        try
        {
            var clients = await clientService.GetAllClientsAsync();
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
            logger.LogError(ex, "Error retrieving all client sets");
            return [];
        }
    }

    public async Task<bool> UpdateSquareGloballyAsync(string squareId, bool isChecked)
    {
        try
        {
            var clients = await clientService.GetAllClientsAsync();
            var updateTasks = new List<Task<bool>>();

            // Store the global state
            var globalKey = $"global_square_{squareId}";
            await cache.SetStringAsync(globalKey, isChecked.ToString());

            // Update all client bingo sets that contain this square
            foreach (var client in clients.Where(c => !string.IsNullOrEmpty(c.CurrentBingoSetId)))
            {
                updateTasks.Add(UpdateSquareStatusAsync(client.ConnectionId, squareId, isChecked));
            }

            var results = await Task.WhenAll(updateTasks);
            var successCount = results.Count(r => r);

            logger.LogInformation("Updated square {SquareId} globally for {SuccessCount}/{TotalCount} clients",
                squareId, successCount, results.Length);

            return successCount > 0; // Return true if at least one update succeeded
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating square globally");
            return false;
        }
    }

    public async Task<bool> UpdateSquareForAdminAsync(string adminClientId, string squareId, bool isChecked)
    {
        try
        {
            // Update only the admin's bingo set, bypassing approval workflow
            var success = await UpdateSquareStatusAsync(adminClientId, squareId, isChecked);

            if (success)
            {
                // Store the global state for tracking but don't broadcast to other clients
                var globalKey = $"global_square_{squareId}";
                await cache.SetStringAsync(globalKey, isChecked.ToString());

                logger.LogInformation("Updated square {SquareId} for admin {AdminId} without global broadcast",
                    squareId, adminClientId);
            }

            return success;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating square for admin {AdminId}", adminClientId);
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
                var isCheckedStr = await cache.GetStringAsync(globalKey);

                if (bool.TryParse(isCheckedStr, out bool isChecked) && isChecked)
                {
                    globallyChecked.Add(square.Id);
                }
            }

            return globallyChecked;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving globally checked squares");
            return [];
        }
    }

    public async Task<string> RequestSquareApprovalAsync(string clientId, string squareId, bool requestedState)
    {
        try
        {
            // Check if the square is already approved globally with the requested state
            var globalKey = $"global_square_{squareId}";
            var globalStateStr = await cache.GetStringAsync(globalKey);

            if (bool.TryParse(globalStateStr, out bool currentGlobalState) && currentGlobalState == requestedState)
            {
                logger.LogInformation("Square {SquareId} is already globally approved with state {RequestedState}, skipping approval request for client {ClientId}",
                    squareId, requestedState, clientId);

                // Return a special marker to indicate already approved
                return "ALREADY_APPROVED";
            }

            // Get the square details for the label
            var square = _allSquares.FirstOrDefault(s => s.Id == squareId) ?? throw new ArgumentException($"Square with ID {squareId} not found");
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
            await cache.SetStringAsync(cacheKey, serializedApproval, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(2) // Expire after 2 hours
            });

            // Add to list of pending approvals
            var pendingListKey = "pending_approvals_list";
            var existingList = await cache.GetStringAsync(pendingListKey);
            var approvalIds = string.IsNullOrEmpty(existingList)
                ? []
                : JsonSerializer.Deserialize<List<string>>(existingList) ?? [];

            approvalIds.Add(approval.Id);
            await cache.SetStringAsync(pendingListKey, JsonSerializer.Serialize(approvalIds));

            logger.LogInformation("Created approval request {ApprovalId} for client {ClientId}, square {SquareId}",
                approval.Id, clientId, squareId);

            return approval.Id;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating approval request for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<List<PendingApproval>> GetPendingApprovalsAsync()
    {
        try
        {
            var pendingListKey = "pending_approvals_list";
            var existingList = await cache.GetStringAsync(pendingListKey);

            if (string.IsNullOrEmpty(existingList))
                return [];

            var approvalIds = JsonSerializer.Deserialize<List<string>>(existingList) ?? [];
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
            logger.LogError(ex, "Error retrieving pending approvals");
            return [];
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

            // Find all pending requests for the same square with the same requested state
            var allPendingApprovals = await GetPendingApprovalsAsync();
            var relatedApprovals = allPendingApprovals
                .Where(a => a.SquareId == approval.SquareId &&
                           a.RequestedState == approval.RequestedState &&
                           a.Status == ApprovalStatus.Pending)
                .ToList();

            logger.LogInformation("Found {Count} related approval requests for square {SquareId}",
                relatedApprovals.Count, approval.SquareId);

            // Process all related approvals
            foreach (var relatedApproval in relatedApprovals)
            {
                relatedApproval.Status = ApprovalStatus.Approved;
                relatedApproval.ProcessedByAdmin = adminId;
                relatedApproval.ProcessedAt = DateTime.UtcNow;

                // Save updated approval
                var cacheKey = $"pending_approval_{relatedApproval.Id}";
                var serializedApproval = JsonSerializer.Serialize(relatedApproval);
                await cache.SetStringAsync(cacheKey, serializedApproval);

                // CRITICAL FIX: Update the client's board state in the server cache
                await UpdateSquareStatusAsync(relatedApproval.ClientId, approval.SquareId, approval.RequestedState);

                logger.LogInformation("Approved related square request {ApprovalId} for client {ClientId} and updated their board state",
                    relatedApproval.Id, relatedApproval.ClientId);
            }

            // Update ONLY the admin's board, not globally - this is the key change
            // The admin gets the square checked off on their board when they approve it
            await UpdateSquareForAdminAsync(adminId, approval.SquareId, approval.RequestedState);

            logger.LogInformation("Approved {Count} square requests for square {SquareId} by admin {AdminId} - updated admin board only",
                relatedApprovals.Count, approval.SquareId, adminId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error approving square request {ApprovalId}", approvalId);
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

            // Find all pending requests for the same square with the same requested state
            var allPendingApprovals = await GetPendingApprovalsAsync();
            var relatedApprovals = allPendingApprovals
                .Where(a => a.SquareId == approval.SquareId &&
                           a.RequestedState == approval.RequestedState &&
                           a.Status == ApprovalStatus.Pending)
                .ToList();

            logger.LogInformation("Found {Count} related approval requests to deny for square {SquareId}",
                relatedApprovals.Count, approval.SquareId);

            // Process all related approvals
            foreach (var relatedApproval in relatedApprovals)
            {
                relatedApproval.Status = ApprovalStatus.Denied;
                relatedApproval.ProcessedByAdmin = adminId;
                relatedApproval.ProcessedAt = DateTime.UtcNow;
                relatedApproval.DenialReason = reason;

                // Save updated approval
                var cacheKey = $"pending_approval_{relatedApproval.Id}";
                var serializedApproval = JsonSerializer.Serialize(relatedApproval);
                await cache.SetStringAsync(cacheKey, serializedApproval);

                logger.LogInformation("Denied related square request {ApprovalId} for client {ClientId}",
                    relatedApproval.Id, relatedApproval.ClientId);
            }

            logger.LogInformation("Denied {Count} square requests for square {SquareId} by admin {AdminId}",
                relatedApprovals.Count, approval.SquareId, adminId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error denying square request {ApprovalId}", approvalId);
            return false;
        }
    }

    public async Task<PendingApproval?> GetPendingApprovalAsync(string approvalId)
    {
        try
        {
            var cacheKey = $"pending_approval_{approvalId}";
            var serializedApproval = await cache.GetStringAsync(cacheKey);

            if (string.IsNullOrEmpty(serializedApproval))
                return null;

            return JsonSerializer.Deserialize<PendingApproval>(serializedApproval);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving pending approval {ApprovalId}", approvalId);
            return null;
        }
    }

    public async Task CleanupExpiredApprovalsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var pendingListKey = "pending_approvals_list";
            var existingList = await cache.GetStringAsync(pendingListKey, cancellationToken);

            if (string.IsNullOrEmpty(existingList))
                return;

            var approvalIds = JsonSerializer.Deserialize<List<string>>(existingList) ?? [];
            var validApprovalIds = new List<string>();

            foreach (var approvalId in approvalIds)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
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
                        await cache.SetStringAsync(cacheKey, serializedApproval, cancellationToken);
                    }

                    // Keep in list if not too old (for tracking purposes)
                    if (approval.RequestedAt.AddDays(1) > DateTime.UtcNow)
                    {
                        validApprovalIds.Add(approvalId);
                    }
                }
            }

            // Update the list
            await cache.SetStringAsync(pendingListKey, JsonSerializer.Serialize(validApprovalIds), cancellationToken);

            logger.LogInformation("Cleaned up expired approvals, {ValidCount} remain", validApprovalIds.Count);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Cleanup operation was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up expired approvals");
        }
    }

    public async Task<bool> UpdateClientConnectionAsync(string oldClientId, string newClientId)
    {
        try
        {
            // Get the existing bingo set
            var bingoSet = await GetClientBingoSetAsync(oldClientId);
            if (bingoSet == null) return false;

            // Don't change the ClientId - keep it as the persistent client ID
            // Just update the timestamp
            bingoSet.LastUpdated = DateTime.UtcNow;

            // Store under the same persistent client ID (the cache key doesn't change)
            var cacheKey = $"bingo_set_{oldClientId}";
            var serializedSet = JsonSerializer.Serialize(bingoSet);
            await cache.SetStringAsync(cacheKey, serializedSet, new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromHours(24)
            });

            logger.LogInformation("Updated timestamp for persistent client {PersistentClientId} with new connection {NewConnectionId}",
                oldClientId, newClientId);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating client connection for persistent client {PersistentClientId} with new connection {NewConnectionId}",
                oldClientId, newClientId);
            return false;
        }
    }

    public async Task SetLiveModeAsync(bool isLiveMode)
    {
        try
        {
            var cacheKey = "bingo_live_mode";
            await cache.SetStringAsync(cacheKey, isLiveMode.ToString());
            logger.LogInformation("Live mode set to: {IsLiveMode}", isLiveMode);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error setting live mode to {IsLiveMode}", isLiveMode);
            throw;
        }
    }

    public async Task<bool> GetLiveModeAsync()
    {
        try
        {
            var cacheKey = "bingo_live_mode";
            var liveModeStr = await cache.GetStringAsync(cacheKey);

            // Default to true (live mode) if not set for safety
            if (string.IsNullOrEmpty(liveModeStr))
            {
                await SetLiveModeAsync(true);
                return true;
            }

            return bool.TryParse(liveModeStr, out bool isLiveMode) ? isLiveMode : true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting live mode");
            return true; // Default to live mode for safety
        }
    }

    public async Task<(bool needsApproval, string? approvalId)> HandleSquareRequestAsync(string clientId, string squareId, bool requestedState)
    {
        try
        {
            var isLiveMode = await GetLiveModeAsync();

            if (!isLiveMode)
            {
                // Free play mode - update square directly without approval
                var success = await UpdateSquareStatusAsync(clientId, squareId, requestedState);
                if (success)
                {
                    // Also update the global state for tracking
                    var globalKey = $"global_square_{squareId}";
                    await cache.SetStringAsync(globalKey, requestedState.ToString());

                    logger.LogInformation("Free play mode: Updated square {SquareId} to {RequestedState} for client {ClientId}",
                        squareId, requestedState, clientId);

                    return (false, null); // No approval needed
                }
                else
                {
                    throw new InvalidOperationException("Failed to update square in free play mode");
                }
            }
            else
            {
                // Live mode - require approval
                var approvalId = await RequestSquareApprovalAsync(clientId, squareId, requestedState);
                return (true, approvalId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling square request for client {ClientId}", clientId);
            throw;
        }
    }

    public async Task<int> ApproveAllPendingRequestsAsync(string adminId)
    {
        try
        {
            var pendingApprovals = await GetPendingApprovalsAsync();

            if (!pendingApprovals.Any())
            {
                logger.LogInformation("No pending approvals to process");
                return 0;
            }

            // Group approvals by square and requested state to process them efficiently
            var approvalGroups = pendingApprovals
                .GroupBy(a => new { a.SquareId, a.RequestedState })
                .ToList();

            int totalProcessed = 0;

            foreach (var group in approvalGroups)
            {
                var groupKey = group.Key;

                // Process all approvals in this group
                foreach (var approval in group)
                {
                    approval.Status = ApprovalStatus.Approved;
                    approval.ProcessedByAdmin = adminId;
                    approval.ProcessedAt = DateTime.UtcNow;

                    // Save updated approval
                    var cacheKey = $"pending_approval_{approval.Id}";
                    var serializedApproval = JsonSerializer.Serialize(approval);
                    await cache.SetStringAsync(cacheKey, serializedApproval);

                    // Update the client's board state in the server cache
                    await UpdateSquareStatusAsync(approval.ClientId, approval.SquareId, approval.RequestedState);

                    totalProcessed++;
                }

                // Update the square globally for this group (only once per group)
                await UpdateSquareGloballyAsync(groupKey.SquareId, groupKey.RequestedState);
            }

            logger.LogInformation("Approved {Count} pending requests across {GroupCount} square groups by admin {AdminId}",
                totalProcessed, approvalGroups.Count, adminId);

            return totalProcessed;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error approving all pending requests");
            throw; // Re-throw to let caller handle the error
        }
    }

    /// <summary>
    /// Generate the complete list of available bingo squares
    /// </summary>
    private static List<BingoSquare> GenerateAllSquares()
    {
        return
        [
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
        ];
    }
}
