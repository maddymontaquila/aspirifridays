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
aspire run
```

That's it. Aspire handles spinning up PostgreSQL, Redis, the admin panel, and the Vue client.

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
