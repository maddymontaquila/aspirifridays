namespace BingoBoard.Admin.Models;

/// <summary>
/// Represents a pending approval request from a client
/// </summary>
public class PendingApproval
{
    /// <summary>
    /// Unique identifier for the approval request
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Client connection ID who requested the approval
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// The square ID that was marked for approval
    /// </summary>
    public string SquareId { get; set; } = string.Empty;

    /// <summary>
    /// The square label for display purposes
    /// </summary>
    public string SquareLabel { get; set; } = string.Empty;

    /// <summary>
    /// Whether the client wants to check or uncheck the square
    /// </summary>
    public bool RequestedState { get; set; }

    /// <summary>
    /// When the approval request was created
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Current status of the approval request
    /// </summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    /// <summary>
    /// Admin who processed the approval (if any)
    /// </summary>
    public string? ProcessedByAdmin { get; set; }

    /// <summary>
    /// When the approval was processed (if any)
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Optional reason for denial
    /// </summary>
    public string? DenialReason { get; set; }
}

/// <summary>
/// Status of an approval request
/// </summary>
public enum ApprovalStatus
{
    Pending,
    Approved,
    Denied,
    Expired
}
