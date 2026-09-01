using System.Reflection;
using Aspire.Hosting;

internal sealed record BuildInfo(
    string AspireVersion,
    string DotnetVersion,
    string CommitSha,
    string ViteVersion)
{
    public static BuildInfo Load(IDistributedApplicationBuilder builder)
    {
        var aspireAssembly = typeof(IDistributedApplicationBuilder).Assembly;
        var aspireVersion = Environment.GetEnvironmentVariable("ASPIRE_VERSION")
            ?? aspireAssembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion
                ?.Split('+')[0]
            ?? aspireAssembly.GetName().Version?.ToString(3)
            ?? "unknown";
        var dotnetVersion = Environment.GetEnvironmentVariable("DOTNET_VERSION")
            ?? Environment.Version.ToString();
        var commitSha = Environment.GetEnvironmentVariable("COMMIT_SHA") ?? "dev";
        var viteVersion = GetViteVersion(
            Path.Combine(builder.Environment.ContentRootPath, "bingo-board", "package.json"));

        return new BuildInfo(aspireVersion, dotnetVersion, commitSha, viteVersion);
    }

    private static string GetViteVersion(string packageJsonPath)
    {
        if (!File.Exists(packageJsonPath))
        {
            return "dev";
        }

        using var stream = File.OpenRead(packageJsonPath);
        using var document = System.Text.Json.JsonDocument.Parse(stream);
        if (!document.RootElement.TryGetProperty("devDependencies", out var devDependencies)
            || !devDependencies.TryGetProperty("vite", out var viteVersionProperty))
        {
            return "dev";
        }

        return viteVersionProperty.GetString()?.TrimStart('^', '~') ?? "dev";
    }
}
