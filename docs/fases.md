# Implementatiefases

Uitwerking van de 6 fases uit [LU2 AdviesRapportage.docx](../LU2%20AdviesRapportage.docx), vertaald naar concrete deliverables voor dit project.

---

## Fase 1 — Voorbereiding en ontwerpen

**Doel:** Duidelijkheid over data, gebruikers, architectuur en planning.

### Deliverables

| Item | Document / artefact | Status |
|------|---------------------|--------|
| Adviesrapport | `LU2 AdviesRapportage.docx` | ✅ |
| Technisch ontwerp | `docs/ONTWERP.md` | ✅ |
| Architectuur | `docs/architectuur.md` | ✅ |
| Data-mapping | `docs/netsuite-data-mapping.md` | ✅ (concept) |
| Checklist | `CHECKLIST.md` | ✅ |
| Data-inventarisatie TEBI | Workshop met opdrachtgever | ⬜ |
| Rollen/rechtenmatrix | Toevoegen aan `ONTWERP.md` sectie 4 | ⬜ |
| Solution-structuur | `src/TebbiDashboard.sln` | ⬜ |

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
6. Endpoints: `/health`, `/dashboard/summary`, `/omzet`, `/afzet`
7. Swagger/OpenAPI
8. Unit tests voor mapping en services

### Exit-criteria

- API retourneert echte of mock-data via alle Must have-endpoints
- Geen NetSuite-credentials in front-end
- Auth werkt met minimaal 2 rollen (Admin, ReadOnly)

---

## Fase 3 — Ontwikkelen van het dashboard

**Doel:** Must have-visualisaties live in MAUI en Web.

### Taken

1. Razor Class Library (`TebbiDashboard.UI`) met MudBlazor
2. Pagina's: Home (KPI), Omzet, Afzet, Trends, Doelen
3. Gedeelde filter-component (periode, klant, artikelgroep)
4. .NET MAUI Blazor Hybrid app (Windows eerst)
5. Blazor Web App voor browser
6. Responsive layout
7. Koppeling met API via HttpClient

### Exit-criteria

- Omzet, afzet, trends en doelen zichtbaar in grafieken/tabellen
- App werkt op Windows (MAUI) en in browser (Web)
- Filters beïnvloeden data correct

---

## Fase 4 — Extra functionaliteiten (Should have)

**Doel:** Verrijking na stabiele basis.

### Taken

1. Automatische data-refresh (timer / SignalR)
2. Uitgebreide zoek- en filteropties
3. Export Excel (ClosedXML) en PDF (QuestPDF of vergelijkbaar)
4. Rollenbeheer zichtbaar in UI (menu op basis van rol)

### Exit-criteria

- Gebruiker kan data exporteren
- Dashboard ververst zonder handmatige page reload
- Verschillende rollen zien verschillende menu's/data

---

## Fase 5 — Testen en verbeteren

**Doel:** Kwaliteit en gebruikersfeedback.

### Testplan

| Type | Focus |
|------|-------|
| Unit tests | Application services, mapping |
| Integration tests | API + mock NetSuite |
| UI tests | Kritieke flows (login, filter, dashboard laden) |
| Device tests | Windows, Android, browser (Chrome/Edge) |
| Security | JWT, CORS, geen credential leaks |
| Performance | API responstijd < 2s voor summary |
| UX | Feedbackronde met 3–5 TEBI-gebruikers |

### Exit-criteria

- Geen kritieke bugs open
- Eindgebruikers akkoord met dashboard-digitaliteit
- Security checklist afgerond

---

## Fase 6 — Ingebruikname en doorontwikkeling

**Doel:** Productie-ready oplevering.

### Taken

1. Deployment API + Web (Azure/IIS)
2. MAUI distributie (MSIX / APK)
3. Korte gebruikershandleiding (PDF of in-app help)
4. Monitoring / logging in productie
5. Backlog Could have: persoonlijke dashboards, notificaties, dark mode

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
