using System.ComponentModel.DataAnnotations;

namespace BingoBoard.Data;

/// <summary>
/// Database entity for a bingo square template
/// </summary>
public class BingoSquareEntity
{
    /// <summary>
    /// Unique identifier for the bingo square (e.g., "damian-tbc")
    /// </summary>
    [Key]
    [MaxLength(100)]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Display text shown on the bingo square
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Optional categorization of the square (e.g., "bug", "quote", "dev", "oops", "meta")
    /// </summary>
    [MaxLength(50)]
    public string? Type { get; set; }

    /// <summary>
    /// Whether this square is currently active and can appear on boards
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order for admin management
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// When the square was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the square was last modified
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
