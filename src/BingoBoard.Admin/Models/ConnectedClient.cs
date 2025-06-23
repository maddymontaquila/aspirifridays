namespace BingoBoard.Admin.Models
{
    /// <summary>
    /// Represents a connected client
    /// </summary>
    public class ConnectedClient
    {
        /// <summary>
        /// SignalR connection ID
        /// </summary>
        public string ConnectionId { get; set; } = string.Empty;

        /// <summary>
        /// Optional user identifier or name
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// When the client connected
        /// </summary>
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last activity timestamp
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Current bingo set ID for this client
        /// </summary>
        public string? CurrentBingoSetId { get; set; }

        /// <summary>
        /// Client IP address (if available)
        /// </summary>
        public string? IpAddress { get; set; }

        /// <summary>
        /// User agent string (if available)
        /// </summary>
        public string? UserAgent { get; set; }
    }
}
