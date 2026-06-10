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
2. Hernoem de service naar `tebbi-api`
3. Open **Settings**:
   - **Build** → **Dockerfile Path**: `Dockerfile.api`
   - **Deploy** → **Start Command**: leeg laten (Dockerfile regelt dit)
4. Open **Variables** → **Add Reference**:
   - Koppel `DATABASE_URL` van de PostgreSQL-service
5. Voeg deze variabelen toe:

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `Database__ApplyMigrationsOnStartup` | `true` |
| `Database__SeedDemoData` | `true` |

6. **Settings** → **Networking** → **Generate Domain**
   - Noteer de URL, bijv. `https://tebbi-api-production.up.railway.app`

7. Controleer de deploy logs. Je zou moeten zien:
   - `Database migrations applied (PostgreSql).`
   - `Demo data seed completed.`
   - `Now listening on: http://0.0.0.0:...`

8. Test in de browser: `https://jouw-api-url/health` → moet `{"status":"healthy",...}` tonen

### Stap 4 — Web UI deployen

1. **+ New** → **GitHub Repo** → `TebiDashboard` (derde service)
2. Hernoem naar `tebbi-web`
3. **Settings** → **Dockerfile Path**: `Dockerfile.web`
4. **Variables**:

| Variable | Value |
|----------|-------|
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ApiBaseUrl` | `https://jouw-api-url/` ← **met trailing slash!** |

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
| API start niet | Check deploy logs; controleer of `DATABASE_URL` gekoppeld is |
| Web toont login maar data laadt niet | `ApiBaseUrl` moet exact de **publieke HTTPS API-URL** zijn met `/` aan het eind |
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
