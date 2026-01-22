namespace BingoBoard.Admin.Models;

/// <summary>
/// Represents a single bingo square with its properties
/// </summary>
public class BingoSquare
{
    /// <summary>
    /// Unique identifier for the bingo square
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display text shown on the bingo square
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional categorization of the square (e.g., bug, dev moment, inside joke)
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Whether this square is currently checked/completed
    /// </summary>
    public bool IsChecked { get; set; }

    /// <summary>
    /// Timestamp when the square was last updated
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}
