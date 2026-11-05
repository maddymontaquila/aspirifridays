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

**Prerequisites:** .NET 10+, Node.js, Docker

```bash
cd src
aspire start
```

That's it. Aspire handles spinning up PostgreSQL, Redis, the admin panel, and the Vue client.

## Deployment safety

The AppHost remains on Aspire 13.5.3. Its local-only MAUI hosting integration
uses the published 13.1.1 preview because the previous preview pin is unavailable.

The producer console is local-only and exits when standard input closes.
Azure publishing sets the Bingo Log Analytics workspace to a 1 GB/day ingestion
cap and 30-day retention. Daily caps can be exceeded before ingestion stops;
budget alerts do not stop resources.

The migration worker runs automatically only during local orchestration.
Production deployment assumes the existing database is initialized and does not
publish or start the migration Container App. Schema changes require a separate,
controlled migration run with the existing credentials. Omitted resources are
not deleted by incremental deployment; verify the producer is absent and any
previously deployed migration app remains stopped before deployment.

Production reads the existing `connectionstrings--db` Key Vault secret using
the app's managed identity. It does not deploy PostgreSQL or write database
secrets. The `existing-postgres-key-vault` parameter selects the existing vault
in the deployment resource group; local orchestration still uses containerized
PostgreSQL and runs migrations.

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
