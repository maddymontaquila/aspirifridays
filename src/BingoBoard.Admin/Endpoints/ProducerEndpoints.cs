using BingoBoard.Admin.Hubs;
using BingoBoard.Admin.Models;
using BingoBoard.Admin.Services;
using Microsoft.AspNetCore.SignalR;

namespace BingoBoard.Admin.Endpoints;

public static class ProducerEndpoints
{
    public static IEndpointRouteBuilder MapProducerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/demo/producer");

        group.MapPost("/squares/import", ImportSquaresAsync);
        group.MapGet("/status", GetStatusAsync);
        group.MapPost("/squares/{squareId}/state/{isChecked:bool}", SetSquareStateAsync);

        return endpoints;
    }

    private static async Task<IResult> ImportSquaresAsync(
        List<BingoSquareImport> squares,
        string? mode,
        IBingoService bingoService,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<BingoImportMode>(
                mode ?? "merge",
                ignoreCase: true,
                out var importMode))
        {
            return Results.BadRequest("Mode must be 'merge' or 'replace'.");
        }

        try
        {
            var result = await bingoService.ImportSquaresAsync(
                squares,
                importMode,
                cancellationToken);
            return Results.Ok(result);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(exception.Message);
        }
    }

    private static async Task<IResult> GetStatusAsync(
        IClientConnectionService clientService,
        IBingoService bingoService)
    {
        var clients = await clientService.GetAllClientsAsync();
        var approvals = await bingoService.GetPendingApprovalsAsync();
        var calledSquares = await bingoService.GetGloballyCheckedSquaresAsync();

        return Results.Ok(new
        {
            ConnectedPlayers = clients.Count,
            PendingApprovals = approvals.Count,
            CalledSquares = calledSquares
        });
    }

    private static async Task<IResult> SetSquareStateAsync(
        string squareId,
        bool isChecked,
        IBingoService bingoService,
        IHubContext<BingoHub> hubContext,
        CancellationToken cancellationToken)
    {
        var square = (await bingoService.GetAllSquaresAsync())
            .FirstOrDefault(square =>
                string.Equals(square.Id, squareId, StringComparison.OrdinalIgnoreCase));

        if (square is null)
        {
            return Results.NotFound($"Square '{squareId}' was not found.");
        }

        if (!await bingoService.UpdateSquareGloballyAsync(square.Id, isChecked))
        {
            return Results.Problem("The square could not be updated.");
        }

        await hubContext.Clients.All.SendAsync(
            "GlobalSquareUpdate",
            new
            {
                SquareId = square.Id,
                IsChecked = isChecked,
                Timestamp = DateTime.UtcNow,
                Message = $"'{square.Label}' was {(isChecked ? "called" : "cleared")} from the producer console"
            },
            cancellationToken);

        return Results.Ok(new
        {
            square.Id,
            square.Label,
            IsChecked = isChecked
        });
    }
}
