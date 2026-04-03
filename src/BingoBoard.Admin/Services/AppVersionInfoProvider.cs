using System.Reflection;
using System.Text.RegularExpressions;

namespace BingoBoard.Admin.Services;

public sealed record AppVersionInfo(
    string CommitSha,
    string CommitHash,
    string CommitUrl,
    string DotNetVersion,
    string AspireVersion,
    string ViteVersion);

public sealed class AppVersionInfoProvider(IConfiguration configuration)
{
    private const string RepositoryUrl = "https://github.com/maddymontaquila/aspirifridays";
    private readonly AppVersionInfo versionInfo = BuildVersionInfo(configuration);

    public AppVersionInfo GetVersionInfo() => versionInfo;

    private static AppVersionInfo BuildVersionInfo(IConfiguration configuration)
    {
        var fullCommitSha = ResolveCommitSha(configuration);
        var commitHash = fullCommitSha.Length >= 7 ? fullCommitSha[..7] : "dev";
        var commitUrl = fullCommitSha.Length > 0
            ? $"{RepositoryUrl}/commit/{fullCommitSha}"
            : RepositoryUrl;

        return new AppVersionInfo(
            fullCommitSha,
            commitHash,
            commitUrl,
            ResolveValue(configuration["DOTNET_VERSION"], Environment.Version.ToString(), fallback: "dev"),
            ResolveValue(configuration["ASPIRE_VERSION"], fallback: "dev"),
            ResolveValue(configuration["VITE_VERSION"], fallback: "dev"));
    }

    private static string ResolveCommitSha(IConfiguration configuration)
    {
        var configuredValue = ResolveValue(configuration["COMMIT_SHA"]);
        if (configuredValue.Length > 0)
        {
            return configuredValue;
        }

        var informationalVersion = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        if (string.IsNullOrWhiteSpace(informationalVersion))
        {
            return string.Empty;
        }

        var match = Regex.Match(informationalVersion, @"(?<![0-9a-fA-F])[0-9a-fA-F]{7,40}(?![0-9a-fA-F])");
        return match.Success ? match.Value : string.Empty;
    }

    private static string ResolveValue(string? primaryValue, string? secondaryValue = null, string fallback = "")
    {
        if (!string.IsNullOrWhiteSpace(primaryValue) && !string.Equals(primaryValue, "dev", StringComparison.OrdinalIgnoreCase))
        {
            return primaryValue;
        }

        if (!string.IsNullOrWhiteSpace(secondaryValue) && !string.Equals(secondaryValue, "dev", StringComparison.OrdinalIgnoreCase))
        {
            return secondaryValue;
        }

        return fallback;
    }
}