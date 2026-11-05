# Installation & Usage Guide

How to install, configure, and run the TEBI Dashboard application.

> **Platforms for portfolio demo:** Windows + Android  
> **Stack:** .NET 10, .NET MAUI (Blazor Hybrid), ASP.NET Core Web API

---

## Prerequisites

### All developers

| Tool | Version | Download |
|------|---------|----------|
| .NET SDK | 10.x | https://dotnet.microsoft.com/download |
| Git | Latest | https://git-scm.com |
| Docker Desktop | Latest | https://www.docker.com/products/docker-desktop/ |
| IDE | VS 2022 17.12+ or VS Code + C# Dev Kit | — |

> Docker runs the local **PostgreSQL** database — the same engine used on Railway.

### .NET MAUI workload

```bash
dotnet workload install maui
```

### Windows (MAUI target)

- Windows 10/11 (build 19041+)
- Visual Studio workload: **.NET Multi-platform App UI development**

### Android (MAUI target)

- Android SDK (via Visual Studio Installer or Android Studio)
- Android emulator **or** physical device with USB debugging

---

## Clone repository

```bash
git clone https://github.com/JorikLeemans/TebiDashboard.git
cd TebiDashboard
```

---

## Configuration

### Web API (`src/TebiDashboard.Api`)

1. User secrets are optional until you connect NetSuite:

```bash
cd src/TebiDashboard.Api
dotnet user-secrets init
```

2. Local demo data does not need NetSuite. The API reads PostgreSQL. `Database:Provider=InMemory` uses `MockNetSuiteService` and is for tests.

3. For a real NetSuite sync, set OAuth 2.0 machine-to-machine values (certificate JWT). Do not commit the private key.

```bash
dotnet user-secrets set "NetSuite:Enabled" "true"
dotnet user-secrets set "NetSuite:AccountId" "YOUR_ACCOUNT"
dotnet user-secrets set "NetSuite:ClientId" "YOUR_CLIENT_ID"
dotnet user-secrets set "NetSuite:CertificateId" "YOUR_CERTIFICATE_ID"
dotnet user-secrets set "NetSuite:PrivateKeyPem" "-----BEGIN PRIVATE KEY-----..."
```

On Railway the same keys are environment variables (`NetSuite__Enabled`, `NetSuite__PrivateKeyPem`, and so on). An admin then runs the sync from the Administration page.

> Never commit secrets. Use User Secrets locally or environment variables in production.

### MAUI app (`src/TebiDashboard.Maui`)

`MauiProgram.cs` points Windows at `https://localhost:7119/` and the Android emulator at `https://10.0.2.2:7119/`.

---

## Run locally

### Quick start (API + Blazor Web + PostgreSQL)

From the repository root:

```powershell
.\start.cmd
```

Requirements: **Docker Desktop** must be running.

`start.cmd` runs `start.ps1` with `-ExecutionPolicy Bypass`, so you do not need to change Windows execution policy.

This starts PostgreSQL in Docker, then opens two terminal windows (API on `https://localhost:7119`, Web on `https://localhost:7274`), trusts the HTTPS dev certificate, and waits for the API health check before starting the web app.

### 1. Start Web API

```bash
cd src/TebiDashboard.Api
dotnet run
```

OpenAPI document (Development only): `https://localhost:7119/openapi/v1.json`

### 2. Start MAUI app — Windows

```bash
cd src/TebiDashboard.Maui
dotnet build -f net10.0-windows10.0.19041.0
dotnet run -f net10.0-windows10.0.19041.0
```

Or: Visual Studio → set `TebiDashboard.Maui` as startup → target **Windows Machine**.

### 3. Start MAUI app — Android

1. Start Android emulator (AVD) or connect device.
2. Run:

```bash
dotnet build -f net10.0-android
dotnet run -f net10.0-android
```

Or: Visual Studio → target **Android Emulators**.

### 4. Blazor Web

`.\start.cmd` already starts this. To run it on its own:

```bash
cd src/TebiDashboard.Web
dotnet run
```

Browser: `https://localhost:7274`

---

## Barcode

### Android and the website

The scan page uses the phone camera. A known barcode opens the item, its locations, and its open transactions (`GET /api/items/by-barcode/{barcode}`).

### Windows

Focus the scan field and use a USB barcode scanner (keyboard wedge), or type the code. The MAUI app can also open the camera through `IBarcodeScannerService`.

---

## Tests

Run the four projects that CI runs. The full solution build needs the MAUI workload.

```bash
dotnet test tests/TebiDashboard.Application.Tests
dotnet test tests/TebiDashboard.Api.Tests
dotnet test tests/TebiDashboard.Infrastructure.Tests
dotnet test tests/TebiDashboard.UI.Tests
```

---

## Usage (end user)

1. **Launch** the Blazor site, or the MAUI app on Windows or Android.
2. **Log in** with a TEBI account. In Development, `demo` / `demo` signs in as Management. A new registration starts as ReadOnly.
3. **Dashboard home** shows revenue, sales volume, goal progress, and low-stock alerts for the selected period.
4. **Navigate** via Home, Goals, Customers, Suppliers, Expenses, Inventory, and Bestsellers. Administrators also see Administration.
5. **Change the period** in the filter bar. Bestsellers can also filter by product group.
6. **Export** the period as Excel or PDF. ReadOnly cannot export.

Automatic refresh is not built yet (US-16). Reload the page to pick up a new sync.

---

## Build release packages

### Windows (MSIX / unpackaged)

Visual Studio → Publish → Windows → Create package.

### Android (APK)

```bash
cd src/TebiDashboard.Maui
dotnet publish -f net10.0-android -c Release
```

APK location: `bin/Release/net10.0-android/publish/`

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| `running scripts is disabled` | Use `.\start.cmd` instead of `.\start.ps1`, or run `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` once |
| `Docker is required for local PostgreSQL` | Install and start [Docker Desktop](https://www.docker.com/products/docker-desktop/), then run `.\start.cmd` again |
| API cannot connect to database | Run `docker compose ps` — PostgreSQL should be healthy on port 5432 |
| MAUI workload missing (`NETSDK1147`) | `dotnet workload install maui`. CI does not build the MAUI project |
| Android cannot reach API | The emulator uses `https://10.0.2.2:7119/`, which `MauiProgram.cs` already sets |
| HTTPS certificate error | `dotnet dev-certs https --trust` |
| Empty dashboard | Confirm the API is running and `Database:SeedDemoData` is true, or sign in as admin and run a NetSuite sync |

---

## Portfolio demonstration checklist

- [ ] API running against PostgreSQL (or InMemory for a test)
- [ ] Windows app shows the dashboard with KPIs
- [ ] Android app shows the same data after login
- [ ] At least one period filter demonstrated live
- [ ] Test results shown for the four test projects

See [lu3-assessment.md](./lu3-assessment.md) for the portfolio requirements.
