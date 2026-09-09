using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;

internal static class AddSquaresCommand
{
    public static IResourceBuilder<T> WithImportSquaresCommand<T>(
        this IResourceBuilder<T> resource)
        where T : IResourceWithEndpoints
    {
        return resource.WithHttpCommand(
            path: "/api/demo/producer/squares/import",
            displayName: "Import bingo squares",
            commandName: "import-squares",
            commandOptions: new HttpCommandOptions
            {
                Description = "Import bingo squares from a JSON file.",
                IconName = "ArrowUpload",
                Method = HttpMethod.Post,
                ResultMode = HttpCommandResultMode.Auto,
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
                    },
                    new InteractionInput
                    {
                        Name = "mode",
                        Label = "Import mode",
                        Description = "Merge preserves existing squares; replace removes them first.",
                        InputType = InputType.Choice,
                        Value = "merge",
                        Required = true,
                        Options =
                        [
                            new("merge", "Merge with existing squares"),
                            new("replace", "Replace all squares")
                        ]
                    }
                ],
                PrepareRequest = context =>
                {
                    if (!context.Arguments.TryGetByName("file", out var input) ||
                        input.Files is not [var file])
                    {
                        throw new InvalidOperationException("Select one JSON file.");
                    }

                    var mode = context.Arguments.GetString("mode") ?? "merge";
                    var requestUri = new UriBuilder(context.Request.RequestUri!)
                    {
                        Query = $"mode={Uri.EscapeDataString(mode)}"
                    };

                    context.Request.RequestUri = requestUri.Uri;
                    context.Request.Content = new StreamContent(file.OpenRead());
                    context.Request.Content.Headers.ContentType =
                        new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                    return Task.CompletedTask;
                }
            });
    }
}
