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

## Projectstructuur (gepland)

```
F:\School\Full-Stack .NET MAUI\
├── docs/
├── src/
│   ├── TebbiDashboard.Api/
│   ├── TebbiDashboard.Application/
│   ├── TebbiDashboard.Infrastructure/
│   ├── TebbiDashboard.Shared/
│   ├── TebbiDashboard.UI/
│   ├── TebbiDashboard.Maui/
│   └── TebbiDashboard.Web/
└── tests/
```

## Status

| Fase | Status |
|------|--------|
| Fase 1 — Voorbereiding en ontwerpen | In uitvoering |
| Fase 2 — Web API | Nog niet gestart |
| Fase 3 — Dashboard | Nog niet gestart |

Zie [CHECKLIST.md](./CHECKLIST.md) voor de volledige voortgang.

## Git

Repository: **https://github.com/JorikLeemans/TebiDashboard**

```bash
cd "F:\School\Full-Stack .NET MAUI"
git pull origin main
```
