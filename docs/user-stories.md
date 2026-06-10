# User Stories — TEBI Dashboard

User stories voor de .NET MAUI-portfolio-app, afgeleid van het [adviesrapport](../LU2%20AdviesRapportage.docx) (MoSCoW Must have).

> Format: **Als** [rol] **wil ik** [functionaliteit] **zodat** [waarde]  
> Acceptatiecriteria in Given/When/Then-stijl waar mogelijk.

---

## Must have (portfolio MVP)

### US-01 — Dashboard overzicht

**Als** verkoper  
**wil ik** een dashboard met KPI-kaarten (omzet, afzet, doel-%)  
**zodat** ik in één oogopslag mijn prestaties zie.

**Acceptatiecriteria:**
- [ ] Given ingelogde gebruiker, When dashboard opent, Then KPI-kaarten tonen omzet, afzet en doel-percentage
- [ ] Given geselecteerde periode, When data laadt, Then waarden komen van de Web API
- [ ] Given API-fout, When dashboard laadt, Then foutmelding wordt getoond

**DoD:** [definition-of-done.md](./definition-of-done.md)

---

### US-02 — Omzet bekijken

**Als** management-gebruiker  
**wil ik** omzet per periode in een grafiek zien  
**zodat** ik trends kan herkennen.

**Acceptatiecriteria:**
- [ ] Given periode-filter, When omzetpagina opent, Then lijn- of staafgrafiek toont omzet
- [ ] Given detail nodig, When gebruiker scrollt, Then tabel met regels is zichtbaar
- [ ] Given vergelijking met doel, When doel beschikbaar is, Then doellijn of indicator zichtbaar

---

### US-03 — Afzet per artikel

**Als** inkoper  
**wil ik** afzet per artikel/artikelgroep zien  
**zodat** ik weet welke producten goed verkopen.

**Acceptatiecriteria:**
- [ ] Given artikelgroep-filter, When afzetpagina opent, Then top-artikelen worden getoond
- [ ] Given filter wijziging, When gebruiker selecteert groep, Then data ververst

---

### US-04 — Trendanalyse

**Als** management-gebruiker  
**wil ik** omzet/afzet vergelijken tussen periodes  
**zodat** ik groei of daling kan onderbouwen.

**Acceptatiecriteria:**
- [ ] Given twee periodes, When trendpagina opent, Then vergelijking grafisch zichtbaar
- [ ] Given stijgende trend, When summary berekend, Then indicator "up" getoond

---

### US-05 — Doelen vs. actueel

**Als** verkoper  
**wil ik** mijn actuele omzet naast het gestelde doel zien  
**zodat** ik weet of ik op koers lig.

**Acceptatiecriteria:**
- [ ] Given doelstellingen in API, When doelenpagina opent, Then percentage en visuele status (groen/oranje/rood)
- [ ] Given geen doel, When pagina laadt, Then lege staat met uitleg

---

### US-06 — Filters

**Als** gebruiker  
**wil ik** filteren op periode, klant en artikelgroep  
**zodat** ik gericht data kan bekijken.

**Acceptatiecriteria:**
- [ ] Given filtercomponent, When periode gekozen, Then alle dashboard-pagina's respecteren filter
- [ ] Given klant-dropdown, When klant geselecteerd, Then data gefilterd op klant

---

### US-07 — Inloggen (beveiliging)

**Als** TEBI-medewerker  
**wil ik** veilig inloggen  
**zodat** alleen geautoriseerde personen bedrijfsdata zien.

**Acceptatiecriteria:**
- [ ] Given geen token, When app start, Then login-scherm getoond
- [ ] Given geldige credentials, When login, Then JWT opgeslagen en dashboard bereikbaar
- [ ] Given ongeldige credentials, When login, Then foutmelding zonder data-lek

---

### US-08 — Multi-platform gebruik

**Als** gebruiker  
**wil ik** de app op Windows én Android gebruiken  
**zodat** ik thuis, op kantoor en onderweg inzicht heb.

**Acceptatiecriteria:**
- [ ] Given Windows-build, When app start, Then UI correct en bruikbaar
- [ ] Given Android-build, When app start, Then UI responsive en bruikbaar
- [ ] Given zelfde API-config, When beide platformen, Then zelfde data getoond

---

## Should have (backlog)

| ID | Story | Prioriteit |
|----|-------|------------|
| US-09 | Auto-refresh dashboard | Should |
| US-10 | Export naar Excel/PDF | Should |
| US-11 | Rollen bepalen zichtbare menu's | Should |

## Could have (backlog)

| ID | Story | Prioriteit |
|----|-------|------------|
| US-12 | Persoonlijk dashboard / widgets | Could |
| US-13 | Notificaties bij doel-afwijking | Could |
| US-14 | Donkere modus | Could |

---

## Story map → modules

| Story | MAUI page | API endpoint |
|-------|-----------|--------------|
| US-01 | `DashboardHome.razor` | `GET /api/dashboard/summary` |
| US-02 | `RevenuePage.razor` | `GET /api/revenue` |
| US-03 | `SalesVolumePage.razor` | `GET /api/sales-volume` |
| US-04 | `TrendsPage.razor` | `GET /api/trends` |
| US-05 | `GoalsPage.razor` | `GET /api/goals` |
| US-06 | `FilterBar.razor` (shared) | `GET /api/filters/*` |
| US-07 | `LoginPage.razor` | `POST /api/auth/login` |
| US-08 | Platform targets | — |
