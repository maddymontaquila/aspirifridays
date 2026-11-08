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

The migration worker is published again and runs during orchestration to apply
schema migrations, seed missing default squares, and create or update the admin
password. Existing squares are preserved. Changing `ADMIN_PASSWORD` alone does
not update the account; the next successful deployment and migration run applies
it. Omitted resources are not deleted by incremental deployment; verify the
producer is absent before deployment.

Production now uses Aspire's PostgreSQL resource and connection-secret wiring
instead of reading the existing `connectionstrings--db` Key Vault secret directly.
Deployment requires `POSTGRES_USER` and `POSTGRES_PASSWORD` to match the existing
server's administrator credentials; it must not generate replacement credentials.
Both admin and migrations use `SSL Mode=VerifyFull` in published deployments.
Local orchestration retains Aspire-generated container credentials and connection
settings.

The `live` deployment workflow verifies the personal Bingo subscription, tenant,
resource group, and region before deployment. It also requires the configured
cache password to match the existing Redis secret, so a stale CI secret cannot
reset that credential. Target or credential mismatches fail without printing
secret values.

## Deployed Admin Access

Sign in at https://admin.aspireify.live/login using the deployed admin password.
To recover access, update the GitHub Actions `ADMIN_PASSWORD` secret and redeploy
the intended `live` revision. See the [admin access and recovery guide](src/BingoBoard.Admin/README.md#deployed-admin-access-and-password-recovery)
for the full procedure and login error troubleshooting.

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
