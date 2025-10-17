# aspirifridays

AspiriFridays bingo

## Version Information

Both the admin and client applications display version information in their footers, including:
- Commit SHA (short hash)
- .NET version
- .NET Aspire version
- Build/render timestamp

### Setting Version Information

The version information is automatically populated during CI/CD deployment via environment variables set in the GitHub Actions workflow.

For local development, you can set these environment variables before running the application:

```bash
# For the admin app
export COMMIT_SHA=$(git rev-parse HEAD)
export DOTNET_VERSION=$(dotnet --version)
export ASPIRE_VERSION="13.0.0"
export BUILD_TIME=$(date -u +"%a, %d %b %Y %H:%M:%S GMT")

# For the client app (Vue.js) - needs VITE_ prefix
export VITE_COMMIT_SHA=$(git rev-parse HEAD)
export VITE_DOTNET_VERSION=$(dotnet --version)
export VITE_ASPIRE_VERSION="13.0.0"
```

Or create a `.env` file in the `src/bingo-board` directory based on `.env.example`.
