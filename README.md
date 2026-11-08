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

**Prerequisites:** .NET 10+, Aspire CLI 13.6.4, Node.js, Docker, and Chrome or Edge for browser logs.

```bash
aspire secret set Parameters:admin-password "<your-local-demo-password>"
aspire start
```

That's it. Aspire handles spinning up PostgreSQL, Redis, the admin panel, and the Vue client.

## .NET Conf demo (Aspire 13.6.4)

The AppHost uses stable 13.6.4 packages with matching browser logs, EF Core, and MAUI previews.
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

Azure publishing configuration and the opt-in MAUI launch profile are preserved.
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
