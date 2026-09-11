namespace BingoBoard.Admin.Models;

public sealed class BingoSquareImport
{
    public required string Id { get; init; }

    public required string Label { get; init; }

    public string? Type { get; init; }

    public bool IsActive { get; init; } = true;
}

public enum BingoImportMode
{
    Merge,
    Replace
}

public sealed record BingoImportResult(int Added, int Updated, int Total);
