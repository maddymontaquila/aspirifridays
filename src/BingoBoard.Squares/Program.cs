using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVueFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure JSON serializer options
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowVueFrontend");

// Load bingo squares data
var bingoSquaresJson = await File.ReadAllTextAsync("Data/bingoSquares.json");
var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
var allSquares = JsonSerializer.Deserialize<BingoSquare[]>(bingoSquaresJson, jsonOptions) ?? [];

// Get all bingo squares
app.MapGet("/api/squares", () => allSquares)
   .WithName("GetAllSquares")
   .WithOpenApi();

// Get a random subset of bingo squares (default 25 for a 5x5 board)
app.MapGet("/api/squares/random", (int count = 25) =>
{
    if (count <= 0 || count > allSquares.Length)
        return Results.BadRequest($"Count must be between 1 and {allSquares.Length}");

    // Always include the free space if it exists and count allows
    var freeSpace = allSquares.FirstOrDefault(s => s.Type == "free");
    var otherSquares = allSquares.Where(s => s.Type != "free").ToArray();
    
    var randomSquares = new List<BingoSquare>();
    
    if (freeSpace != null && count > 0)
    {
        randomSquares.Add(freeSpace);
        count--;
    }
    
    // Shuffle and take the remaining count
    var shuffled = otherSquares.OrderBy(x => Random.Shared.Next()).Take(count);
    randomSquares.AddRange(shuffled);
    
    return Results.Ok(randomSquares.OrderBy(x => Random.Shared.Next()).ToArray());
})
.WithName("GetRandomSquares")
.WithOpenApi();

// Get squares by type
app.MapGet("/api/squares/type/{type}", (string type) =>
{
    var filteredSquares = allSquares.Where(s => s.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToArray();
    return filteredSquares.Length > 0 ? Results.Ok(filteredSquares) : Results.NotFound($"No squares found with type '{type}'");
})
.WithName("GetSquaresByType")
.WithOpenApi();

app.MapDefaultEndpoints();

app.Run();

record BingoSquare(string Id, string Label, string Type);
