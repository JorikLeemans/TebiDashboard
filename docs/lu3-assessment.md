# LU-3 Assessment — Portfolio-eisen

Mapping van de **Individueel portfolio .NET MAUI**-eisen naar dit project (TEBI Dashboard).

> Bron: opdrachtgever / docent — LU-3 assessment informatie

---

## Niet-functionele eisen

| Eis | Toepassing in TEBI Dashboard | Document / artefact |
|-----|------------------------------|---------------------|
| C# 14 en .NET 10 | Solution target `net10.0`; MAUI volgens .NET 10-richtlijnen | [ONTWERP.md](./ONTWERP.md) §4, `global.json` |
| Actuele .NET MAUI-richtlijnen | Blazor Hybrid, platform targets Windows + Android | [architectuur.md](./architectuur.md) |
| Broncode in het **Engels** | Classes, methods, variables, comments in code = English | `.editorconfig`, code review |
| Consistente codestijl | `.editorconfig` + `dotnet format` | Repository root |
| Demonstratie op **2 platformen** | **Windows + Android** (kantoor + terrein) | [installation.md](./installation.md) |
| **Device feature (barcode)** | Camera/USB scan → artikel lookup (US-11) | [proposal-form.md](./proposal-form.md) |
| Min. **3 zinnige unit tests** | Application + API services | `tests/` |
| Min. **3 zinnige UI/device tests** | MAUI / Blazor UI flows | `tests/` |
| Gemotiveerd **OO-principes & design patterns** | Clean Architecture, Repository, DI, MVVM-achtige scheiding | [oo-and-patterns.md](./oo-and-patterns.md) |
| Gemotiveerde **architectuurstijl** | Clean Architecture + layered API | [ONTWERP.md](./ONTWERP.md) ADR's |
| **Installatie- en gebruikersbeschrijving** | Stap-voor-stap handleiding | [installation.md](./installation.md) |

---

## Functionele eisen

| Eis | Toepassing |
|-----|------------|
| User stories doorlopen | Zie [user-stories.md](./user-stories.md) |
| Definition of Done | Zie [definition-of-done.md](./definition-of-done.md) |

---

## Platformkeuze (demonstratie)

**Gekozen combinatie: Windows + Android**

| Platform | Reden |
|----------|-------|
| Windows | Primaire ontwikkel- en demo-omgeving; TEBI medewerkers op PC/laptop |
| Android | Mobiel/tablet demo; geen Mac vereist voor iOS-build |

iOS kan later worden toegevoegd indien Mac-build-omgeving beschikbaar is.

---

## Teststrategie (minimaal portfolio-eisen)

### Unit tests (min. 3)

| # | Test | Project | Wat wordt bewezen |
|---|------|---------|-------------------|
| 1 | `GetSummaryAsync_ReturnsData_FromMockNetSuite` | Application.Tests | KPI-aggregatie via `INetSuiteService` |
| 2 | `GetDecimal_ParsesNumberAndStringValues` | Infrastructure.Tests | SuiteQL-rij naar een decimaal |
| 3 | `ComputePercentage_ReturnsCorrectValue_WhenTargetIsPositive` | Application.Tests | Doel-percentage |

> De suite is groter dan dit minimum. GitHub Actions draait application, API, infrastructure en UI.

### UI / device running tests (min. 3)

| # | Test | Framework | Wat wordt bewezen |
|---|------|-----------|-------------------|
| 1 | `DashboardHome_ShowsKpiCards_WhenApiReturnsData` | bUnit | Hoofdscherm toont KPI-kaarten |
| 2 | `DashboardHome_ShowsPeriodTrend_AgainstEarlierWindows` | bUnit | Trend over eerdere vensters |
| 3 | `InventoryPage_ShowsItems_WhenApiReturnsData` | bUnit | Voorraadlijst na een API-antwoord |

De device-demo op Windows en Android staat nog open (US-13).

---

## Portfolio-leverables checklist

- [ ] Werkende .NET 10 MAUI-app (Windows + Android)
- [x] Web API als back-end (portfolio + TEBI-advies)
- [x] Min. 3 unit tests groen in CI — workflow `.github/workflows/ci.yml`
- [x] Min. 3 UI/device tests gedocumenteerd en uitvoerbaar — bUnit in `tests/TebiDashboard.UI.Tests`
- [x] [oo-and-patterns.md](./oo-and-patterns.md) ingevuld
- [x] [installation.md](./installation.md) ingevuld
- [ ] [user-stories.md](./user-stories.md) alle Must-have stories Done — US-13 demo nog open
- [ ] [definition-of-done.md](./definition-of-done.md) per story afgevinkt
- [ ] Demonstratie-video of live demo op 2 platformen

---

## Relatie TEBI-advies ↔ LU-3 portfolio

| TEBI (LU2 advies) | LU-3 portfolio |
|-------------------|----------------|
| Blazor + .NET Web API | ✅ Zelfde stack, uitgebreid met MAUI |
| NetSuite dashboard | ✅ User stories + functionele scope |
| Web-based | ✅ Blazor Web naast MAUI |
| MoSCoW Must have | US-01 t/m US-12 en US-14 Done. US-13 (device-demo) nog open |
| MoSCoW Should/Could | US-15 en US-17 Done. US-16 auto-refresh en US-18 donkere modus nog open |
