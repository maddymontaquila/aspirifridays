using System.Text.Json;

var adminUrl =
    Environment.GetEnvironmentVariable("services__boardadmin__https__0") ??
    Environment.GetEnvironmentVariable("services__boardadmin__http__0") ??
    throw new InvalidOperationException("The boardadmin endpoint is unavailable.");

using var client = new HttpClient
{
    BaseAddress = new Uri(adminUrl)
};

Console.WriteLine(
    """
    AspiriFridays Producer Console
    Commands: status, call <square-id>, clear <square-id>, help, quit
    """);

while (true)
{
    Console.Write("\nproducer> ");

    var input = Console.ReadLine()?.Trim();
    if (string.IsNullOrEmpty(input))
    {
        continue;
    }

    var parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
    var command = parts[0].ToLowerInvariant();
    var argument = parts.Length == 2 ? parts[1] : null;

    try
    {
        switch (command)
        {
            case "status":
            {
                using var response = await client.GetAsync("/api/demo/producer/status");
                response.EnsureSuccessStatusCode();
                using var status = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync());
                var root = status.RootElement;
                var connectedPlayers = root.GetProperty("connectedPlayers").GetInt32();
                var pendingApprovals = root.GetProperty("pendingApprovals").GetInt32();
                var calledSquares = root.GetProperty("calledSquares")
                    .EnumerateArray()
                    .Select(square => square.GetString())
                    .Where(square => square is not null)
                    .ToArray();

                Console.WriteLine(
                    $"{connectedPlayers} players connected | " +
                    $"{pendingApprovals} approvals pending");

                Console.WriteLine(calledSquares.Length > 0
                    ? $"Called: {string.Join(", ", calledSquares)}"
                    : "No squares called yet.");
                break;
            }

            case "call" when argument is not null:
                await SetSquareStateAsync(argument, true);
                break;

            case "clear" when argument is not null:
                await SetSquareStateAsync(argument, false);
                break;

            case "help":
                Console.WriteLine("status | call <square-id> | clear <square-id> | quit");
                break;

            case "quit":
            case "exit":
                return;

            default:
                Console.WriteLine("Unknown command. Enter 'help'.");
                break;
        }
    }
    catch (HttpRequestException exception)
    {
        Console.WriteLine($"Request failed: {exception.Message}");
    }
}

async Task SetSquareStateAsync(string squareId, bool isChecked)
{
    using var response = await client.PostAsync(
        $"/api/demo/producer/squares/{Uri.EscapeDataString(squareId)}/state/{isChecked}",
        content: null);

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine($"Failed: {await response.Content.ReadAsStringAsync()}");
        return;
    }

    using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
    var label = result.RootElement.GetProperty("label").GetString() ?? squareId;

    Console.WriteLine(
        $"OK: \"{label}\" {(isChecked ? "called" : "cleared")}");
}
