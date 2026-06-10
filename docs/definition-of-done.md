# Definition of Done

Per user story geldt onderstaande Definition of Done (DoD). Een story is **Done** als **alle** items zijn afgevinkt.

---

## Algemene DoD (geldt voor elke story)

### Code & kwaliteit

- [ ] Code is geschreven in **Engels** (identifiers, comments waar nodig)
- [ ] Code volgt `.editorconfig` / `dotnet format` (consistente stijl)
- [ ] Target framework: **.NET 10** / **C# 14**
- [ ] Geen compiler warnings (of gemotiveerd en gedocumenteerd)
- [ ] OO-principes en patterns zijn toegepast waar relevant (zie [oo-and-patterns.md](./oo-and-patterns.md))

### Tests

- [ ] Bestaande unit tests slagen (`dotnet test`)
- [ ] Nieuwe/gewijzigde businesslogica heeft unit test(s)
- [ ] UI-wijzigingen zijn manueel getest op **Windows** én **Android**
- [ ] Geen regressie in eerder werkende flows

### Documentatie

- [ ] README of installation.md bijgewerkt indien installatiestappen wijzigen
- [ ] API-wijzigingen reflected in Swagger / ONTWERP.md indien van toepassing
- [ ] CHECKLIST.md bijgewerkt

### Review & integratie

- [ ] Code reviewed (zelf of peer/docent)
- [ ] Gemerged naar `main` via pull request (geen direct push op main)
- [ ] Build pipeline groen (indien CI actief)

---

## Story-specifieke DoD

### US-01 t/m US-08 (Must have)

Naast algemene DoD:

- [ ] Acceptatiecriteria uit [user-stories.md](./user-stories.md) zijn behaald
- [ ] Getest met mock-data (NetSuite niet vereist voor Done, tenzij story API-integration betreft)
- [ ] Responsive / leesbaar op phone (Android) en desktop (Windows)

### API / NetSuite stories

- [ ] Endpoint gedocumenteerd in Swagger
- [ ] Geen secrets in source control
- [ ] Foutafhandeling retourneert zinvolle HTTP-statuscodes

### UI stories

- [ ] Loading state zichtbaar tijdens API-call
- [ ] Foutmelding zichtbaar bij API-fout
- [ ] Lege staat (geen data) netjes afgehandeld

---

## Portfolio-minimum (LU-3)

Voor oplevering portfolio moet minimaal gelden:

- [ ] ≥ 3 zinnige unit tests, alle groen
- [ ] ≥ 3 zinnige UI/device running tests, uitvoerbaar en gedocumenteerd
- [ ] App demonstreerbaar op Windows + Android
- [ ] [installation.md](./installation.md) compleet
- [ ] [oo-and-patterns.md](./oo-and-patterns.md) compleet met motivatie
