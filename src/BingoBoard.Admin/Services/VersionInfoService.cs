using System.Reflection;
using System.Runtime.InteropServices;

namespace BingoBoard.Admin.Services;

public interface IVersionInfoService
{
    string GetCommitHash();
    string GetDotNetVersion();
    string GetAspireVersion();
    string GetBuildTime();
}

public class VersionInfoService : IVersionInfoService
{
    private readonly string _commitHash;
    private readonly string _buildTime;

    public VersionInfoService()
    {
        // Try to get commit hash from environment variable (set during CI/CD)
        _commitHash = Environment.GetEnvironmentVariable("COMMIT_SHA") ?? "unknown";
        
        // Try to get build time from environment variable or use assembly date
        _buildTime = Environment.GetEnvironmentVariable("BUILD_TIME") ?? 
                     DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss 'GMT'");
    }

    public string GetCommitHash()
    {
        // If we have a full hash, return short version (first 7 chars)
        if (_commitHash.Length > 7 && _commitHash != "unknown")
        {
            return _commitHash.Substring(0, 7);
        }
        return _commitHash;
    }

    public string GetDotNetVersion()
    {
        return RuntimeInformation.FrameworkDescription.Replace(".NET ", "");
    }

    public string GetAspireVersion()
    {
        // Try to find Aspire version from loaded assemblies
        var aspireAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name?.StartsWith("Aspire.") == true);
        
        if (aspireAssembly != null)
        {
            var version = aspireAssembly.GetName().Version;
            if (version != null)
            {
                return $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }
        
        // Fallback to checking package reference version (this would need to be set at build time)
        return Environment.GetEnvironmentVariable("ASPIRE_VERSION") ?? "13.0.0-preview.1";
    }

    public string GetBuildTime()
    {
        return _buildTime;
    }
}
