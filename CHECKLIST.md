# TEBI Dashboard — Voortgang

Afvinklijst gekoppeld aan het [adviesrapport](./LU2%20AdviesRapportage.docx) en het [technisch ontwerp](./docs/ONTWERP.md).

> Vink items af door `[ ]` te wijzigen naar `[x]` in een commit of pull request.  
> **Laatste update:** oktober 2026 — afgestemd op `main` (US-17 export, JWT, 273 tests).

---

## MoSCoW — Must have

- [x] NetSuite-koppeling via Web API (geen directe client-toegang) — `INetSuiteService` + API-laag; demo-DB i.p.v. live NetSuite
- [x] Dashboard met omzet, afzet, trends en doelen — `/dashboard`, `/goals`
- [x] Duidelijke visualisaties (grafieken en tabellen) — bar/line charts, KPI- en datatabellen
- [x] Gebruiksvriendelijke interface — navigatie, period filter, detailpagina's
- [x] Toegankelijk via browser (Blazor Web) — `TebiDashboard.Web`
- [ ] Toegankelijk via native app (MAUI Blazor Hybrid) — project aanwezig; workload + demo op device nog open
- [x] Veilige gegevensverwerking (auth, HTTPS, rollen) — JWT op data-endpoints, rollen inclusief ReadOnly; formele security review blijft open

## MoSCoW — Should have

- [ ] Automatische data-updates (refresh / polling)
- [x] Filters en zoekmogelijkheden (periode, klant, artikelgroep) — periode-filter + klant-/leveranciers-/voorraadoverzichten; artikelgroep-filter nog open
- [x] Responsive design (telefoon, tablet, desktop) — basis responsive layout in Blazor UI
- [x] Export naar Excel of PDF — `GET /api/exports/period`, knoppen op het periodefilter
- [x] Rollen en rechtenbeheer in UI — admin-dashboard voor rollen/wachtwoorden; rolgebaseerde menu's beperkt

## MoSCoW — Could have (backlog)

- [ ] Persoonlijke dashboards / widgets
- [ ] Geavanceerde interactieve grafieken
- [ ] Notificaties bij afwijkende omzetcijfers
- [ ] Donkere modus

## MoSCoW — Won't have

- [x] ~~NetSuite vervangen~~ — buiten scope
- [x] ~~AI-analyses~~ — buiten scope
- [x] ~~Offline functionaliteit~~ — buiten scope

---

## Fase 1 — Voorbereiding en ontwerpen

- [x] Projectmap aangemaakt (`F:\School\Full-Stack .NET MAUI`)
- [x] Adviesrapport aanwezig
- [ ] NetSuite-datavelden inventariseren met TEBI — concept-mapping in `docs/netsuite-data-mapping.md`; workshop met TEBI nog open
- [ ] Rollen en rechtenmatrix vastleggen — rollen in code; formele matrix met TEBI nog open
- [x] `docs/ONTWERP.md` opgesteld
- [x] `docs/architectuur.md` opgesteld
- [x] `docs/netsuite-data-mapping.md` opgesteld
- [x] `docs/fases.md` opgesteld
- [x] LU-3 portfolio-docs (`lu3-assessment`, user stories, DoD, OO/patterns, installation)
- [x] Voorstelformulier verwerkt ([proposal-form.md](./docs/proposal-form.md))
- [x] `CHECKLIST.md` en `README.md` aangemaakt
- [x] Git-repository geïnitialiseerd (branch `main`, `.gitignore`)
- [x] Eerste commit en push naar GitHub
- [x] Solution-structuur aanmaken (`src/TebiDashboard.slnx`)
- [x] Unit-, API- en UI-tests (groen) — **273 tests** (32 application + 41 API + 71 infrastructure + 129 UI)
- [ ] MAUI workload installeren (zie [maui-setup.md](./docs/maui-setup.md))

## Fase 2 — Web API

- [x] ASP.NET Core Web API-project opgezet — `TebiDashboard.Api`
- [ ] NetSuite-integratielaag (pilot met kleine dataset) — demo-DB + seeder; live NetSuite-pilot nog open
- [x] Auth + autorisatie per rol — login/register, JWT, admin-dashboard; ReadOnly kan niet exporteren
- [x] DTO's en mapping getest — API- en application-tests
- [x] Swagger/OpenAPI documentatie — `/openapi/v1.json` in Development
- [x] Health check endpoint — `GET /api/health`

## Fase 3 — Dashboard (Must have)

- [x] Gedeelde Blazor UI-bibliotheek (`TebiDashboard.UI`)
- [ ] MAUI Blazor Hybrid app (Windows + Android) — project met beide targets; functionele check vastgelegd; portfolio-demo nog open
- [x] Blazor Web App voor browser — `TebiDashboard.Web`
- [x] KPI-kaarten op dashboard home — omzet, afzet, doel-%, low-stock alerts
- [x] Omzet-, afzet-, trend- en doel-visualisaties — dashboard + goals charts
- [x] Basis filters (periode, klant, artikelgroep) — periode + klant/leverancier; artikelgroep nog open
- [x] Responsive layout

### Extra modules (buiten oorspronkelijke checklist)

- [x] Klantenoverzicht en -detail (`/customers`) met revenue en sales orders
- [x] Leveranciersoverzicht en -detail (`/suppliers`) met expenses en purchase orders
- [x] Voorraadoverzicht en artikeldetail (`/inventory`)
- [x] Orderbedragen gekoppeld aan revenue/expenses (demo-alignment)

## Fase 4 — Should have

- [ ] Automatische data-refresh
- [ ] Uitgebreide filters en zoeken (artikelgroep, vrije tekst)
- [x] Export Excel/PDF — `PeriodExcelWriter`, `PeriodPdfWriter`, `PeriodExportButtons`
- [x] Rollenbeheer in UI — admin-dashboard

## Fase 5 — Testen

- [x] Unit- en integration tests API — 144 tests (application + API + infrastructure)
- [ ] UI-tests op meerdere apparaten — 129 bUnit-tests; fysieke Android/Windows-demo nog open
- [x] Build pipeline — GitHub Actions draait de vier testprojecten (geen MAUI-workload)
- [ ] Beveiligings- en performancetest
- [ ] Feedbackronde eindgebruikers (TEBI)

## Fase 6 — Livegang

- [x] Deployment API + Web — Railway/hosted API; lokale Web/Blazor UI
- [ ] MAUI distributie (MSIX / APK)
- [x] Gebruikershandleiding — [installation.md](./docs/installation.md)
- [ ] Doorontwikkeling Could have-items

---

## LU-3 Portfolio (assessment)

Zie [docs/lu3-assessment.md](./docs/lu3-assessment.md) voor volledige mapping.

### Niet-functioneel

- [x] .NET 10 + C# 14 (`global.json`, target frameworks)
- [x] Broncode volledig in het Engels
- [x] `.editorconfig` + consistente codestijl
- [ ] Demonstratie Windows + Android
- [x] Min. 3 zinnige unit tests (groen) — **144 unit/integration tests**
- [x] Min. 3 zinnige UI/device running tests — **129 bUnit UI tests** (device-demo nog open)
- [x] OO-principes & patterns gedocumenteerd ([oo-and-patterns.md](./docs/oo-and-patterns.md))
- [x] Architectuurstijl gemotiveerd ([ONTWERP.md](./docs/ONTWERP.md) ADR's)
- [x] Installatie- & gebruikershandleiding ([installation.md](./docs/installation.md))

### Functioneel

- [x] User stories opgesteld ([user-stories.md](./docs/user-stories.md))
- [x] Definition of Done opgesteld ([definition-of-done.md](./docs/definition-of-done.md))
- [x] US-01 t/m US-12, US-14, US-15 en US-17 Done — zie [user-stories.md](./docs/user-stories.md); US-13 demo nog open
- [x] US-12 t/m US-14 (login, platform, dashboard home) — login + dashboard Done; US-13 platform-demo nog open

---

## Risico's (uit adviesrapport)

| Risico | Maatregel | Status |
|--------|-----------|--------|
| NetSuite-data niet geschikt voor dashboards | Data-mapping workshop (zie `docs/netsuite-data-mapping.md`) | [ ] |
| API-koppeling werkt niet goed | Pilot met kleine dataset | [~] demo-DB actief; live NetSuite nog niet |
| Dashboard niet duidelijk voor gebruikers | UX-test met eindgebruikers | [ ] |
| Onveilige verwerking gevoelige data | Rollen, rechten, beveiligde API-laag | [~] JWT en rollen actief; formele security review nog open |
| Project te groot door extra wensen | Eerst Must have, daarna Should/Could | [x] |
