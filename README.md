# TEBI Dashboard

Dashboard-applicatie voor **TE-BI Bestratingsmaterialen (TEBI)** — een aanvulling op NetSuite voor het overzichtelijk visualiseren van omzet, afzet, trends en doelstellingen.

> Gebaseerd op [LU2 AdviesRapportage.docx](./LU2%20AdviesRapportage.docx) en [Voorstelformulier .NET MAUI app](./Jorik%20Leemans%20Voorstelformulier%20.NET%20MAUI%20app.docx)

## Stack

| Laag | Technologie |
|------|-------------|
| Front-end (native) | .NET MAUI + Blazor Hybrid (.NET 10) |
| Front-end (browser) | Blazor Web App |
| Gedeelde UI | Razor Class Library |
| Back-end | ASP.NET Core Web API (C# 14 / .NET 10) |
| ERP-bron | Oracle NetSuite (via beveiligde API-laag) |
| Demo-platformen | Windows (kantoor) + Android (terrein) |
| Device features | Barcode/camera scan (artikel op terrein) |
| Broncode | Engels |

## Documentatie

- [Technisch ontwerp](./docs/ONTWERP.md) — scope, modules, API-contract, ADR's
- [Architectuur](./docs/architectuur.md) — diagrammen en lagen
- [NetSuite data-mapping](./docs/netsuite-data-mapping.md) — velden en DTO's
- [Implementatiefases](./docs/fases.md) — 6 fases uit het adviesrapport
- [Voorstelformulier](./docs/proposal-form.md) — gebruikers, barcode, live NetSuite API
- [LU-3 Assessment](./docs/lu3-assessment.md) — portfolio-eisen (.NET 10, tests, platforms)
- [User stories](./docs/user-stories.md) — 11 stories uit voorstelformulier
- [Definition of Done](./docs/definition-of-done.md)
- [OO & design patterns](./docs/oo-and-patterns.md)
- [Installatie & gebruik](./docs/installation.md)
- [Voortgang checklist](./CHECKLIST.md) — afvinklijst voor GitHub

## Projectstructuur

```
F:\School\Full-Stack .NET MAUI\
├── src/
│   ├── TebiDashboard.slnx          ← solution (incl. MAUI)
│   ├── TebiDashboard.slnf          ← build zonder MAUI (CI/lokaal)
│   ├── TebiDashboard.Api/          ← Web API
│   ├── TebiDashboard.Application/
│   ├── TebiDashboard.Infrastructure/
│   ├── TebiDashboard.Shared/
│   ├── TebiDashboard.UI/           ← gedeelde Blazor components
│   ├── TebiDashboard.Web/          ← Blazor Web App
│   └── TebiDashboard.Maui/         ← MAUI Blazor Hybrid (workload vereist)
└── tests/
```

### Snel starten

```powershell
# Eén venster: start API + Web op de achtergrond, opent browser
.\start.cmd

# Alles stoppen
.\stop.cmd
```

Handmatig (twee terminals):

```bash
dotnet run --project src/TebiDashboard.Api
dotnet run --project src/TebiDashboard.Web
```

```bash
```bash
dotnet run --project src/TebiDashboard.Api
dotnet run --project src/TebiDashboard.Web
```

```bash
```bash
dotnet run --project src/TebiDashboard.Api
dotnet run --project src/TebiDashboard.Web
```

```bash
```bash
dotnet run --project src/TebiDashboard.Api
dotnet run --project src/TebiDashboard.Web
```

```bash
# Tests (zonder MAUI)
dotnet test src/TebiDashboard.slnf
```

Demo login API: `demo` / `demo`

MAUI: zie [docs/maui-setup.md](./docs/maui-setup.md) (schijfruimte C: voor Android SDK).

## Status

| Fase | Status |
|------|--------|
| Fase 1 — Voorbereiding en ontwerpen | Afgerond (docs) |
| Fase 2 — Web API | Basis gereed (mock NetSuite) |
| Fase 3 — Dashboard | Basis UI (KPI home) |

Zie [CHECKLIST.md](./CHECKLIST.md) voor de volledige voortgang.

## Git

Repository: **https://github.com/JorikLeemans/TebiDashboard**

```bash
cd "F:\School\Full-Stack .NET MAUI"
git pull origin main
```
