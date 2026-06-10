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
| IDE | VS 2022 17.12+ or VS Code + C# Dev Kit | — |

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

### Web API (`src/TebbiDashboard.Api`)

1. Copy User Secrets template (development):

```bash
cd src/TebbiDashboard.Api
dotnet user-secrets init
dotnet user-secrets set "NetSuite:UseMock" "true"
```

2. For real NetSuite (later):

```json
{
  "NetSuite": {
    "UseMock": false,
    "AccountId": "YOUR_ACCOUNT",
    "ConsumerKey": "...",
    "ConsumerSecret": "...",
    "TokenId": "...",
    "TokenSecret": "..."
  }
}
```

> Never commit secrets. Use User Secrets locally or environment variables in production.

### MAUI app (`src/TebbiDashboard.Maui`)

Set API base URL in `appsettings.json` or `MauiProgram.cs`:

```json
{
  "ApiBaseUrl": "https://localhost:7001"
}
```

For Android emulator, use `https://10.0.2.2:7001` to reach host machine localhost.

---

## Run locally

### 1. Start Web API

```bash
cd src/TebbiDashboard.Api
dotnet run
```

Swagger UI: `https://localhost:7001/swagger`

### 2. Start MAUI app — Windows

```bash
cd src/TebbiDashboard.Maui
dotnet build -f net10.0-windows10.0.19041.0
dotnet run -f net10.0-windows10.0.19041.0
```

Or: Visual Studio → set `TebbiDashboard.Maui` as startup → target **Windows Machine**.

### 3. Start MAUI app — Android

1. Start Android emulator (AVD) or connect device.
2. Run:

```bash
dotnet build -f net10.0-android
dotnet run -f net10.0-android
```

Or: Visual Studio → target **Android Emulators**.

### 4. Blazor Web (optional)

```bash
cd src/TebbiDashboard.Web
dotnet run
```

Browser: URL shown in console (e.g. `https://localhost:5001`).

---

### Barcode scannen (Android)

1. Open app → **Scan** tab
2. Richt camera op artikelbarcode op terrein
3. App toont artikel, voorraadlocaties en open transacties

### Barcode scannen (Windows)

1. Open artikel-zoeken
2. Scan met USB-barcode scanner (keyboard wedge) of typ barcode handmatig
3. Zelfde detailweergave als Android

---

```bash
# All unit tests
dotnet test tests/TebbiDashboard.Application.Tests
dotnet test tests/TebbiDashboard.Api.Tests

# UI / device tests (when configured)
dotnet test tests/TebbiDashboard.Maui.Tests
```

---

## Usage (end user)

1. **Launch** the app on Windows or Android.
2. **Log in** with your TEBI credentials (or demo account in development).
3. **Dashboard home** shows KPI cards: revenue, sales volume, goal progress.
4. **Navigate** via menu: Revenue, Sales Volume, Trends, Goals.
5. **Apply filters** (period, customer, product group) — all pages update accordingly.
6. **Pull to refresh** (Android) or refresh button (Windows) when auto-refresh is enabled.

---

## Build release packages

### Windows (MSIX / unpackaged)

Visual Studio → Publish → Windows → Create package.

### Android (APK)

```bash
cd src/TebbiDashboard.Maui
dotnet publish -f net10.0-android -c Release
```

APK location: `bin/Release/net10.0-android/publish/`

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| MAUI workload missing | `dotnet workload install maui` |
| Android cannot reach API | Use `10.0.2.2` instead of `localhost` |
| HTTPS certificate error | `dotnet dev-certs https --trust` |
| Empty dashboard | Ensure API running; check `NetSuite:UseMock` = true for demo data |

---

## Portfolio demonstration checklist

- [ ] API running with mock data
- [ ] Windows app shows dashboard with KPIs
- [ ] Android app shows same data after login
- [ ] At least one filter demonstrated live
- [ ] Unit test results shown (`dotnet test`)

See [lu3-assessment.md](./lu3-assessment.md) for full portfolio requirements.
