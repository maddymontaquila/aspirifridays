using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace BingoBoard.Admin.Endpoints;

/// <summary>
/// Extension methods for mapping bingo square CRUD endpoints
/// </summary>
public static class BingoSquareCrudEndpoints
{
    private static readonly string DataFilePath = Path.Combine("Data", "bingo-squares.json");
    private static readonly SemaphoreSlim FileLock = new(1, 1);
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    /// <summary>
    /// Maps all bingo square CRUD endpoints to the web application
    /// </summary>
    /// <param name="app">The web application instance</param>
    /// <returns>The web application instance for method chaining</returns>
    public static WebApplication MapBingoSquareCrudEndpoints(this WebApplication app)
    {
        var squaresGroup = app.MapGroup("/api/bingo-squares")
            .RequireAuthorization()
            .WithTags("Bingo Squares");

        squaresGroup.MapGet("/", GetAllSquaresHandler)
            .WithName("GetAllBingoSquares")
            .WithSummary("Get all bingo squares")
            .WithDescription("Retrieves all available bingo squares from file storage");

        squaresGroup.MapGet("/{id}", GetSquareByIdHandler)
            .WithName("GetBingoSquareById")
            .WithSummary("Get a specific bingo square")
            .WithDescription("Retrieves a bingo square by its ID");

        squaresGroup.MapPost("/", CreateSquareHandler)
            .WithName("CreateBingoSquare")
            .WithSummary("Create a new bingo square")
            .WithDescription("Creates a new bingo square in file storage");

        squaresGroup.MapPut("/{id}", UpdateSquareHandler)
            .WithName("UpdateBingoSquare")
            .WithSummary("Update an existing bingo square")
            .WithDescription("Updates an existing bingo square in file storage");

        squaresGroup.MapDelete("/{id}", DeleteSquareHandler)
            .WithName("DeleteBingoSquare")
            .WithSummary("Delete a bingo square")
            .WithDescription("Deletes a bingo square from file storage");

        return app;
    }

    private static async Task<List<BingoSquareDto>> ReadSquaresFromFileAsync()
    {
        await FileLock.WaitAsync();
        try
        {
            if (!File.Exists(DataFilePath))
            {
                return [];
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

    /// <summary>
    /// Gets all bingo squares from file storage
    /// </summary>
    /// <param name="logger">The logger instance</param>
    /// <returns>A list of all bingo squares</returns>
    internal static async Task<Results<Ok<List<BingoSquareDto>>, ProblemHttpResult>> GetAllSquaresHandler(
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("API request: Getting all bingo squares from file");
            
            var squares = await ReadSquaresFromFileAsync();
            squares = squares.OrderBy(s => s.Type).ThenBy(s => s.Label).ToList();

            logger.LogInformation("Retrieved {Count} bingo squares from file", squares.Count);
            return TypedResults.Ok(squares);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving bingo squares from file");
            return TypedResults.Problem("Failed to retrieve bingo squares", statusCode: 500);
        }
    }

    /// <summary>
    /// Gets a specific bingo square by ID
    /// </summary>
    /// <param name="id">The square ID</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>The requested bingo square or not found</returns>
    internal static async Task<Results<Ok<BingoSquareDto>, NotFound, ProblemHttpResult>> GetSquareByIdHandler(
        string id,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("API request: Getting bingo square with ID {Id}", id);
            
            var squares = await ReadSquaresFromFileAsync();
            var square = squares.FirstOrDefault(s => s.Id == id);

            if (square == null)
            {
                logger.LogWarning("Bingo square with ID {Id} not found", id);
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(square);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving bingo square {Id}", id);
            return TypedResults.Problem("Failed to retrieve bingo square", statusCode: 500);
        }
    }

    /// <summary>
    /// Creates a new bingo square
    /// </summary>
    /// <param name="request">The square creation request</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>The created bingo square</returns>
    internal static async Task<Results<Created<BingoSquareDto>, BadRequest<string>, ProblemHttpResult>> CreateSquareHandler(
        [FromBody] CreateBingoSquareRequest request,
        ILogger<Program> logger)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return TypedResults.BadRequest("Square ID is required");
            }

            if (string.IsNullOrWhiteSpace(request.Label))
            {
                return TypedResults.BadRequest("Square label is required");
            }

            var squares = await ReadSquaresFromFileAsync();

            // Check if square with this ID already exists
            if (squares.Any(s => s.Id == request.Id))
            {
                logger.LogWarning("Attempt to create bingo square with duplicate ID {Id}", request.Id);
                return TypedResults.BadRequest($"A square with ID '{request.Id}' already exists");
            }

            logger.LogInformation("Creating new bingo square with ID {Id}", request.Id);

            var dto = new BingoSquareDto
            {
                Id = request.Id,
                Label = request.Label,
                Type = request.Type
            };

            squares.Add(dto);
            await WriteSquaresToFileAsync(squares);

            logger.LogInformation("Successfully created bingo square {Id}", request.Id);

            return TypedResults.Created($"/api/bingo-squares/{dto.Id}", dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating bingo square");
            return TypedResults.Problem("Failed to create bingo square", statusCode: 500);
        }
    }

    /// <summary>
    /// Updates an existing bingo square
    /// </summary>
    /// <param name="id">The square ID to update</param>
    /// <param name="request">The square update request</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>The updated bingo square</returns>
    internal static async Task<Results<Ok<BingoSquareDto>, NotFound, BadRequest<string>, ProblemHttpResult>> UpdateSquareHandler(
        string id,
        [FromBody] UpdateBingoSquareRequest request,
        ILogger<Program> logger)
    {
        try
        {
            // Validate request
            if (string.IsNullOrWhiteSpace(request.Label))
            {
                return TypedResults.BadRequest("Square label is required");
            }

            logger.LogInformation("Updating bingo square with ID {Id}", id);

            var squares = await ReadSquaresFromFileAsync();
            var square = squares.FirstOrDefault(s => s.Id == id);
            
            if (square == null)
            {
                logger.LogWarning("Bingo square with ID {Id} not found", id);
                return TypedResults.NotFound();
            }

            square.Label = request.Label;
            square.Type = request.Type;

            await WriteSquaresToFileAsync(squares);

            logger.LogInformation("Successfully updated bingo square {Id}", id);

            return TypedResults.Ok(square);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating bingo square {Id}", id);
            return TypedResults.Problem("Failed to update bingo square", statusCode: 500);
        }
    }

    /// <summary>
    /// Deletes a bingo square
    /// </summary>
    /// <param name="id">The square ID to delete</param>
    /// <param name="logger">The logger instance</param>
    /// <returns>No content on success</returns>
    internal static async Task<Results<NoContent, NotFound, ProblemHttpResult>> DeleteSquareHandler(
        string id,
        ILogger<Program> logger)
    {
        try
        {
            logger.LogInformation("Deleting bingo square with ID {Id}", id);

            var squares = await ReadSquaresFromFileAsync();
            var square = squares.FirstOrDefault(s => s.Id == id);
            
            if (square == null)
            {
                logger.LogWarning("Bingo square with ID {Id} not found", id);
                return TypedResults.NotFound();
            }

            squares.Remove(square);
            await WriteSquaresToFileAsync(squares);

            logger.LogInformation("Successfully deleted bingo square {Id}", id);
            return TypedResults.NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting bingo square {Id}", id);
            return TypedResults.Problem("Failed to delete bingo square", statusCode: 500);
        }
    }
}

#region Request/Response Models

/// <summary>
/// DTO for bingo square responses
/// </summary>
public record BingoSquareDto
{
    [Required(ErrorMessage = "ID is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "ID must be between 1 and 100 characters")]
    public required string Id { get; set; }
    
    [Required(ErrorMessage = "Label is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Label must be between 1 and 200 characters")]
    public required string Label { get; set; }
    
    [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
    public string? Type { get; set; }
}

/// <summary>
/// Request model for creating a bingo square
/// </summary>
public record CreateBingoSquareRequest
{
    /// <summary>
    /// Unique identifier for the square (e.g., "coffee-mention")
    /// </summary>
    [Required(ErrorMessage = "ID is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "ID must be between 1 and 100 characters")]
    [RegularExpression(@"^[a-z0-9-]+$", ErrorMessage = "ID must contain only lowercase letters, numbers, and hyphens")]
    public required string Id { get; init; }

    /// <summary>
    /// Display label for the square (e.g., "Coffee mentioned")
    /// </summary>
    [Required(ErrorMessage = "Label is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Label must be between 1 and 200 characters")]
    public required string Label { get; init; }

    /// <summary>
    /// Optional type/category (e.g., "meta", "quote", "dev")
    /// </summary>
    [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
    public string? Type { get; init; }
}

/// <summary>
/// Request model for updating a bingo square
/// </summary>
public record UpdateBingoSquareRequest
{
    /// <summary>
    /// Display label for the square
    /// </summary>
    [Required(ErrorMessage = "Label is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Label must be between 1 and 200 characters")]
    public required string Label { get; init; }

    /// <summary>
    /// Optional type/category
    /// </summary>
    [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters")]
    public string? Type { get; init; }
}

#endregion
