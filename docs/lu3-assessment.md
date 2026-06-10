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
| Demonstratie op **2 platformen** | **Windows + Android** (primair); iOS optioneel | [installation.md](./installation.md) |
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
| 1 | `DashboardService_GetSummary_ReturnsCorrectTotals` | Application.Tests | Businesslogica aggregatie |
| 2 | `NetSuiteMapper_MapsSalesRecord_ToOmzetDto` | Application.Tests | Mapping NetSuite → DTO |
| 3 | `GoalCalculator_ComputesPercentageCorrectly` | Application.Tests | Doel-percentage berekening |

> Uitbreiden volgens Definition of Done.

### UI / device running tests (min. 3)

| # | Test | Framework | Wat wordt bewezen |
|---|------|-----------|-------------------|
| 1 | Dashboard home laadt KPI-kaarten | MAUI UITest / Appium | Hoofdscherm zichtbaar |
| 2 | Filter wijzigen ververst omzetgrafiek | MAUI UITest | Interactie + data-binding |
| 3 | Navigatie naar Afzet-pagina toont tabel | MAUI UITest | Routing / navigatie |

Alternatief: bUnit voor Blazor-componenttests + 1 device test op Android emulator.

---

## Portfolio-leverables checklist

- [ ] Werkende .NET 10 MAUI-app (Windows + Android)
- [ ] Web API als back-end (portfolio + TEBI-advies)
- [ ] Min. 3 unit tests groen in CI
- [ ] Min. 3 UI/device tests gedocumenteerd en uitvoerbaar
- [ ] [oo-and-patterns.md](./oo-and-patterns.md) ingevuld
- [ ] [installation.md](./installation.md) ingevuld
- [ ] [user-stories.md](./user-stories.md) alle Must-have stories Done
- [ ] [definition-of-done.md](./definition-of-done.md) per story afgevinkt
- [ ] Demonstratie-video of live demo op 2 platformen

---

## Relatie TEBI-advies ↔ LU-3 portfolio

| TEBI (LU2 advies) | LU-3 portfolio |
|-------------------|----------------|
| Blazor + .NET Web API | ✅ Zelfde stack, uitgebreid met MAUI |
| NetSuite dashboard | ✅ User stories + functionele scope |
| Web-based | ✅ Blazor Web naast MAUI |
| MoSCoW Must have | ✅ User stories US-01 t/m US-08 |
| MoSCoW Should/Could | Backlog na portfolio-MVP |
