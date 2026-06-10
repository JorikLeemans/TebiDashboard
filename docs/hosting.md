# Hosting TEBI Dashboard

This guide explains how to run the dashboard with **persistent storage** so user accounts and demo dashboard data survive restarts.

## What changed

| Before | After |
|--------|-------|
| Users in memory (`InMemoryUserStore`) | Users in PostgreSQL (local + hosted) |
| Hard-coded mock KPI data | Database-backed dashboard data with rich seed datasets |
| Data lost on API restart | Data persists across restarts and deployments |

## Local development (PostgreSQL — same as Railway)

Local and hosted environments both use **PostgreSQL** with the same EF Core migrations and seed data.

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (runs PostgreSQL locally)

### Quick start

```powershell
.\start.cmd
```

This script:

1. Starts PostgreSQL in Docker (`docker compose up postgres`)
2. Launches the API (`https://localhost:7119`) — applies migrations and seeds demo data
3. Launches the Web UI (`https://localhost:7274`)

Database credentials (local Docker):

| Setting | Value |
|---------|-------|
| Host | `localhost:5432` |
| Database | `TebiDashboard` |
| User | `Tebi` |
| Password | `Tebi_dev_password` |

Data persists in the Docker volume `Tebi_pg_data`.

Stop the apps by closing the API/Web terminal windows. Stop PostgreSQL with:

```powershell
docker compose stop postgres
```

Remove all local database data:

```powershell
docker compose down -v
```

### Demo accounts (persisted)

| Email | Password | Role |
|-------|----------|------|
| `admin@tebi.nl` | `admin1234` | Admin |
| `finance@tebi.nl` | `finance123` | Finance |
| `sales@tebi.nl` | `sales1234` | Sales |
| `inventory@tebi.nl` | `inventory1` | Inventory |

The built-in `demo` / `demo` login still works for quick demos (not stored in the database).

## Docker Compose (full stack in containers)

Run API + Web + PostgreSQL entirely in Docker (closest to production deployment):

```powershell
docker compose up --build
```

| Service | URL |
|---------|-----|
| Web UI | http://localhost:7274 |
| API | http://localhost:7119 |
| PostgreSQL | localhost:5432 (`Tebi` / `Tebi_dev_password`) |

Data is stored in the Docker volume `Tebi_pg_data`.

Stop:

```powershell
docker compose down
```

Remove all data:

```powershell
docker compose down -v
```

## Configuration

`src/TebiDashboard.Api/appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Host=localhost;Port=5432;Database=TebiDashboard;Username=Tebi;Password=Tebi_dev_password"
},
"Database": {
  "Provider": "PostgreSql",
  "ApplyMigrationsOnStartup": true,
  "SeedDemoData": true
}
```

On Railway, set `DATABASE_URL` from the PostgreSQL service — the API detects it automatically.

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

See the full Dutch walkthrough below: [Railway deployment](#railway-deployment-stap-voor-stap).

Quick summary:

1. Create a [Railway](https://railway.app) project from GitHub.
2. Add **PostgreSQL**.
3. Deploy **API** service (`Dockerfile.api`) and link the database.
4. Generate a public domain for the API.
5. Deploy **Web** service (`Dockerfile.web`) with `ApiBaseUrl` pointing to the API domain.

---

## Railway deployment (stap voor stap)

Deploy **3 onderdelen** op Railway: PostgreSQL, API en Web UI. De MAUI-app blijft lokaal; alleen API + Web gaan naar de cloud.

### Vereisten

- GitHub-repo: [TebiDashboard](https://github.com/JorikLeemans/TebiDashboard.git) (push je laatste wijzigingen)
- [Railway-account](https://railway.app) (gratis trial / hobby plan)

### Stap 1 — Nieuw project

1. Ga naar [railway.app/new](https://railway.app/new)
2. Kies **Deploy from GitHub repo**
3. Selecteer `TebiDashboard`
4. Railway maakt een eerste service aan (die hernoemen we zo)

### Stap 2 — PostgreSQL toevoegen

1. In je project: **+ New** → **Database** → **PostgreSQL**
2. Wacht tot de database **Active** is
3. Railway maakt automatisch een `DATABASE_URL` variabele aan

> De API leest `DATABASE_URL` automatisch en schakelt over naar PostgreSQL. Je hoeft geen connection string handmatig te bouwen.

### Stap 3 — API deployen

1. **+ New** → **GitHub Repo** → kies opnieuw `TebiDashboard` (tweede service)
2. Hernoem de service naar `Tebi-api`
3. Open **Settings** → **Build**:
   - **Builder**: `Dockerfile` (niet Railpack/Nixpacks!)
   - **Dockerfile Path**: `Dockerfile.api`
   - **Config file path**: `/railway.toml` (staat in de repo)
4. Open **Variables** → **Add Reference**:
   - Koppel `DATABASE_URL` van de PostgreSQL-service (of individueel: `PGHOST`, `PGPORT`, `PGUSER`, `PGPASSWORD`, `PGDATABASE`)
5. Voeg deze variabelen toe:

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Database__ApplyMigrationsOnStartup` | `true` |
| `Database__SeedDemoData` | `true` |

6. **Settings** → **Networking** → **Generate Domain**
   - Noteer de URL, bijv. `https://Tebi-api-production.up.railway.app`

7. Controleer de deploy logs. Je zou moeten zien:
   - `Database migrations applied (PostgreSql).`
   - `Demo data seed completed.`
   - `Now listening on: http://0.0.0.0:...`

8. Test in de browser: `https://jouw-api-url/health` → moet `{"status":"healthy",...}` tonen

### Stap 4 — Web UI deployen

1. **+ New** → **GitHub Repo** → `TebiDashboard` (derde service)
2. Hernoem naar `Tebi-web`
3. **Settings** → **Build**:
   - **Builder**: `Dockerfile`
   - **Dockerfile Path**: `Dockerfile.web`
   - **Config file path**: `/railway.web.toml`
4. **Variables**:

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ApiBaseUrl` | `https://jouw-api-url/` ← HTTPS + trailing slash (host-only zoals `Tebi-api-production.up.railway.app` werkt ook) |

5. **Networking** → **Generate Domain**
6. Open de Web-URL in je browser → loginpagina TEBI Dashboard

### Stap 5 — Inloggen

| Account | Wachtwoord | Rol |
|---------|------------|-----|
| `admin@tebi.nl` | `admin1234` | Admin |
| `demo` | `demo` | Management (hardcoded, geen DB) |

### Kosten & tips

- **Hobby plan**: ~$5/maand credits; PostgreSQL + 2 services verbruiken credits
- **Slaapstand**: gratis tier laat services soms slapen → eerste request duurt langer
- **Logs**: per service → **Deployments** → **View logs** bij problemen
- **Redeploy**: push naar GitHub → Railway bouwt automatisch opnieuw

### Problemen oplossen

| Probleem | Oplossing |
|----------|-----------|
| **Railpack could not determine how to build** | Zet **Builder** op `Dockerfile` (Settings → Build). Railpack herkent onze monorepo niet; gebruik `Dockerfile.api` of `Dockerfile.web`. |
| **Healthcheck blijft falen** | Controleer deploy **runtime logs** (niet alleen build). Zorg dat `DATABASE_URL` gekoppeld is. HTTPS-redirect is uitgeschakeld in Production. |
| API start niet | Check deploy logs; controleer of `DATABASE_URL` gekoppeld is |
| Web toont login maar data laadt niet | `ApiBaseUrl` moet exact de **publieke HTTPS API-URL** zijn met `/` aan het eind |
| **Error.** pagina op Web-URL | Open **Tebi-web** logs in Railway. Controleer `ApiBaseUrl` (publieke API-URL + `/`). Test `https://jouw-web-url/health` en `https://jouw-api-url/health`. Open niet per ongeluk de API-URL in de browser. |
| Database leeg na deploy | Zet `Database__SeedDemoData=true`; herstart API |
| Build faalt op .NET 10 | Railway ondersteunt Docker — onze Dockerfiles gebruiken `mcr.microsoft.com/dotnet/sdk:10.0` |

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
  --project src/TebiDashboard.Infrastructure/TebiDashboard.Infrastructure.csproj `
  --startup-project src/TebiDashboard.Api/TebiDashboard.Api.csproj
```

Migrations run automatically on API startup when `ApplyMigrationsOnStartup` is `true`.

## Demo seed contents

The seed includes:

- 15 revenue records across 5 customers and 3 product groups
- 8 goals (quarterly + item-level)
- 10 inventory items (4 low-stock alerts)
- Stock locations, open transactions, and 8 expense records

Use this as a baseline and extend via SQL, admin tools, or future import endpoints.
