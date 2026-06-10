# Hosting TEBI Dashboard

This guide explains how to run the dashboard with **persistent storage** so user accounts and demo dashboard data survive restarts.

## What changed

| Before | After |
|--------|-------|
| Users in memory (`InMemoryUserStore`) | Users in SQLite or PostgreSQL |
| Hard-coded mock KPI data | Database-backed dashboard data with rich seed datasets |
| Data lost on API restart | Data persists across restarts and deployments |

## Local development (SQLite — easiest)

1. Start the app as usual:

```powershell
.\start.cmd
```

2. On first run the API creates `data/tebbi-local.db` and seeds demo data automatically.

3. Demo accounts (persisted):

| Email | Password | Role |
|-------|----------|------|
| `admin@tebi.nl` | `admin1234` | Admin |
| `finance@tebi.nl` | `finance123` | Finance |
| `sales@tebi.nl` | `sales1234` | Sales |
| `inventory@tebi.nl` | `inventory1` | Inventory |

The built-in `demo` / `demo` login still works for quick demos (not stored in the database).

## Docker Compose (PostgreSQL — closest to production)

Requires [Docker Desktop](https://www.docker.com/products/docker-desktop/).

```powershell
docker compose up --build
```

| Service | URL |
|---------|-----|
| Web UI | http://localhost:7274 |
| API | http://localhost:7119 |
| PostgreSQL | localhost:5432 (`tebbi` / `tebbi_dev_password`) |

Data is stored in the Docker volume `tebbi_pg_data`.

Stop:

```powershell
docker compose down
```

Remove all data:

```powershell
docker compose down -v
```

## Configuration

`src/TebbiDashboard.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=tebbi.db"
},
"Database": {
  "Provider": "Sqlite",
  "ApplyMigrationsOnStartup": true,
  "SeedDemoData": true
}
```

For PostgreSQL set `"Provider": "PostgreSql"` and a PostgreSQL connection string.

Environment variable examples (Docker / cloud):

```
ConnectionStrings__DefaultConnection=Host=...;Database=...;Username=...;Password=...
Database__Provider=PostgreSql
Database__ApplyMigrationsOnStartup=true
Database__SeedDemoData=true
ApiBaseUrl=https://your-api-url/
```

## Reload demo dashboard data

Log in as admin and call:

```
POST /api/admin/demo-data/reseed
Headers: X-Tebi-Username: admin@tebi.nl, X-Tebi-Role: Admin
```

This clears revenue, goals, inventory, expenses and reloads the seed datasets. **Registered users are kept.**

## Cloud hosting options

### Option A — Railway (recommended for quick setup)

1. Create a [Railway](https://railway.app) project.
2. Add a **PostgreSQL** plugin.
3. Deploy the **API** from this repo:
   - Root directory: repository root
   - Dockerfile: `Dockerfile.api`
   - Set env vars from the PostgreSQL plugin + `Database__Provider=PostgreSql`
4. Deploy the **Web** host:
   - Dockerfile: `Dockerfile.web`
   - Set `ApiBaseUrl` to the public API URL (with trailing `/`)

### Option B — Render

1. Create a **PostgreSQL** instance on [Render](https://render.com).
2. Create a **Web Service** for the API using `Dockerfile.api`.
3. Create a second **Web Service** for the Blazor host using `Dockerfile.web`.
4. Point `ApiBaseUrl` on the Web service to the API URL.

### Option C — Azure App Service

1. Azure Database for PostgreSQL (Flexible Server).
2. Two App Services (API + Web) or one App Service with both containers.
3. Configure connection string in App Service **Configuration**.

## EF Core migrations

Create a new migration after entity changes:

```powershell
dotnet ef migrations add MigrationName `
  --project src/TebbiDashboard.Infrastructure/TebbiDashboard.Infrastructure.csproj `
  --startup-project src/TebbiDashboard.Api/TebbiDashboard.Api.csproj
```

Migrations run automatically on API startup when `ApplyMigrationsOnStartup` is `true`.

## Demo seed contents

The seed includes:

- 15 revenue records across 5 customers and 3 product groups
- 8 goals (quarterly + item-level)
- 10 inventory items (4 low-stock alerts)
- Stock locations, open transactions, and 8 expense records

Use this as a baseline and extend via SQL, admin tools, or future import endpoints.
