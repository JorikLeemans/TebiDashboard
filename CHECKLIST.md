# TEBI Dashboard — Voortgang

Afvinklijst gekoppeld aan het [adviesrapport](./LU2%20AdviesRapportage.docx) en het [technisch ontwerp](./docs/ONTWERP.md).

> Vink items af door `[ ]` te wijzigen naar `[x]` in een commit of pull request.  
> **Laatste update:** juni 2026 — afgestemd op `feature/goals-and-charts`.

---

## MoSCoW — Must have

- [x] NetSuite-koppeling via Web API (geen directe client-toegang) — `INetSuiteService` + API-laag; demo-DB i.p.v. live NetSuite
- [x] Dashboard met omzet, afzet, trends en doelen — `/dashboard`, `/goals`
- [x] Duidelijke visualisaties (grafieken en tabellen) — bar/line charts, KPI- en datatabellen
- [x] Gebruiksvriendelijke interface — navigatie, period filter, detailpagina's
- [x] Toegankelijk via browser (Blazor Web) — `TebiDashboard.Web`
- [ ] Toegankelijk via native app (MAUI Blazor Hybrid) — project aanwezig; workload + demo op device nog open
- [ ] Veilige gegevensverwerking (auth, HTTPS, rollen) — login + rollen + admin; JWT en API-auth middleware nog open

## MoSCoW — Should have

- [ ] Automatische data-updates (refresh / polling)
- [x] Filters en zoekmogelijkheden (periode, klant, artikelgroep) — periode-filter + klant-/leveranciers-/voorraadoverzichten; artikelgroep-filter nog open
- [x] Responsive design (telefoon, tablet, desktop) — basis responsive layout in Blazor UI
- [ ] Export naar Excel of PDF
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
- [x] Unit-, API- en UI-tests (groen) — **63 tests** (21 application + 17 API + 25 UI)
- [ ] MAUI workload installeren (zie [maui-setup.md](./docs/maui-setup.md))

## Fase 2 — Web API

- [x] ASP.NET Core Web API-project opgezet — `TebiDashboard.Api`
- [ ] NetSuite-integratielaag (pilot met kleine dataset) — demo-DB + seeder; live NetSuite-pilot nog open
- [ ] Auth + autorisatie per rol — login/register + admin; JWT + endpoint-bescherming nog open
- [x] DTO's en mapping getest — API- en application-tests
- [x] Swagger/OpenAPI documentatie — `/openapi/v1.json` in Development
- [x] Health check endpoint — `GET /api/health`

## Fase 3 — Dashboard (Must have)

- [x] Gedeelde Blazor UI-bibliotheek (`TebiDashboard.UI`)
- [ ] MAUI Blazor Hybrid app (Windows als eerste target) — project scaffold; build/demo nog open
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
- [ ] Export Excel/PDF
- [x] Rollenbeheer in UI — admin-dashboard

## Fase 5 — Testen

- [x] Unit- en integration tests API — 38 tests (application + API)
- [ ] UI-tests op meerdere apparaten — 25 bUnit-tests; fysieke Android/Windows-demo nog open
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
- [x] Min. 3 zinnige unit tests (groen) — **38 unit/integration tests**
- [x] Min. 3 zinnige UI/device running tests — **25 bUnit UI tests** (+ device na MAUI workload)
- [x] OO-principes & patterns gedocumenteerd ([oo-and-patterns.md](./docs/oo-and-patterns.md))
- [x] Architectuurstijl gemotiveerd ([ONTWERP.md](./docs/ONTWERP.md) ADR's)
- [x] Installatie- & gebruikershandleiding ([installation.md](./docs/installation.md))

### Functioneel

- [x] User stories opgesteld ([user-stories.md](./docs/user-stories.md))
- [x] Definition of Done opgesteld ([definition-of-done.md](./docs/definition-of-done.md))
- [ ] US-01 t/m US-11 geïmplementeerd en Done — zie status in [user-stories.md](./docs/user-stories.md); US-04, US-07 nog open; US-10/11 gedeeltelijk
- [x] US-12 t/m US-14 (login, platform, dashboard home) — login + dashboard Done; US-13 platform-demo nog open

---

## Risico's (uit adviesrapport)

| Risico | Maatregel | Status |
|--------|-----------|--------|
| NetSuite-data niet geschikt voor dashboards | Data-mapping workshop (zie `docs/netsuite-data-mapping.md`) | [ ] |
| API-koppeling werkt niet goed | Pilot met kleine dataset | [~] demo-DB actief; live NetSuite nog niet |
| Dashboard niet duidelijk voor gebruikers | UX-test met eindgebruikers | [ ] |
| Onveilige verwerking gevoelige data | Rollen, rechten, beveiligde API-laag | [~] basis auth; JWT/hardening nog open |
| Project te groot door extra wensen | Eerst Must have, daarna Should/Could | [x] |
