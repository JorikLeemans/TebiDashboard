# Technisch ontwerp — TEBI Dashboard

**Versie:** 1.0  
**Datum:** juni 2026  
**Bron:** [LU2 AdviesRapportage.docx](../LU2%20AdviesRapportage.docx), [Voorstelformulier](../Jorik%20Leemans%20Voorstelformulier%20.NET%20MAUI%20app.docx)  
**Projectlocatie:** `F:\School\Full-Stack .NET MAUI`

---

## 1. Doel en scope

### Probleem

TEBI gebruikt NetSuite als ERP-systeem. NetSuite bevat alle bedrijfsdata, maar biedt onvoldoende mogelijkheden om omzet, afzet, trends en doelstellingen overzichtelijk te visualiseren en te presenteren aan medewerkers, klanten en leveranciers.

### Gebruikers (voorstelformulier)

Directie, inkoop, voorraadbeheer, verkoop en financiële medewerkers bij TEBI Bestratingsmaterialen.

### Oplossing

Een **dashboard-applicatie als aanvulling** op NetSuite — geen vervanging. Live data via Web API; weergave in Blazor (MAUI Hybrid + Web). Op het terrein: **barcode-scan** voor snelle artikelopzoeking.

### Scope (Must have)

- NetSuite-koppeling via API (**live data**, mock alleen voor dev/test)
- Dashboards: omzet, winst/uitgaven, afzet, trends, doelen
- **Voorraad:** locaties, lage voorraad, openstaande transacties
- **Barcode:** artikel opzoeken via camera/scanner (Android + Windows)
- Filters op periode, klant, leverancier, artikelgroep
- Windows + Android (kantoor + terrein)
- Veilige gegevensverwerking

### Buiten scope (Won't have)

- NetSuite vervangen
- AI-analyses
- Offline functionaliteit

---

## 2. Architectuurbeslissingen (ADR's)

### ADR-001: Blazor Hybrid in .NET MAUI

**Besluit:** Front-end voor native clients via .NET MAUI met BlazorWebView (Blazor Hybrid).

**Reden:** Sluit aan op het advies (Blazor + .NET) en levert één C#/Razor-codebase voor Windows, Android en iOS.

### ADR-002: Gedeelde UI-bibliotheek

**Besluit:** Razor Class Library (`TebiDashboard.UI`) gedeeld door MAUI-app en Blazor Web App.

**Reden:** DRY-principe; wijzigingen in dashboards hoeven maar op één plek.

### ADR-003: Web API als enige NetSuite-toegang

**Besluit:** Clients praten uitsluitend met `TebiDashboard.Api`. Geen NetSuite-credentials in front-end.

**Reden:** Beveiliging, controle over data-uitwisseling, voorkomen van directe toegang tot gevoelige ERP-data.

### ADR-004: Clean Architecture

**Besluit:** Lagen: Api → Application → Infrastructure, met Shared voor DTO's.

**Reden:** Testbaarheid, onderhoudbaarheid, modulaire uitbreiding (rapport: modulair opbouwen).

### ADR-005: Blazor Web App naast MAUI

**Besluit:** Apart Blazor Web-project voor browser-toegang.

**Reden:** Must have "web-based applicatie" uit adviesrapport; MAUI dekt native, Web dekt browser.

### ADR-006: Barcode via MAUI device features

**Besluit:** `IBarcodeScannerService` in MAUI; ZXing.Net.Maui op Android; keyboard wedge + optionele camera op Windows.

**Reden:** Voorstelformulier — terreinmedewerkers scannen artikelbarcodes; unieke barcode per artikel bij TEBI.

### ADR-007: Live NetSuite-data (geen mock in productie)

**Besluit:** Productie gebruikt echte NetSuite API; `MockNetSuiteService` alleen voor lokale ontwikkeling en tests.

**Reden:** Voorstelformulier — realistische bedrijfsvraag; live dashboard is het doel.

---

## 3. Functionele modules

### 3.1 Dashboard Home

- KPI-kaarten: totale omzet, afzet, doel-behalen (%)
- Snelle periode-selectie (vandaag, week, maand, kwartaal, jaar)
- Samenvatting trend (stijgend/dalend t.o.v. vorige periode)

### 3.2 Omzet

- Lijn- of staafgrafiek omzet per periode
- Tabel met detailregels
- Vergelijking met doelstelling

### 3.3 Afzet

- Afzet per artikel / artikelgroep
- Top-N artikelen
- Filter op artikelgroep (klasse / prijsbepalingsgroep)

### 3.4 Trends

- Vergelijking meerdere periodes
- Trendlijn over maanden/kwartalen

### 3.5 Doelen

- Actuele cijfers vs. gestelde doelen
- Visuele indicatie (groen/oranje/rood)

### 3.6 Filters (gedeeld)

| Filter | Type | Bron |
|--------|------|------|
| Periode | Datumrange | Gebruikersinvoer |
| Klant | Dropdown / zoek | NetSuite |
| Leverancier | Dropdown / zoek | NetSuite |
| Artikelgroep | Dropdown | NetSuite |

### 3.7 Voorraad (voorstelformulier)

- Lage voorraad + hoge verkoop (inkoop-alert)
- Opslaglocaties per artikel (meerdere plekken op terrein)
- Openstaande transacties per artikel
- Voorraadwaarde in KPI (optioneel)

### 3.8 Barcode scan (MAUI device feature)

- Camera-scan op Android (terrein)
- USB-barcode scanner op Windows (keyboard wedge)
- Lookup: artikel + voorraad + open transacties + verkoop/inkoop-context

### 3.9 Uitgaven / financieel

- Uitgaven per categorie en periode
- Complementair aan omzet/winst-inzicht voor directie/financieel

---

## 4. Technische keuzes

| Onderdeel | Keuze | Toelichting |
|-----------|-------|-------------|
| Runtime | **.NET 10** / **C# 14** | LU-3 portfolio-eis; volg actuele .NET 10 + MAUI-richtlijnen |
| Taal broncode | **Engels** | LU-3: identifiers, classes, methods, comments |
| Codestijl | `.editorconfig` + `dotnet format` | LU-3: consistente stijl |
| Demo-platformen | **Windows + Android** | LU-3: demonstratie op 2 platformen |
| Auth | JWT Bearer + rollen | `Admin`, `Management`, `Verkoper`, `Inkoop`, `ReadOnly` |
| NetSuite | REST / SuiteTalk API | OAuth 1.0 of 2.0 (afhankelijk TEBI-configuratie) |
| UI-framework | MudBlazor | Gratis, goede Blazor-charts en componenten |
| Cache | IMemoryCache | Should have: auto-refresh; later Redis mogelijk |
| Opslag (optioneel) | SQLite / SQL Server | Geaggregeerde snapshots voor snellere dashboards |
| Tests | xUnit; min. 3 unit + 3 UI/device | LU-3 portfolio; zie [lu3-assessment.md](./lu3-assessment.md) |
| Secrets | User Secrets (dev) / Key Vault (prod) | Geen credentials in source control |

---

## 5. API-contract

Basis-URL: `https://{host}/api`

### Endpoints (Must have)

```
GET  /health
GET  /dashboard/summary
GET  /revenue?from=&to=&customerId=
GET  /sales-volume/top?from=&to=&limit=
GET  /sales-volume/popular?from=&to=
GET  /trends?from=&to=&interval=
GET  /goals?year=&period=
GET  /goals/by-item?period=
GET  /goals/progress?goalId=
GET  /expenses?from=&to=
GET  /inventory/low-stock?highSales=true
GET  /inventory/{itemId}/locations
GET  /inventory/{itemId}/open-transactions
GET  /items/by-barcode/{barcode}
GET  /filters/customers|suppliers|product-groups
POST /auth/login
```

### Voorbeeld-response: `/dashboard/summary`

```json
{
  "omzet": 1250000.00,
  "afzet": 8420,
  "doelPercentage": 87.5,
  "periode": { "from": "2026-01-01", "to": "2026-06-30" },
  "trendOmzet": "stijgend"
}
```

---

## 6. Beveiliging

1. **HTTPS** verplicht in productie
2. **JWT** op alle data-endpoints (behalve `/health` en `/auth/login`)
3. **Autorisatie** per rol — bijv. `ReadOnly` mag alleen lezen, geen export
4. **CORS** alleen voor bekende Web-app origins
5. **Rate limiting** op API
6. **NetSuite-credentials** alleen in Infrastructure-laag, nooit in clients
7. **Logging** zonder gevoelige bedrijfsdata in plain text

---

## 7. Risico's en mitigatie

| Risico | Impact | Mitigatie |
|--------|--------|-----------|
| NetSuite-data niet dashboard-klaar | Hoog | Workshop met TEBI; mapping-doc (`netsuite-data-mapping.md`) |
| API-koppeling faalt | Hoog | Pilot met kleine dataset; mock-data voor ontwikkeling |
| Dashboard onduidelijk | Medium | UX-test met verkopers/inkopers |
| Onveilige dataverwerking | Hoog | ADR-003; security review voor livegang |
| Scope creep | Medium | MoSCoW + CHECKLIST; Should/Could pas na Must have |

---

## 8. Fasering

Zie [fases.md](./fases.md) voor de volledige uitwerking van de 6 implementatiefases uit het adviesrapport.

| Fase | Focus |
|------|-------|
| 1 | Voorbereiding, ontwerp, data-inventarisatie |
| 2 | Web API + NetSuite-koppeling |
| 3 | Dashboard Must have |
| 4 | Should have (refresh, export, rollen) |
| 5 | Testen |
| 6 | Livegang + Could have backlog |

---

## 9. Gerelateerde documenten

- [architectuur.md](./architectuur.md)
- [netsuite-data-mapping.md](./netsuite-data-mapping.md)
- [fases.md](./fases.md)
- [lu3-assessment.md](./lu3-assessment.md) — LU-3 portfolio-eisen
- [user-stories.md](./user-stories.md)
- [definition-of-done.md](./definition-of-done.md)
- [oo-and-patterns.md](./oo-and-patterns.md)
- [installation.md](./installation.md)
- [proposal-form.md](./proposal-form.md) — voorstelformulier TEBI
- [CHECKLIST.md](../CHECKLIST.md)
