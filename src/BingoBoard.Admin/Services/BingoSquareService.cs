using BingoBoard.Admin.Endpoints;
using System.Text.Json;

namespace BingoBoard.Admin.Services;

/// <summary>
/// Service for managing bingo squares in file storage
/// </summary>
public interface IBingoSquareService
{
    Task<List<BingoSquareDto>> GetAllSquaresAsync();
    Task<BingoSquareDto?> GetSquareByIdAsync(string id);
    Task<BingoSquareDto> CreateSquareAsync(BingoSquareDto square);
    Task<BingoSquareDto?> UpdateSquareAsync(string id, BingoSquareDto square);
    Task<bool> DeleteSquareAsync(string id);
}

/// <summary>
/// Implementation of bingo square service using file-based storage
/// </summary>
public class BingoSquareService : IBingoSquareService
{
    private static readonly string DataFilePath = Path.Combine("Data", "bingo-squares.json");
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly ILogger<BingoSquareService> _logger;

    public BingoSquareService(ILogger<BingoSquareService> logger)
    {
        _logger = logger;
    }

    public async Task<List<BingoSquareDto>> GetAllSquaresAsync()
    {
        try
        {
            _logger.LogInformation("Getting all bingo squares from file");
            var squares = await ReadSquaresFromFileAsync();
            squares = squares.OrderBy(s => s.Type).ThenBy(s => s.Label).ToList();
            _logger.LogInformation("Retrieved {Count} bingo squares from file", squares.Count);
            return squares;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bingo squares from file");
            return new List<BingoSquareDto>();
        }
    }

    public async Task<BingoSquareDto?> GetSquareByIdAsync(string id)
    {
        try
        {
            _logger.LogInformation("Getting bingo square {Id} from file", id);
            var squares = await ReadSquaresFromFileAsync();
            return squares.FirstOrDefault(s => s.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving bingo square {Id}", id);
            return null;
        }
    }

    public async Task<BingoSquareDto> CreateSquareAsync(BingoSquareDto square)
    {
        try
        {
            _logger.LogInformation("Creating bingo square {Id}", square.Id);
            var squares = await ReadSquaresFromFileAsync();
            
            if (squares.Any(s => s.Id == square.Id))
            {
                throw new InvalidOperationException($"Square with ID {square.Id} already exists");
            }

            squares.Add(square);
            await WriteSquaresToFileAsync(squares);
            
            _logger.LogInformation("Successfully created bingo square {Id}", square.Id);
            return square;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bingo square");
            throw;
        }
    }

    public async Task<BingoSquareDto?> UpdateSquareAsync(string id, BingoSquareDto square)
    {
        try
        {
            _logger.LogInformation("Updating bingo square {Id}", id);
            var squares = await ReadSquaresFromFileAsync();
            
            var existingSquare = squares.FirstOrDefault(s => s.Id == id);
            if (existingSquare == null)
            {
                return null;
            }

            squares.Remove(existingSquare);
            square.Id = id; // Ensure ID doesn't change
            squares.Add(square);
            await WriteSquaresToFileAsync(squares);
            
            _logger.LogInformation("Successfully updated bingo square {Id}", id);
            return square;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bingo square {Id}", id);
            throw;
        }
    }

    public async Task<bool> DeleteSquareAsync(string id)
    {
        try
        {
            _logger.LogInformation("Deleting bingo square {Id}", id);
            var squares = await ReadSquaresFromFileAsync();
            
            var squareToDelete = squares.FirstOrDefault(s => s.Id == id);
            if (squareToDelete == null)
            {
                return false;
            }

            squares.Remove(squareToDelete);
            await WriteSquaresToFileAsync(squares);
            
            _logger.LogInformation("Successfully deleted bingo square {Id}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bingo square {Id}", id);
            throw;
        }
    }

    private static async Task<List<BingoSquareDto>> ReadSquaresFromFileAsync()
    {
        await FileLock.WaitAsync();
        try
        {
            if (!File.Exists(DataFilePath))
            {
                return new List<BingoSquareDto>();
            }

            var json = await File.ReadAllTextAsync(DataFilePath);
            return JsonSerializer.Deserialize<List<BingoSquareDto>>(json, JsonOptions) ?? new List<BingoSquareDto>();
        }
        finally
        {
            FileLock.Release();
        }
    }

    private static async Task WriteSquaresToFileAsync(List<BingoSquareDto> squares)
    {
        await FileLock.WaitAsync();
        try
        {
            var directory = Path.GetDirectoryName(DataFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(squares, JsonOptions);
            await File.WriteAllTextAsync(DataFilePath, json);
        }
        finally
        {
            FileLock.Release();
        }
    }
}
