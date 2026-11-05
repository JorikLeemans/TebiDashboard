# Implementatiefases

Uitwerking van de 6 fases uit [LU2 AdviesRapportage.docx](../LU2%20AdviesRapportage.docx). De afvinklijst staat in [CHECKLIST.md](../CHECKLIST.md).

---

## Huidige stand (november 2026)

De web-app voor fase 1 t/m 3 staat. **De volgende bouwfase is fase 4: automatische verversing (US-16).** Export (US-17) en rollenbeheer op de admin-pagina zijn al af. Bestsellers filteren al op artikelgroep; vrije-tekstzoeken nog niet.

Dit blijft open en hoort niet bij die volgende bouwfase:

| Onderwerp | Waar |
|-----------|------|
| Datavelden en een formele rollenmatrix met TEBI | Fase 1 |
| NetSuite-pilot met echte credentials | Fase 2. De sync bestaat; de dashboardpagina's lezen PostgreSQL |
| Demonstratie op Windows en Android | US-13, fase 3 en 6 |
| Security review, performance, feedback van TEBI | Fase 5 |
| MAUI-pakketten (MSIX / APK) | Fase 6 |

---

## Fase 1 — Voorbereiding en ontwerpen

**Doel:** Duidelijkheid over data, gebruikers, architectuur en planning.

### Deliverables

| Item | Document / artefact | Status |
|------|---------------------|--------|
| Adviesrapport | `LU2 AdviesRapportage.docx` | ✅ |
| Technisch ontwerp | `docs/ONTWERP.md` | ✅ |
| Architectuur | `docs/architectuur.md` | ✅ |
| Data-mapping | `docs/netsuite-data-mapping.md` | ✅ (sync in code; workshop met TEBI nog open) |
| Checklist | `CHECKLIST.md` | ✅ |
| Data-inventarisatie TEBI | Workshop met opdrachtgever | ⬜ |
| Rollen/rechtenmatrix | Rollen staan in code en in `ONTWERP.md` sectie 4. Formele matrix met TEBI | ⬜ |
| Solution-structuur | `src/TebiDashboard.slnx` | ✅ |

### Exit-criteria

- Alle Must have-eisen zijn vertaald naar modules en API-endpoints
- TEBI heeft data-velden bevestigd (of mock-data afgesproken)
- Ontwerp goedgekeurd door docent / opdrachtgever

---

## Fase 2 — Ontwikkelen van de Web API

**Doel:** Veilige koppeling tussen NetSuite en dashboard.

### Taken

1. ASP.NET Core Web API-project opzetten
2. Clean Architecture-lagen (Application, Infrastructure, Shared)
3. `INetSuiteService` interface + mock-implementatie
4. NetSuite REST-integratie (pilot met kleine dataset)
5. JWT-authenticatie en rolgebaseerde autorisatie
6. Endpoints zoals in [ONTWERP.md](./ONTWERP.md) sectie 5 (`/health`, `/dashboard/period-overview`, `/revenue`, `/sales-volume`)
7. OpenAPI op `/openapi/v1.json` in Development
8. Unit tests voor mapping en services

**Status:** de API, JWT, zeven rollen en de testprojecten staan. Een pilot tegen de TEBI-sandbox is de open exit.

### Exit-criteria

- API retourneert data uit PostgreSQL (na seed of na sync) via de Must-have-endpoints
- Geen NetSuite-credentials in de front-end
- Auth werkt met Admin, Management, Purchasing, Inventory, Sales, Finance en ReadOnly

---

## Fase 3 — Ontwikkelen van het dashboard

**Doel:** Must have-visualisaties live in MAUI en Web.

### Taken

1. Razor Class Library (`TebiDashboard.UI`) met eigen componenten
2. Pagina's: Home, Goals, Customers, Suppliers, Expenses, Inventory, Bestsellers
3. Gedeelde periodefilter
4. .NET MAUI Blazor Hybrid app (Windows en Android als target; device-demo nog open)
5. Blazor Web App (Interactive Server)
6. Responsive layout
7. Koppeling met de API via `DashboardApiClient`

**Status:** de browser-app is de dagelijkse demo. US-13 (Windows + Android live) staat nog open.

### Exit-criteria

- Omzet, afzet, trends en doelen zijn zichtbaar in de browser
- De periodefilter verandert de getoonde cijfers
- MAUI deelt dezelfde UI. De live demo op Windows en Android is nog open

---

## Fase 4 — Extra functionaliteiten (Should have)

**Doel:** Verrijking na stabiele basis.

### Taken

1. Automatische data-refresh (US-16) — **volgende bouwfase**
2. Vrije-tekstzoeken. Artikelgroepfilter op bestsellers is af
3. Export Excel (ClosedXML) en PDF (QuestPDF) — af, `GET /api/exports/period`
4. Rollenbeheer in de admin-pagina — af. Menu's zijn nog niet per rol verschillend, behalve Administration en de exportknop

**Status:** start hier met US-16.

### Exit-criteria

- Gebruiker kan de periode exporteren
- Automatische verversing zonder page reload — nog open (US-16)
- Admin beheert rollen. ReadOnly exporteert niet

---

## Fase 5 — Testen en verbeteren

**Doel:** Kwaliteit en gebruikersfeedback.

### Testplan

| Type | Focus |
|------|-------|
| Unit- en integration tests | Application, API, Infrastructure |
| UI tests | bUnit in `tests/TebiDashboard.UI.Tests` |
| Device tests | Windows en Android — nog open (US-13) |
| Security | JWT en CORS staan. Formele review nog open |
| Performance | Nog niet gemeten |
| UX | Feedbackronde met TEBI nog open |
| CI | `.github/workflows/ci.yml` draait de vier testprojecten, zonder MAUI |

### Exit-criteria

- Geen kritieke bugs open
- Eindgebruikers akkoord met dashboard-digitaliteit
- Security checklist afgerond

---

## Fase 6 — Ingebruikname en doorontwikkeling

**Doel:** Productie-ready oplevering.

### Taken

1. API + Web draaien op Railway (`Dockerfile.api`, `Dockerfile.web`). Zie [hosting.md](./hosting.md)
2. MAUI-distributie (MSIX / APK) — nog open
3. Gebruikershandleiding: [installation.md](./installation.md)
4. Could have: persoonlijke dashboards, notificaties, donkere modus (US-18)

### Exit-criteria

- Applicatie draait in productie (of TEBI acceptatie-omgeving)
- Gebruikers getraind
- Backlog voor doorontwikkeling vastgelegd in GitHub Issues

---

## Tijdlijn (indicatief)

| Fase | Duur (indicatie) | Afhankelijkheid |
|------|------------------|-----------------|
| 1 | 1–2 weken | TEBI beschikbaarheid |
| 2 | 2–3 weken | NetSuite sandbox |
| 3 | 3–4 weken | Fase 2 API |
| 4 | 2 weken | Fase 3 dashboard |
| 5 | 1–2 weken | Testgroep TEBI |
| 6 | 1 week | Go-live akkoord |

> Pas de tijdlijn aan op basis van schooldeadlines en beschikbaarheid TEBI.

---

## Voortgang bijhouden

Alle taken staan als afvinkbare items in [CHECKLIST.md](../CHECKLIST.md).
