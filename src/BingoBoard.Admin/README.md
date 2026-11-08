# BingoBoard.Admin - AspiriFridays Bingo Admin Dashboard

## Overview

The BingoBoard.Admin project is a Blazor Server application that provides an administrative interface for managing the AspiriFridays Bingo Board. It allows administrators to monitor connected clients, manage bingo squares, and interact with clients in real-time using SignalR.

## Features Implemented

### ✅ Core Features (As per specification)

1. **Admin Dashboard**
   - User-friendly dashboard to manage bingo squares
   - Display list of connected clients with their status
   - Real-time updates when squares are checked/unchecked
   - Admin can send updates to all clients

2. **Customizable Bingo Sets**
   - 33+ predefined bingo squares with categorization
   - Random selection of 25 squares for each client (24 + 1 free space)
   - Each square has: `label`, `id`, `type` (categorization)
   - Free space automatically placed in center and marked as checked

3. **Client Connection Management**
   - Track all connected clients with connection details
   - Store current bingo set for each client in cache
   - Real-time connection/disconnection notifications
   - Admin can verify current bingo set for each client

### ✅ Technical Implementation

- **Frontend**: Blazor Server with Bootstrap 5 and Font Awesome icons
- **Real-time Communication**: SignalR Hub for bidirectional communication
- **Caching**: Distributed memory cache (in-memory for development, Redis-ready for production)
- **Architecture**: Clean separation with services and models

## Project Structure

```
BingoBoard.Admin/
├── Components/
│   ├── Layout/           # Layout components (MainLayout, NavMenu)
│   └── Pages/            # Razor pages (Home dashboard, SignalR demo)
├── Hubs/
│   └── BingoHub.cs       # SignalR hub for real-time communication
├── Models/               # Data models
│   ├── BingoSquare.cs    # Individual bingo square model
│   ├── BingoSet.cs       # Complete bingo set for a client
│   └── ConnectedClient.cs # Connected client information
├── Services/             # Business logic services
│   ├── IBingoService.cs      # Bingo management interface
│   ├── BingoService.cs       # Bingo management implementation
│   ├── IClientConnectionService.cs  # Client management interface
│   └── ClientConnectionService.cs   # Client management implementation
└── Program.cs            # Application configuration
```

## SignalR Hub Methods

### Client → Admin Messages
- `RequestBingoSet(userName?)` - Client requests a new bingo set
- `UpdateSquare(squareId, isChecked)` - Client updates their own square
- `GetCurrentBingoSet()` - Client requests their current bingo set

### Admin → Client Messages  
- `AdminUpdateSquare(connectionId, squareId, isChecked)` - Admin updates square for specific client
- `GetConnectedClients()` - Admin requests list of connected clients

### Broadcast Messages (Hub → All)
- `UserConnected` - New client connected
- `UserDisconnected` - Client disconnected  
- `BingoAchieved` - Client achieved bingo
- `SquareUpdated` - Square status changed
- `BingoSetReceived` - New bingo set generated

## Key Services

### BingoService
- Manages the 33+ available bingo squares
- Generates random 5x5 bingo sets for clients
- Handles square status updates
- Checks for winning conditions (rows, columns, diagonals)
- Stores/retrieves bingo sets from cache

### ClientConnectionService  
- Tracks connected clients and their metadata
- Associates bingo sets with client connections
- Manages client activity timestamps
- Stores client information in distributed cache

## Configuration

### Deployed Admin Access and Password Recovery

The deployed admin panel is at https://admin.aspireify.live/login. Password
sign-in uses the single `admin` account; no username is required.

The deployed password is the password last set on the `admin` database account.
Routine production deployments do not run migrations or pass
`ADMIN_PASSWORD` to a worker. Updating that GitHub Actions secret does not update
the account, and redeploying does not reset it.

To reset a forgotten password:

1. Open the repository's **Settings > Secrets and variables > Actions** and
   update `ADMIN_PASSWORD` with a new, strong, unique password. Store it in a
   password manager; do not put it in source code, logs, or issues.
2. Obtain separate approval for a controlled account recovery run against the
   existing database, with the intended revision, backup/rollback plan, and
   database access scope reviewed. The migration service can create/update the
   account, but also applies schema migrations and seeds squares: do not run it
   against production as an unreviewed password-reset shortcut.
3. After the approved recovery updates the account successfully, sign in with
   the new password. Password rotation changes the admin security stamp, so
   existing sessions will be invalidated when it is next validated.

No automated production recovery path is provided by this corrective change.
The updated `ADMIN_PASSWORD` still requires that separately approved recovery;
a routine deployment is not sufficient. There is no public forgot-password
endpoint or email reset flow.
Do not delete the database or admin account to reset access.

#### Login Troubleshooting

- `error=invalid`: credentials were rejected; use the deployed password or the
  recovery procedure above.
- `error=no-user`: the admin account was not found; request controlled recovery,
  not a routine deployment.
- `error=passkey-error`: passkey authentication failed; try password sign-in.
- `error=unexpected`: the login handler caught a server exception, not an
  ordinary password rejection. Inspect the deployed `boardadmin` console logs
  for `Unhandled exception during login.` and its stack trace. A password reset
  is not a substitute for diagnosing this error.

Published admin connections explicitly use `SSL Mode=VerifyFull`; migrations
are not published. This requires TLS and validates the Azure
PostgreSQL server certificate and hostname. Local Aspire runs retain their
container connection settings. Do not disable PostgreSQL's
`require_secure_transport` setting to work around connection failures.

Production database credentials are separate from `ADMIN_PASSWORD`. Set the
repository's `POSTGRES_USER` and `POSTGRES_PASSWORD` Actions secrets to the
existing PostgreSQL administrator login and its current password. The workflow
uses Npgsql's connection-string builder to construct an external
`ConnectionStrings__db` input with the existing server hostname and database `db`.
Quotes, semicolons, and other credential punctuation are encoded by the builder,
not shell interpolation. No PostgreSQL infrastructure or Key Vault database
secrets are managed by routine publishing, and there is no vault-secret fallback.

Database credential rotation is a separate, explicitly approved operation, not
part of deployment or admin account recovery. Keep the existing GitHub credentials
aligned with the server's actual login. Do not rerun an older deployment revision
that provisions PostgreSQL or writes database Key Vault secrets.

### Development
- Uses in-memory distributed cache
- Default SignalR hub at `/bingohub`
- Bootstrap 5 and Font Awesome 6.4.0 via CDN

### Production Ready
- Redis connection string configurable via `appsettings.json`
- Environment-specific caching (memory for dev, Redis for prod)
- Logging and error handling implemented

## How to Run

1. **Prerequisites**: .NET 9.0 SDK
2. **Build**: `dotnet build`
3. **Run**: `dotnet run`
4. **Access**: Navigate to `https://localhost:5001` or `http://localhost:5000`

## Pages Available

1. **Dashboard** (`/`) - Main admin interface
   - View connected clients
   - Select client to view/manage their bingo board
   - Admin controls for square management
   - Activity log

2. **SignalR Demo** (`/signalr-demo`) - Client simulator for testing
   - Connect/disconnect simulation
   - Request bingo sets
   - Simulate square clicks
   - View real-time messages

## Usage Workflow

1. **Admin starts the application** - Dashboard loads and connects to SignalR hub
2. **Clients connect** (via the demo page or external client) - Appears in connected clients list
3. **Client requests bingo set** - Gets randomized 25 squares, stored in cache
4. **Admin selects client** - Views client's current bingo board
5. **Admin/Client updates squares** - Real-time updates across all connected interfaces
6. **Win detection** - Automatic bingo detection and notification

## Bingo Squares Categories

The system includes squares categorized as:
- **bug** - Bug-related events (red border)
- **dev** - Development moments (blue border)  
- **quote** - Memorable quotes (purple border)
- **oops** - Mistakes/accidents (orange border)
- **meta** - Meta-commentary (gray border)
- **free** - Free space (yellow background)

## Future Enhancements

- Persistent storage (database instead of cache)
- User authentication and authorization
- Multiple game sessions/rooms
- Custom bingo set creation
- Statistics and analytics
- Mobile-responsive improvements
- Push notifications

## Technical Notes

- All async operations properly handled with error catching
- Comprehensive logging for debugging and monitoring
- Clean architecture with dependency injection
- SignalR connection management with automatic reconnection
- Responsive design with Bootstrap grid system
- Font Awesome icons for enhanced UI
