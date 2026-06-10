# User Stories — TEBI Dashboard

User stories voor de .NET MAUI-portfolio-app.

**Bronnen:**
- [LU2 AdviesRapportage.docx](../LU2%20AdviesRapportage.docx) — MoSCoW Must have
- [Jorik Leemans Voorstelformulier .NET MAUI app.docx](../Jorik%20Leemans%20Voorstelformulier%20.NET%20MAUI%20app.docx) — ~10 stories + device features

> Format: **Als** [rol] **wil ik** [functionaliteit] **zodat** [waarde]  
> DoD: [definition-of-done.md](./definition-of-done.md)

---

## Overzicht (11 stories uit voorstelformulier)

| ID | Rol | Story | Prioriteit |
|----|-----|-------|------------|
| US-01 | Directeur | Omzet inzicht | Must |
| US-02 | Directeur | Doelen per periode | Must |
| US-03 | Inkoop | Veel verkocht + lage voorraad | Must |
| US-04 | Inkoop | Meest verkochte artikelen | Must |
| US-05 | Voorraad | Artikel op meerdere locaties | Must |
| US-06 | Voorraad | Openstaande transacties | Must |
| US-07 | Verkoop | Populaire artikelen | Must |
| US-08 | Verkoop | Doelen per artikel | Must |
| US-09 | Verkoop | Op schema lopen bij doel | Must |
| US-10 | Financieel | Uitgavenoverzicht | Must |
| US-11 | Gebruiker | Barcode scannen → artikel | Must |

## Technische enablers (portfolio)

| ID | Story | Prioriteit |
|----|-------|------------|
| US-12 | Veilig inloggen | Must |
| US-13 | Windows + Android | Must (LU-3) |
| US-14 | Dashboard home (KPI's) | Must |
| US-15 | Trendanalyse | Should |

---

## US-01 — Omzet inzicht (Directeur)

**Als** directeur  
**wil ik** op een gemakkelijke manier inzicht krijgen in de omzet van mijn bedrijf  
**zodat** ik prestaties kan beoordelen en beslissingen kan nemen.

**Acceptatiecriteria:**
- [ ] Given ingelogde directeur, When omzetpagina opent, Then grafiek en totalen zichtbaar
- [ ] Given periode-filter, When filter wijzigt, Then omzetdata ververst via API
- [ ] Given API-fout, Then foutmelding zonder crash

**API:** `GET /api/revenue?from=&to=`

---

## US-02 — Doelen per periode (Directeur)

**Als** directeur  
**wil ik** inzicht krijgen in de doelen die gesteld zijn voor een specifieke periode  
**zodat** ik weet wat de organisatie moet halen.

**Acceptatiecriteria:**
- [ ] Given geselecteerd jaar/kwartaal, When doelenpagina opent, Then doelen per periode zichtbaar
- [ ] Given geen doelen, Then lege staat met uitleg

**API:** `GET /api/goals?year=&period=`

---

## US-03 — Veel verkocht + lage voorraad (Inkoop)

**Als** inkoopmedewerker  
**wil ik** snel kunnen inzien welke artikelen veel verkocht worden en lage voorraad hebben  
**zodat** ik tijdig kan bijbestellen.

**Acceptatiecriteria:**
- [ ] Given voorraad- en verkoopdata, When inkoop-dashboard opent, Then lijst "kritieke artikelen" zichtbaar
- [ ] Given drempelwaarde voorraad, When artikel onder drempel én hoge verkoop, Then artikel gemarkeerd

**API:** `GET /api/inventory/low-stock?highSales=true`

---

## US-04 — Meest verkochte artikelen (Inkoop)

**Als** inkoopmedewerker  
**wil ik** snel kunnen zien wat de meest verkochte artikelen zijn  
**zodat** ik inkoop kan afstemmen op vraag.

**Acceptatiecriteria:**
- [ ] Given periode, When bestsellers-pagina opent, Then top-N artikelen gesorteerd op afzet
- [ ] Given filter artikelgroep, When toegepast, Then lijst gefilterd

**API:** `GET /api/sales-volume/top?from=&to=&limit=20`

---

## US-05 — Meerdere opslaglocaties (Voorraad)

**Als** voorraadmedewerker  
**wil ik** snel kunnen zien of een artikel op meerdere plekken op het terrein ligt  
**zodat** ik voorraad kan consolideren of correct kan picken.

**Acceptatiecriteria:**
- [ ] Given artikel geselecteerd, When voorraad-detail opent, Then alle locaties met hoeveelheid zichtbaar
- [ ] Given artikel op 1 locatie, Then enkele locatie getoond

**API:** `GET /api/inventory/{itemId}/locations`

---

## US-06 — Openstaande transacties (Voorraad)

**Als** voorraadmedewerker  
**wil ik** snel kunnen zien of een artikel veel openstaande transacties heeft  
**zodat** ik weet waar logistieke knelpunten zitten.

**Acceptatiecriteria:**
- [ ] Given artikel, When transactie-overzicht opent, Then open orders/reserveringen geteld en getoond
- [ ] Given geen open transacties, Then "0 open" status

**API:** `GET /api/inventory/{itemId}/open-transactions`

---

## US-07 — Populaire artikelen (Verkoop)

**Als** verkoopmedewerker  
**wil ik** snel kunnen zien wat populaire artikelen zijn  
**zodat** ik klanten gericht kan adviseren.

**Acceptatiecriteria:**
- [ ] When verkoop-overzicht opent, Then populaire artikelen (periode) zichtbaar
- [ ] Overlap met US-04 acceptabel; UI gericht op verkoper-context

**API:** `GET /api/sales-volume/popular?from=&to=`

---

## US-08 — Doelen per artikel (Verkoop)

**Als** verkoopmedewerker  
**wil ik** eenvoudig kunnen zien aan welke artikelen doelen zijn gesteld  
**zodat** ik weet waar focus ligt.

**Acceptatiecriteria:**
- [ ] When doelen-per-artikel pagina opent, Then artikelen met doelstelling in tabel
- [ ] Given artikel zonder doel, Then niet in lijst of als "geen doel"

**API:** `GET /api/goals/by-item?period=`

---

## US-09 — Op schema bij doel (Verkoop)

**Als** verkoopmedewerker  
**wil ik** eenvoudig kunnen zien of we op schema lopen bij een specifiek doel  
**zodat** ik mijn verkoop kan bijsturen.

**Acceptatiecriteria:**
- [ ] Given doel + actuele voortgang, Then status groen/oranje/rood
- [ ] Given percentage, Then voortgangsbalk zichtbaar

**API:** `GET /api/goals/progress?goalId=`

---

## US-10 — Uitgavenoverzicht (Financieel)

**Als** financieel medewerker  
**wil ik** gemakkelijk een overzicht kunnen zien van de uitgaven in het bedrijf  
**zodat** ik kosten kan monitoren (complementair aan omzet/winst).

**Acceptatiecriteria:**
- [ ] When uitgavenpagina opent, Then uitgaven per categorie/periode zichtbaar
- [ ] Given periode-filter, Then data gefilterd

**API:** `GET /api/expenses?from=&to=`

---

## US-11 — Barcode scannen (Terrein)

**Als** gebruiker op het terrein  
**wil ik** een barcode kunnen scannen om een artikel snel op te zoeken in het dashboard  
**zodat** ik direct voorraad, verkoop/inkoop en open transacties zie.

**Acceptatiecriteria:**
- [ ] Given Android device, When camera scan, Then barcode herkend
- [ ] Given Windows + USB-scanner, When scan, Then artikel opgezocht
- [ ] Given geldige barcode, When lookup, Then artikeldetail met US-05/06 data
- [ ] Given onbekende barcode, Then duidelijke foutmelding

**API:** `GET /api/items/by-barcode/{barcode}`  
**MAUI:** `IBarcodeScannerService` (ZXing.Net.Maui / platform abstraction)

---

## US-12 — Inloggen

**Als** TEBI-medewerker **wil ik** veilig inloggen **zodat** alleen geautoriseerden data zien.

- [ ] JWT-auth; rollen: Directie, Inkoop, Voorraad, Verkoop, Financieel, Admin

**API:** `POST /api/auth/login`

---

## US-13 — Multi-platform (Windows + Android)

**Als** medewerker **wil ik** de app op kantoor (Windows) en terrein (Android) gebruiken.

- [ ] Zelfde functionaliteit; barcode primair op Android
- [ ] LU-3: demonstratie op beide platformen

---

## US-14 — Dashboard home

**Als** gebruiker **wil ik** een startpagina met KPI's **zodat** ik snel een totaalbeeld krijg.

- [ ] KPI: omzet, afzet, doel-%, optioneel voorraad-waarschuwingen

**API:** `GET /api/dashboard/summary`

---

## Story map

| Story | MAUI / Blazor page | API |
|-------|-------------------|-----|
| US-01 | `RevenuePage.razor` | `/api/revenue` |
| US-02 | `GoalsPage.razor` | `/api/goals` |
| US-03 | `LowStockPage.razor` | `/api/inventory/low-stock` |
| US-04 | `TopSalesPage.razor` | `/api/sales-volume/top` |
| US-05 | `ItemLocationsPage.razor` | `/api/inventory/{id}/locations` |
| US-06 | `OpenTransactionsPage.razor` | `/api/inventory/{id}/open-transactions` |
| US-07 | `PopularItemsPage.razor` | `/api/sales-volume/popular` |
| US-08 | `ItemGoalsPage.razor` | `/api/goals/by-item` |
| US-09 | `GoalProgressPage.razor` | `/api/goals/progress` |
| US-10 | `ExpensesPage.razor` | `/api/expenses` |
| US-11 | `BarcodeScanPage.razor` | `/api/items/by-barcode/{code}` |
| US-12 | `LoginPage.razor` | `/api/auth/login` |
| US-14 | `DashboardHome.razor` | `/api/dashboard/summary` |

---

## Backlog (LU2 Should/Could)

| ID | Story |
|----|-------|
| US-15 | Trendanalyse over periodes |
| US-16 | Auto-refresh dashboard |
| US-17 | Export Excel/PDF |
| US-18 | Donkere modus |
