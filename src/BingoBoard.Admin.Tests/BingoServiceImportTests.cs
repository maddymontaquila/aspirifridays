using BingoBoard.Admin.Models;
using BingoBoard.Admin.Services;
using BingoBoard.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BingoBoard.Admin.Tests;

public sealed class BingoServiceImportTests
{
    [Fact]
    public async Task ImportSquaresAsync_MergeUpdatesExistingAndAddsNormalizedSquare()
    {
        await using var database = await TestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        database.Context.BingoSquares.AddRange(
            CreateEntity("existing", "Old label", 3, now),
            CreateEntity("retained", "Retained", 7, now));
        await database.Context.SaveChangesAsync();

        var service = CreateService(database.Context);

        var result = await service.ImportSquaresAsync(
        [
            new BingoSquareImport
            {
                Id = "existing",
                Label = "Updated label",
                Type = "QUOTE",
                IsActive = false
            },
            new BingoSquareImport
            {
                Id = " New Square! ",
                Label = "A new square",
                Type = "DEV"
            }
        ],
            BingoImportMode.Merge);

        Assert.Equal(new BingoImportResult(Added: 1, Updated: 1, Total: 2), result);

        var squares = await database.Context.BingoSquares
            .OrderBy(square => square.DisplayOrder)
            .ToListAsync();
        Assert.Equal(3, squares.Count);

        var existing = Assert.Single(squares, square => square.Id == "existing");
        Assert.Equal("Updated label", existing.Label);
        Assert.Equal("quote", existing.Type);
        Assert.False(existing.IsActive);
        Assert.Equal(3, existing.DisplayOrder);

        var added = Assert.Single(squares, square => square.Id == "new-square");
        Assert.Equal("A new square", added.Label);
        Assert.Equal("dev", added.Type);
        Assert.True(added.IsActive);
        Assert.Equal(8, added.DisplayOrder);
    }

    [Fact]
    public async Task ImportSquaresAsync_ReplaceRemovesExistingCatalog()
    {
        await using var database = await TestDatabase.CreateAsync();
        var now = DateTime.UtcNow;
        database.Context.BingoSquares.AddRange(
            CreateEntity("old-one", "Old one", 4, now),
            CreateEntity("old-two", "Old two", 5, now));
        await database.Context.SaveChangesAsync();

        var service = CreateService(database.Context);

        var result = await service.ImportSquaresAsync(
        [
            new BingoSquareImport
            {
                Id = "replacement",
                Label = "Replacement",
                Type = "meta"
            }
        ],
            BingoImportMode.Replace);

        Assert.Equal(new BingoImportResult(Added: 1, Updated: 0, Total: 1), result);

        database.Context.ChangeTracker.Clear();
        var replacement = Assert.Single(await database.Context.BingoSquares.ToListAsync());
        Assert.Equal("replacement", replacement.Id);
        Assert.Equal(0, replacement.DisplayOrder);
    }

    [Fact]
    public async Task ImportSquaresAsync_RejectsIdsThatCollideAfterNormalization()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ImportSquaresAsync(
            [
                new BingoSquareImport { Id = "Demo Square", Label = "First" },
                new BingoSquareImport { Id = "demo-square", Label = "Second" }
            ],
                BingoImportMode.Merge));

        Assert.Contains("duplicate ID 'demo-square'", exception.Message);
        Assert.Empty(await database.Context.BingoSquares.ToListAsync());
    }

    [Fact]
    public async Task ImportSquaresAsync_RejectsEmptyImport()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = CreateService(database.Context);

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ImportSquaresAsync([], BingoImportMode.Merge));

        Assert.Contains("at least one square", exception.Message);
    }

    private static BingoService CreateService(ApplicationDbContext context) =>
        new(
            cache: null!,
            logger: NullLogger<BingoService>.Instance,
            clientService: null!,
            dbContext: context);

    private static BingoSquareEntity CreateEntity(
        string id,
        string label,
        int displayOrder,
        DateTime timestamp) =>
        new()
        {
            Id = id,
            Label = label,
            IsActive = true,
            DisplayOrder = displayOrder,
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;

        private TestDatabase(SqliteConnection connection, ApplicationDbContext context)
        {
            this.connection = connection;
            Context = context;
        }

        public ApplicationDbContext Context { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
