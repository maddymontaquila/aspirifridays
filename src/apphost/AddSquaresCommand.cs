using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

internal static class AddSquaresCommand
{
    public static IResourceBuilder<T> WithImportSquaresCommand<T>(
        this IResourceBuilder<T> resource)
        where T : IResourceWithEndpoints
    {
        return resource.WithCommand(
            "import-squares",
            "Import bingo squares",
            async context =>
            {
                if (!context.Arguments.TryGetByName("file", out var input) ||
                    input.Files is not [var file])
                {
                    return CommandResults.Failure("Select one JSON file.");
                }

                var adminUrl = await resource.GetEndpoint("https")
                    .GetValueAsync(context.CancellationToken);

                if (string.IsNullOrWhiteSpace(adminUrl))
                {
                    return CommandResults.Failure("The admin endpoint is unavailable.");
                }

                using var client = new HttpClient();
                using var content = new StreamContent(file.OpenRead());
                content.Headers.ContentType =
                    new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                using var response = await client.PostAsync(
                    $"{adminUrl}/api/demo/producer/squares/import?mode=merge",
                    content,
                    context.CancellationToken);

                var result = await response.Content.ReadAsStringAsync(
                    context.CancellationToken);

                return response.IsSuccessStatusCode
                    ? CommandResults.Success(
                        "Bingo squares imported.",
                        result,
                        CommandResultFormat.Json)
                    : CommandResults.Failure(result);
            },
            new CommandOptions
            {
                Description = "Merge bingo squares from a JSON file.",
                IconName = "ArrowUpload",
                IsHighlighted = true,
                Arguments =
                [
                    new InteractionInput
                    {
                        Name = "file",
                        Label = "Bingo squares",
                        InputType = InputType.File,
                        FileFilter = ".json",
                        MaxFileSize = 1024 * 1024,
                        Required = true
                    }
                ]
            });
    }
}
