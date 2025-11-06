namespace BingoBoard.Admin.Models;

/// <summary>
/// Represents a complete bingo set for a client
/// </summary>
public class BingoSet
{
    /// <summary>
    /// Unique identifier for the bingo set
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Client ID this set belongs to
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// List of bingo squares in this set (typically 25 for a 5x5 grid)
    /// </summary>
    public List<BingoSquare> Squares { get; set; } = [];

    /// <summary>
    /// When this bingo set was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time this set was updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether this bingo set has achieved a winning condition
    /// </summary>
    public bool HasWin { get; set; }
}
