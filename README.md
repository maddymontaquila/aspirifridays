# AspiriFridays Bingo 🎯

A real-time multiplayer bingo app for live streams, built with Aspire.

## What is this?

A bingo board that viewers can play along with during a live stream. Each player gets a randomized 5x5 bingo board with stream-related squares. The host can run it in **Live Mode** (squares require admin approval) or **Free Play** (mark freely).

## Tech Stack

- **Orchestration:** Aspire
- **Backend:** ASP.NET Core (Blazor) + SignalR
- **Frontend:** Vue 3 + Vite
- **Database:** PostgreSQL
- **Cache:** Redis
- **Mobile:** .NET MAUI Hybrid

## Running Locally

**Prerequisites:** A stable .NET 10 SDK, Aspire CLI daily `17.1.0-preview.1.26558.1`,
Node.js, Docker, and Chrome or Edge for browser logs.

```bash
aspire secret set Parameters:admin-password "<your-local-demo-password>"
aspire start
```

That's it. Aspire handles spinning up PostgreSQL, Redis, the admin panel, and the Vue client.

## .NET Conf demo (Aspire 17 daily)

This branch builds on the Aspire 13.6.4 demo, retaining browser logs, EF Core commands,
the migration worker, and the opt-in MAUI clients. The AppHost SDK and all Aspire packages
are pinned to `17.1.0-preview.1.26558.1` (the current Aspire 17 daily).
`NuGet.config` maps only `Aspire.*` to the public `dotnet9` daily feed; other packages
continue to use NuGet.org.

Install the daily CLI with `aspire update --self --channel daily --yes --non-interactive`.
For this pinned demo, verify `aspire --version` reports `17.1.0-preview.1.26558.1`
(source commit `01d9682d38aa93b9df020fbc767529ca96096893`); a later daily may differ.
`#:property AspireUseCliBundle=true` uses the CLI's bundled dashboard and orchestration tools
instead of downloading separate runtime packs.

Use `aspire start --isolated` in a worktree; supply the admin password in that isolated
run's dashboard when prompted.

1. Open `bingoboard-dev` for the player board and `boardadmin` for the Blazor admin panel.
2. Under `bingoboard-dev-browser-logs`, choose **Open tracked browser**, then interact
   with the player board to see browser console and network events in the dashboard.
3. Open `ef-migrations` for **Get Database Status** or **Update Database**.
   Migrations live in `BingoBoard.Data`; commands use the running app's database connection.
   The existing migration service still applies migrations and seeds the admin user and squares
   at startup. Do not remove migrations or drop the database during the live demo.
4. Use the `producer-console` terminal: `status`, `call <square-id>`, `clear <square-id>`.
   Aspire 17 adds the existing file-based `producerconsole.cs` with `AddDotnetProject`
   from `Aspire.Hosting.Dotnet`, replacing `AddCSharpApp`.
   This local demo terminal is excluded from the publish manifest because
   `DotnetProjectResource` does not support automatic .NET project publishing.

Azure publishing configuration and the opt-in MAUI launch profile are preserved.
SignalR reuses the Aspire-managed Redis connection so the backplane shares the
integration's TLS certificate trust and connection configuration.
The `bingoboard` YARP resource remains explicitly started; the local player board runs on Vite.
`global.json` selects a stable .NET 10 SDK, and the EF tool is pinned to the app's EF Core
version so a newer installed SDK or tool cannot change the demo's target framework.

## Project Structure

```
src/
├── apphost.cs                    # Aspire orchestrator
├── bingo-board/                  # Vue.js player client
├── BingoBoard.Admin/             # Blazor admin panel + SignalR hub
├── BingoBoard.MigrationService/  # DB migrations
├── BingoBoard.MauiHybrid/        # Mobile/desktop app
└── BingoBoard.ServiceDefaults/   # Shared service defaults
```

## How It Works

1. Players connect to the Vue client and get a randomized bingo board
2. Admin can toggle between **Live Mode** and **Free Play**
3. In Live Mode, square markings require approval from the admin
4. SignalR keeps everything in sync in real-time
5. Get 5 in a row → bingo! 🎉
