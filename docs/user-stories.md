# User Stories — TEBI Dashboard

User stories voor de .NET MAUI-portfolio-app.

**Bronnen:**
- [LU2 AdviesRapportage.docx](../LU2%20AdviesRapportage.docx) — MoSCoW Must have
- [Jorik Leemans Voorstelformulier .NET MAUI app.docx](../Jorik%20Leemans%20Voorstelformulier%20.NET%20MAUI%20app.docx) — ~10 stories + device features

> Format: **Als** [rol] **wil ik** [functionaliteit] **zodat** [waarde]  
> DoD: [definition-of-done.md](./definition-of-done.md)  
> **Status:** Done = acceptatiecriteria behaald · Partial = deels gebouwd · Open = nog niet gestart

---

## Overzicht (11 stories uit voorstelformulier)

| ID | Rol | Story | Prioriteit | Status |
|----|-----|-------|------------|--------|
| US-01 | Directeur | Omzet inzicht | Must | **Done** |
| US-02 | Directeur | Doelen per periode | Must | **Done** |
| US-03 | Inkoop | Veel verkocht + lage voorraad | Must | **Done** |
| US-04 | Inkoop | Meest verkochte artikelen | Must | **Open** |
| US-05 | Voorraad | Artikel op meerdere locaties | Must | **Done** |
| US-06 | Voorraad | Openstaande transacties | Must | **Done** |
| US-07 | Verkoop | Populaire artikelen | Must | **Open** |
| US-08 | Verkoop | Doelen per artikel | Must | **Done** |
| US-09 | Verkoop | Op schema lopen bij doel | Must | **Done** |
| US-10 | Financieel | Uitgavenoverzicht | Must | **Partial** |
| US-11 | Gebruiker | Barcode scannen → artikel | Must | **Partial** |

## Technische enablers (portfolio)

| ID | Story | Prioriteit | Status |
|----|-------|------------|--------|
| US-12 | Veilig inloggen | Must | **Partial** |
| US-13 | Windows + Android | Must (LU-3) | **Partial** |
| US-14 | Dashboard home (KPI's) | Must | **Done** |
| US-15 | Trendanalyse | Should | **Partial** |

---

## US-01 — Omzet inzicht (Directeur) — **Done**

**Als** directeur  
**wil ik** op een gemakkelijke manier inzicht krijgen in de omzet van mijn bedrijf  
**zodat** ik prestaties kan beoordelen en beslissingen kan nemen.

**Acceptatiecriteria:**
- [x] Given ingelogde directeur, When omzetpagina opent, Then grafiek en totalen zichtbaar — `DashboardHome.razor`: revenue KPI, bar chart, omzet-tabel
- [x] Given periode-filter, When filter wijzigt, Then omzetdata ververst via API — `PeriodFilterBar` + `GET /api/dashboard/overview`
- [x] Given API-fout, Then foutmelding zonder crash — error state op dashboard

**API:** `GET /api/revenue?from=&to=` · `GET /api/dashboard/overview`

---

## US-02 — Doelen per periode (Directeur) — **Done**

**Als** directeur  
**wil ik** inzicht krijgen in de doelen die gesteld zijn voor een specifieke periode  
**zodat** ik weet wat de organisatie moet halen.

**Acceptatiecriteria:**
- [x] Given geselecteerd jaar/kwartaal, When doelenpagina opent, Then doelen per periode zichtbaar — `GoalsPage.razor` met jaar/kwartaal-filter, bar chart, goal cards
- [x] Given geen doelen, Then lege staat met uitleg

**API:** `GET /api/goals?year=&period=`

---

## US-03 — Veel verkocht + lage voorraad (Inkoop) — **Done**

**Als** inkoopmedewerker  
**wil ik** snel kunnen inzien welke artikelen veel verkocht worden en lage voorraad hebben  
**zodat** ik tijdig kan bijbestellen.

**Acceptatiecriteria:**
- [x] Given voorraad- en verkoopdata, When inkoop-dashboard opent, Then lijst "kritieke artikelen" zichtbaar — low-stock KPI op dashboard + `/inventory`
- [x] Given drempelwaarde voorraad, When artikel onder drempel én hoge verkoop, Then artikel gemarkeerd — `IsLowStock` badge op inventory- en itemdetail

**API:** `GET /api/inventory/low-stock`

---

## US-04 — Meest verkochte artikelen (Inkoop) — **Open**

**Als** inkoopmedewerker  
**wil ik** snel kunnen zien wat de meest verkochte artikelen zijn  
**zodat** ik inkoop kan afstemmen op vraag.

**Acceptatiecriteria:**
- [ ] Given periode, When bestsellers-pagina opent, Then top-N artikelen gesorteerd op afzet
- [ ] Given filter artikelgroep, When toegepast, Then lijst gefilterd

**API:** `GET /api/sales-volume/top?from=&to=&limit=20` — service-methode `GetTopSellingItemsAsync` bestaat; endpoint en UI nog te bouwen

---

## US-05 — Meerdere opslaglocaties (Voorraad) — **Done**

**Als** voorraadmedewerker  
**wil ik** snel kunnen zien of een artikel op meerdere plekken op het terrein ligt  
**zodat** ik voorraad kan consolideren of correct kan picken.

**Acceptatiecriteria:**
- [x] Given artikel geselecteerd, When voorraad-detail opent, Then alle locaties met hoeveelheid zichtbaar — `ItemDetailPage.razor`
- [x] Given artikel op 1 locatie, Then enkele locatie getoond

**API:** `GET /api/items/{itemId}` (inclusief locaties)

---

## US-06 — Openstaande transacties (Voorraad) — **Done**

**Als** voorraadmedewerker  
**wil ik** snel kunnen zien of een artikel veel openstaande transacties heeft  
**zodat** ik weet waar logistieke knelpunten zitten.

**Acceptatiecriteria:**
- [x] Given artikel, When transactie-overzicht opent, Then open orders/reserveringen geteld en getoond — KPI + tabel op `ItemDetailPage.razor`
- [x] Given geen open transacties, Then "0 open" status

**API:** `GET /api/inventory/{itemId}/open-transactions`

---

## US-07 — Populaire artikelen (Verkoop) — **Open**

**Als** verkoopmedewerker  
**wil ik** snel kunnen zien wat populaire artikelen zijn  
**zodat** ik klanten gericht kan adviseren.

**Acceptatiecriteria:**
- [ ] When verkoop-overzicht opent, Then populaire artikelen (periode) zichtbaar
- [ ] Overlap met US-04 acceptabel; UI gericht op verkoper-context

**API:** `GET /api/sales-volume/popular?from=&to=` — nog te bouwen

---

## US-08 — Doelen per artikel (Verkoop) — **Done**

**Als** verkoopmedewerker  
**wil ik** eenvoudig kunnen zien aan welke artikelen doelen zijn gesteld  
**zodat** ik weet waar focus ligt.

**Acceptatiecriteria:**
- [x] When doelen-per-artikel pagina opent, Then artikelen met doelstelling in tabel — sectie "Item goals" op `GoalsPage.razor`
- [x] Given artikel zonder doel, Then niet in lijst of als "geen doel"

**API:** `GET /api/goals?year=&period=` (item goals in response)

---

## US-09 — Op schema bij doel (Verkoop) — **Done**

**Als** verkoopmedewerker  
**wil ik** eenvoudig kunnen zien of we op schema lopen bij een specifiek doel  
**zodat** ik mijn verkoop kan bijsturen.

**Acceptatiecriteria:**
- [x] Given doel + actuele voortgang, Then status groen/oranje/rood — `GoalProgressCard` (on-track / ahead / off-track)
- [x] Given percentage, Then voortgangsbalk zichtbaar

**API:** `GET /api/goals?year=&period=` (progress berekend in `DashboardService`)

---

## US-10 — Uitgavenoverzicht (Financieel) — **Partial**

**Als** financieel medewerker  
**wil ik** gemakkelijk een overzicht kunnen zien van de uitgaven in het bedrijf  
**zodat** ik kosten kan monitoren (complementair aan omzet/winst).

**Acceptatiecriteria:**
- [x] When uitgavenpagina opent, Then uitgaven per categorie/periode zichtbaar — expenses-tabel op `DashboardHome.razor` + leveranciersdetail
- [x] Given periode-filter, Then data gefilterd
- [ ] Dedicated `/expenses`-pagina voor financiële rol (nu verspreid over dashboard en suppliers)

**API:** `GET /api/expenses?from=&to=`

---

## US-11 — Barcode scannen (Terrein) — **Partial**

**Als** gebruiker op het terrein  
**wil ik** een barcode kunnen scannen om een artikel snel op te zoeken in het dashboard  
**zodat** ik direct voorraad, verkoop/inkoop en open transacties zie.

**Acceptatiecriteria:**
- [ ] Given Android device, When camera scan, Then barcode herkend — `BarcodeScannerService` is nog stub
- [ ] Given Windows + USB-scanner, When scan, Then artikel opgezocht
- [x] Given geldige barcode, When lookup, Then artikeldetail met US-05/06 data — `GET /api/items/by-barcode/{barcode}` + `ItemDetailPage`
- [x] Given onbekende barcode, Then duidelijke foutmelding — API 404 + UI error state

**API:** `GET /api/items/by-barcode/{barcode}`  
**MAUI:** `IBarcodeScannerService` (ZXing.Net.Maui package aanwezig; UI nog te koppelen)

---

## US-12 — Inloggen — **Partial**

**Als** TEBI-medewerker **wil ik** veilig inloggen **zodat** alleen geautoriseerden data zien.

- [x] Login en registratie — `Login.razor`, `Register.razor`, `POST /api/auth/login`
- [x] Rollen in gebruik — Admin, Finance, Sales, Inventory, Management (`Roles.cs`)
- [ ] JWT-auth — huidige implementatie gebruikt demo-token; echte JWT nog open
- [x] Admin kan rollen beheren — `AdminDashboard.razor`

**API:** `POST /api/auth/login`

---

## US-13 — Multi-platform (Windows + Android) — **Partial**

**Als** medewerker **wil ik** de app op kantoor (Windows) en terrein (Android) gebruiken.

- [x] Zelfde Blazor UI gedeeld tussen Web en MAUI — `TebiDashboard.UI`
- [ ] Zelfde functionaliteit getest op MAUI Windows + Android
- [ ] LU-3: demonstratie op beide platformen (video of live demo)

---

## US-14 — Dashboard home — **Done**

**Als** gebruiker **wil ik** een startpagina met KPI's **zodat** ik snel een totaalbeeld krijg.

- [x] KPI: omzet, afzet, doel-%, low-stock alerts — `DashboardHome.razor`
- [x] Revenue chart en sales/expenses in periode

**API:** `GET /api/dashboard/overview`

---

## US-15 — Trendanalyse (Should) — **Partial**

- [x] Dagelijkse revenue-lijngrafiek op goals-pagina — `LineChart` component
- [ ] Vergelijkende trendanalyse over meerdere periodes (backlog)

---

## Story map (actuele implementatie)

| Story | Status | Blazor page | API |
|-------|--------|-------------|-----|
| US-01 | Done | `DashboardHome.razor` | `/api/dashboard/overview`, `/api/revenue` |
| US-02 | Done | `GoalsPage.razor` | `/api/goals` |
| US-03 | Done | `DashboardHome.razor`, `InventoryPage.razor` | `/api/inventory/low-stock` |
| US-04 | Open | — | `/api/sales-volume/top` (nog te bouwen) |
| US-05 | Done | `ItemDetailPage.razor` | `/api/items/{id}` |
| US-06 | Done | `ItemDetailPage.razor` | `/api/inventory/{id}/open-transactions` |
| US-07 | Open | — | `/api/sales-volume/popular` (nog te bouwen) |
| US-08 | Done | `GoalsPage.razor` (item goals) | `/api/goals` |
| US-09 | Done | `GoalsPage.razor` (`GoalProgressCard`) | `/api/goals` |
| US-10 | Partial | `DashboardHome.razor`, `SupplierDetailPage.razor` | `/api/expenses` |
| US-11 | Partial | `ItemDetailPage.razor` (+ MAUI scanner stub) | `/api/items/by-barcode/{code}` |
| US-12 | Partial | `Login.razor`, `Register.razor`, `AdminDashboard.razor` | `/api/auth/login` |
| US-14 | Done | `DashboardHome.razor` | `/api/dashboard/overview` |

### Extra modules (niet in oorspronkelijk voorstelformulier)

| Module | Blazor page | API |
|--------|-------------|-----|
| Klanten | `CustomersPage.razor`, `CustomerDetailPage.razor` | `/api/customers` |
| Leveranciers | `SuppliersPage.razor`, `SupplierDetailPage.razor` | `/api/suppliers` |

---

## Backlog (LU2 Should/Could)

| ID | Story | Status |
|----|-------|--------|
| US-15 | Trendanalyse over periodes | Partial |
| US-16 | Auto-refresh dashboard | Open |
| US-17 | Export Excel/PDF | Open |
| US-18 | Donkere modus | Open |
