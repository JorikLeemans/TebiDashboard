# TEBI Dashboard

Dashboard-applicatie voor **TE-BI Bestratingsmaterialen (TEBI)** — een aanvulling op NetSuite voor het overzichtelijk visualiseren van omzet, afzet, trends en doelstellingen.

> Gebaseerd op [LU2 AdviesRapportage.docx](./LU2%20AdviesRapportage.docx)

## Stack

| Laag | Technologie |
|------|-------------|
| Front-end (native) | .NET MAUI + Blazor Hybrid |
| Front-end (browser) | Blazor Web App |
| Gedeelde UI | Razor Class Library |
| Back-end | ASP.NET Core Web API (C# / .NET) |
| ERP-bron | Oracle NetSuite (via beveiligde API-laag) |

## Documentatie

- [Technisch ontwerp](./docs/ONTWERP.md) — scope, modules, API-contract, ADR's
- [Architectuur](./docs/architectuur.md) — diagrammen en lagen
- [NetSuite data-mapping](./docs/netsuite-data-mapping.md) — velden en DTO's
- [Implementatiefases](./docs/fases.md) — 6 fases uit het adviesrapport
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

Repository is geïnitialiseerd op branch `main`. Eerste commit en koppeling met GitHub:

```bash
cd "F:\School\Full-Stack .NET MAUI"
git add .
git commit -m "Initial commit: projectdocumentatie en ontwerp"
git remote add origin https://github.com/JOUW-GEBRUIKERSNAAM/tebbi-dashboard.git
git push -u origin main
```

> Vervang de remote-URL door je eigen GitHub-repository.
