# TEBI Dashboard — Voortgang

Afvinklijst gekoppeld aan het [adviesrapport](./LU2%20AdviesRapportage.docx) en het [technisch ontwerp](./docs/ONTWERP.md).

> Vink items af door `[ ]` te wijzigen naar `[x]` in een commit of pull request.

---

## MoSCoW — Must have

- [ ] NetSuite-koppeling via Web API (geen directe client-toegang)
- [ ] Dashboard met omzet, afzet, trends en doelen
- [ ] Duidelijke visualisaties (grafieken en tabellen)
- [ ] Gebruiksvriendelijke interface
- [ ] Toegankelijk via browser (Blazor Web)
- [ ] Toegankelijk via native app (MAUI Blazor Hybrid)
- [ ] Veilige gegevensverwerking (auth, HTTPS, rollen)

## MoSCoW — Should have

- [ ] Automatische data-updates (refresh / polling)
- [ ] Filters en zoekmogelijkheden (periode, klant, artikelgroep)
- [ ] Responsive design (telefoon, tablet, desktop)
- [ ] Export naar Excel of PDF
- [ ] Rollen en rechtenbeheer in UI

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
- [ ] NetSuite-datavelden inventariseren met TEBI
- [ ] Rollen en rechtenmatrix vastleggen
- [x] `docs/ONTWERP.md` opgesteld
- [x] `docs/architectuur.md` opgesteld
- [x] `docs/netsuite-data-mapping.md` opgesteld
- [x] `docs/fases.md` opgesteld
- [x] `CHECKLIST.md` en `README.md` aangemaakt
- [x] Git-repository geïnitialiseerd (branch `main`, `.gitignore`)
- [ ] Eerste commit en push naar GitHub
- [ ] Solution-structuur aanmaken
- [ ] CI-basis (build + test pipeline)

## Fase 2 — Web API

- [ ] ASP.NET Core Web API-project opgezet
- [ ] NetSuite-integratielaag (pilot met kleine dataset)
- [ ] Auth + autorisatie per rol
- [ ] DTO's en mapping getest
- [ ] Swagger/OpenAPI documentatie
- [ ] Health check endpoint

## Fase 3 — Dashboard (Must have)

- [ ] Gedeelde Blazor UI-bibliotheek (`TebbiDashboard.UI`)
- [ ] MAUI Blazor Hybrid app (Windows als eerste target)
- [ ] Blazor Web App voor browser
- [ ] KPI-kaarten op dashboard home
- [ ] Omzet-, afzet-, trend- en doel-visualisaties
- [ ] Basis filters (periode, klant, artikelgroep)
- [ ] Responsive layout

## Fase 4 — Should have

- [ ] Automatische data-refresh
- [ ] Uitgebreide filters en zoeken
- [ ] Export Excel/PDF
- [ ] Rollenbeheer in UI

## Fase 5 — Testen

- [ ] Unit- en integration tests API
- [ ] UI-tests op meerdere apparaten
- [ ] Beveiligings- en performancetest
- [ ] Feedbackronde eindgebruikers (TEBI)

## Fase 6 — Livegang

- [ ] Deployment API + Web
- [ ] MAUI distributie (MSIX / APK)
- [ ] Gebruikershandleiding
- [ ] Doorontwikkeling Could have-items

---

## Risico's (uit adviesrapport)

| Risico | Maatregel | Status |
|--------|-----------|--------|
| NetSuite-data niet geschikt voor dashboards | Data-mapping workshop (zie `docs/netsuite-data-mapping.md`) | [ ] |
| API-koppeling werkt niet goed | Pilot met kleine dataset | [ ] |
| Dashboard niet duidelijk voor gebruikers | UX-test met eindgebruikers | [ ] |
| Onveilige verwerking gevoelige data | Rollen, rechten, beveiligde API-laag | [ ] |
| Project te groot door extra wensen | Eerst Must have, daarna Should/Could | [x] |
