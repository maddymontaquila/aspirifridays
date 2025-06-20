using Microsoft.AspNetCore.SignalR;
using BingoBoard.Admin.Services;
using BingoBoard.Admin.Models;

namespace BingoBoard.Admin.Hubs
{
    /// <summary>
    /// SignalR hub for real-time bingo board communication
    /// </summary>
    public class BingoHub : Hub
    {
        private readonly IBingoService _bingoService;
        private readonly IClientConnectionService _clientService;
        private readonly ILogger<BingoHub> _logger;

        public BingoHub(
            IBingoService bingoService, 
            IClientConnectionService clientService,
            ILogger<BingoHub> logger)
        {
            _bingoService = bingoService;
            _clientService = clientService;
            _logger = logger;
        }

        /// <summary>
        /// Client requests a new bingo set
        /// </summary>
        public async Task RequestBingoSet(string? userName = null)
        {
            try
            {
                var connectionId = Context.ConnectionId;
                _logger.LogInformation("Client {ConnectionId} requested a new bingo set", connectionId);

                // Generate a new bingo set for the client
                var bingoSet = await _bingoService.GenerateRandomBingoSetAsync(connectionId);
                
                // Associate the bingo set with the client
                await _clientService.AssociateBingoSetAsync(connectionId, bingoSet.Id);

                // Send the bingo set to the requesting client
                await Clients.Caller.SendAsync("BingoSetReceived", bingoSet);

                // Notify admin of new client with bingo set
                await Clients.Others.SendAsync("ClientBingoSetGenerated", new 
                { 
                    ConnectionId = connectionId, 
                    BingoSetId = bingoSet.Id,
                    UserName = userName,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Sent bingo set {BingoSetId} to client {ConnectionId}", 
                    bingoSet.Id, connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating bingo set for client {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Failed to generate bingo set");
            }
        }

        /// <summary>
        /// Admin updates a square status for a specific client
        /// </summary>
        public async Task AdminUpdateSquare(string clientId, string squareId, bool isChecked)
        {
            try
            {
                _logger.LogInformation("Admin updating square {SquareId} to {Status} for client {ClientId}", 
                    squareId, isChecked, clientId);

                var success = await _bingoService.UpdateSquareStatusAsync(clientId, squareId, isChecked);
                
                if (success)
                {
                    // Check for win condition
                    var hasWin = await _bingoService.CheckForWinAsync(clientId);

                    // Notify the specific client about the update
                    await Clients.Client(clientId).SendAsync("SquareUpdated", new 
                    { 
                        SquareId = squareId, 
                        IsChecked = isChecked,
                        HasWin = hasWin,
                        Timestamp = DateTime.UtcNow
                    });

                    // Notify all admin clients about the update
                    await Clients.Others.SendAsync("AdminSquareUpdate", new 
                    { 
                        ClientId = clientId,
                        SquareId = squareId, 
                        IsChecked = isChecked,
                        HasWin = hasWin,
                        Timestamp = DateTime.UtcNow
                    });

                    if (hasWin)
                    {
                        _logger.LogInformation("Client {ClientId} achieved bingo!", clientId);
                        await Clients.All.SendAsync("BingoAchieved", new { ClientId = clientId });
                    }
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Failed to update square");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating square for admin");
                await Clients.Caller.SendAsync("Error", "Failed to update square");
            }
        }

        /// <summary>
        /// Client requests approval to mark a square
        /// </summary>
        public async Task RequestSquareApproval(string squareId, bool requestedState)
        {
            try
            {
                var connectionId = Context.ConnectionId;
                _logger.LogInformation("Client {ConnectionId} requesting approval for square {SquareId} to {Status}", 
                    connectionId, squareId, requestedState);

                var approvalId = await _bingoService.RequestSquareApprovalAsync(connectionId, squareId, requestedState);
                
                // Update client activity
                await _clientService.UpdateClientActivityAsync(connectionId);

                // Get the square details for notification
                var allSquares = await _bingoService.GetAllSquaresAsync();
                var square = allSquares.FirstOrDefault(s => s.Id == squareId);
                var squareLabel = square?.Label ?? squareId;

                // Confirm request submission to the client
                await Clients.Caller.SendAsync("ApprovalRequestSubmitted", new 
                { 
                    ApprovalId = approvalId,
                    SquareId = squareId,
                    RequestedState = requestedState,
                    Message = $"Request to {(requestedState ? "check" : "uncheck")} '{squareLabel}' has been submitted for admin approval",
                    Timestamp = DateTime.UtcNow
                });

                // Notify admin clients about the new approval request
                await Clients.Others.SendAsync("NewApprovalRequest", new 
                { 
                    ApprovalId = approvalId,
                    ClientId = connectionId,
                    SquareId = squareId,
                    SquareLabel = squareLabel,
                    RequestedState = requestedState,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Created approval request {ApprovalId} for client {ConnectionId}", 
                    approvalId, connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating approval request for client {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Failed to submit approval request");
            }
        }

        /// <summary>
        /// Get current bingo set for a client
        /// </summary>
        public async Task GetCurrentBingoSet()
        {
            try
            {
                var connectionId = Context.ConnectionId;
                var bingoSet = await _bingoService.GetClientBingoSetAsync(connectionId);
                
                if (bingoSet != null)
                {
                    await Clients.Caller.SendAsync("CurrentBingoSet", bingoSet);
                }
                else
                {
                    await Clients.Caller.SendAsync("NoBingoSet");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bingo set for client {ConnectionId}", Context.ConnectionId);
                await Clients.Caller.SendAsync("Error", "Failed to retrieve bingo set");
            }
        }

        /// <summary>
        /// Admin requests list of all connected clients
        /// </summary>
        public async Task GetConnectedClients()
        {
            try
            {
                var clients = await _clientService.GetAllClientsAsync();
                await Clients.Caller.SendAsync("ConnectedClientsList", clients);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving connected clients");
                await Clients.Caller.SendAsync("Error", "Failed to retrieve connected clients");
            }
        }

        public override async Task OnConnectedAsync()
        {
            try
            {
                var connectionId = Context.ConnectionId;
                var httpContext = Context.GetHttpContext();
                
                var client = new ConnectedClient
                {
                    ConnectionId = connectionId,
                    IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = httpContext?.Request.Headers.UserAgent.ToString()
                };

                await _clientService.AddClientAsync(client);
                
                // Notify all clients about the new connection
                await Clients.All.SendAsync("UserConnected", new 
                { 
                    ConnectionId = connectionId,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Client {ConnectionId} connected", connectionId);
                await base.OnConnectedAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client connection");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var connectionId = Context.ConnectionId;
                
                await _clientService.RemoveClientAsync(connectionId);
                
                // Notify all clients about the disconnection
                await Clients.All.SendAsync("UserDisconnected", new 
                { 
                    ConnectionId = connectionId,
                    Timestamp = DateTime.UtcNow
                });

                _logger.LogInformation("Client {ConnectionId} disconnected", connectionId);
                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling client disconnection");
            }
        }

        /// <summary>
        /// Admin checks a square globally for all clients
        /// </summary>
        public async Task AdminCheckSquareGlobally(string squareId, bool isChecked)
        {
            try
            {
                _logger.LogInformation("Admin checking square {SquareId} globally to {Status}", 
                    squareId, isChecked);

                var success = await _bingoService.UpdateSquareGloballyAsync(squareId, isChecked);
                
                if (success)
                {
                    // Get the square label for a better message
                    var allSquares = await _bingoService.GetAllSquaresAsync();
                    var square = allSquares.FirstOrDefault(s => s.Id == squareId);
                    var squareLabel = square?.Label ?? squareId;

                    // Notify all clients about the global square update
                    await Clients.All.SendAsync("GlobalSquareUpdate", new 
                    { 
                        SquareId = squareId, 
                        IsChecked = isChecked,
                        Timestamp = DateTime.UtcNow,
                        Message = $"'{squareLabel}' has been {(isChecked ? "checked" : "unchecked")} by admin"
                    });

                    _logger.LogInformation("Global square update sent for {SquareId} ({SquareLabel})", squareId, squareLabel);
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Failed to update square globally");
                    _logger.LogWarning("Failed to update square {SquareId} globally", squareId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating square globally");
                await Clients.Caller.SendAsync("Error", "Failed to update square globally");
            }
        }

        /// <summary>
        /// Get all available squares for admin management
        /// </summary>
        public async Task GetAllAvailableSquares()
        {
            try
            {
                var squares = await _bingoService.GetAllSquaresAsync();
                await Clients.Caller.SendAsync("AllSquaresReceived", squares);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all squares");
                await Clients.Caller.SendAsync("Error", "Failed to retrieve squares");
            }
        }

        #region Legacy Methods (for backward compatibility)
        
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("UserJoined", Context.ConnectionId);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("UserLeft", Context.ConnectionId);
        }

        public async Task SendMessageToGroup(string groupName, string user, string message)
        {
            await Clients.Group(groupName).SendAsync("ReceiveMessage", user, message);
        }

        public async Task SendBingoUpdate(string groupName, object bingoData)
        {
            await Clients.Group(groupName).SendAsync("BingoUpdate", bingoData);
        }

        public async Task BroadcastToAll(string message, object data)
        {
            await Clients.All.SendAsync("BroadcastMessage", message, data);
        }

        #endregion

        /// <summary>
        /// Admin approves a square marking request
        /// </summary>
        public async Task ApproveSquareRequest(string approvalId)
        {
            try
            {
                var adminId = Context.ConnectionId; // In a real app, you'd get this from authentication
                _logger.LogInformation("Admin {AdminId} approving request {ApprovalId}", adminId, approvalId);

                // Get the approval details before processing
                var approval = await _bingoService.GetPendingApprovalAsync(approvalId);
                if (approval == null)
                {
                    await Clients.Caller.SendAsync("Error", "Approval request not found");
                    return;
                }

                var success = await _bingoService.ApproveSquareRequestAsync(approvalId, adminId);
                
                if (success)
                {
                    // Get the square details for notification
                    var allSquares = await _bingoService.GetAllSquaresAsync();
                    var square = allSquares.FirstOrDefault(s => s.Id == approval.SquareId);
                    var squareLabel = square?.Label ?? approval.SquareId;

                    // Notify the requesting client that their request was approved
                    await Clients.Client(approval.ClientId).SendAsync("ApprovalRequestApproved", new 
                    { 
                        ApprovalId = approvalId,
                        SquareId = approval.SquareId,
                        RequestedState = approval.RequestedState,
                        Message = $"Your request to {(approval.RequestedState ? "check" : "uncheck")} '{squareLabel}' has been approved!",
                        Timestamp = DateTime.UtcNow
                    });

                    // Notify all admin clients about the approval
                    await Clients.Others.SendAsync("ApprovalRequestProcessed", new 
                    { 
                        ApprovalId = approvalId,
                        Status = "Approved",
                        ProcessedBy = adminId,
                        SquareId = approval.SquareId,
                        SquareLabel = squareLabel,
                        RequestedState = approval.RequestedState,
                        Timestamp = DateTime.UtcNow
                    });

                    // Send global square update to all clients (including the one who requested it)
                    await Clients.All.SendAsync("GlobalSquareUpdate", new 
                    { 
                        SquareId = approval.SquareId, 
                        IsChecked = approval.RequestedState,
                        Timestamp = DateTime.UtcNow,
                        Message = $"'{squareLabel}' has been {(approval.RequestedState ? "checked" : "unchecked")} by admin approval"
                    });

                    _logger.LogInformation("Approved request {ApprovalId} and sent global update for {SquareId}", approvalId, approval.SquareId);
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Failed to approve request");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving request {ApprovalId}", approvalId);
                await Clients.Caller.SendAsync("Error", "Failed to approve request");
            }
        }

        /// <summary>
        /// Admin denies a square marking request
        /// </summary>
        public async Task DenySquareRequest(string approvalId, string? reason = null)
        {
            try
            {
                var adminId = Context.ConnectionId; // In a real app, you'd get this from authentication
                _logger.LogInformation("Admin {AdminId} denying request {ApprovalId}", adminId, approvalId);

                // Get the approval details before processing
                var approval = await _bingoService.GetPendingApprovalAsync(approvalId);
                if (approval == null)
                {
                    await Clients.Caller.SendAsync("Error", "Approval request not found");
                    return;
                }

                var success = await _bingoService.DenySquareRequestAsync(approvalId, adminId, reason);
                
                if (success)
                {
                    // Get the square details for notification
                    var allSquares = await _bingoService.GetAllSquaresAsync();
                    var square = allSquares.FirstOrDefault(s => s.Id == approval.SquareId);
                    var squareLabel = square?.Label ?? approval.SquareId;

                    // Notify the requesting client that their request was denied
                    await Clients.Client(approval.ClientId).SendAsync("ApprovalRequestDenied", new 
                    { 
                        ApprovalId = approvalId,
                        SquareId = approval.SquareId,
                        RequestedState = approval.RequestedState,
                        Reason = reason,
                        Message = $"Your request to {(approval.RequestedState ? "check" : "uncheck")} '{squareLabel}' was denied" + 
                                  (string.IsNullOrEmpty(reason) ? "" : $": {reason}"),
                        Timestamp = DateTime.UtcNow
                    });

                    // Notify all admin clients about the denial
                    await Clients.Others.SendAsync("ApprovalRequestProcessed", new 
                    { 
                        ApprovalId = approvalId,
                        Status = "Denied",
                        ProcessedBy = adminId,
                        SquareId = approval.SquareId,
                        SquareLabel = squareLabel,
                        RequestedState = approval.RequestedState,
                        Reason = reason,
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("Denied request {ApprovalId} by admin {AdminId}", approvalId, adminId);
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Failed to deny request");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error denying request {ApprovalId}", approvalId);
                await Clients.Caller.SendAsync("Error", "Failed to deny request");
            }
        }

        /// <summary>
        /// Admin requests list of pending approval requests
        /// </summary>
        public async Task GetPendingApprovals()
        {
            try
            {
                // Clean up expired approvals first
                await _bingoService.CleanupExpiredApprovalsAsync();

                var pendingApprovals = await _bingoService.GetPendingApprovalsAsync();
                await Clients.Caller.SendAsync("PendingApprovalsList", pendingApprovals);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending approvals");
                await Clients.Caller.SendAsync("Error", "Failed to retrieve pending approvals");
            }
        }
    }
}
