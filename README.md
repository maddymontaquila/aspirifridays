# aspirifridays

AspiriFridays bingo

## Version Information

Both the admin and client applications display version information in their footers, including:
- Commit SHA (short hash)
- .NET version
- .NET Aspire version
- Vite version (client app only)
- Build/render timestamp

### Setting Version Information

The version information is automatically populated during CI/CD deployment via environment variables set in the GitHub Actions workflow. The Aspire version is dynamically extracted from the `apphost.cs` file, and the Vite version is automatically read from `package.json`.

**Requirements:**
- .NET 10 RC or later (required for `dotnet build` command with apphost.cs)

For local development, you can set these environment variables before running the application:

```bash
# Extract Aspire version from apphost.cs
ASPIRE_VERSION=$(grep -o '#:sdk Aspire.AppHost.Sdk@.*' ./src/apphost.cs | cut -d'@' -f2 | grep -oP '^\d+\.\d+\.\d+(-preview\.\d+)?')

# For the admin app
export COMMIT_SHA=$(git rev-parse HEAD)
export DOTNET_VERSION=$(dotnet --version)
export ASPIRE_VERSION="$ASPIRE_VERSION"
export BUILD_TIME=$(date -u +"%a, %d %b %Y %H:%M:%S GMT")

# For the client app (Vue.js) - needs VITE_ prefix
export VITE_COMMIT_SHA=$(git rev-parse HEAD)
export VITE_DOTNET_VERSION=$(dotnet --version)
export VITE_ASPIRE_VERSION="$ASPIRE_VERSION"
# Note: Vite version is automatically extracted from package.json
```

Or create a `.env` file in the `src/bingo-board` directory based on `.env.example`.

**Note:** 
- The Aspire version is automatically extracted from the `#:sdk` directive in `apphost.cs` during CI/CD deployment.
- The Vite version is automatically read from `package.json` during the build process.
