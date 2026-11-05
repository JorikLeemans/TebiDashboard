# Voorstelformulier — .NET MAUI app TEBI

Samenvatting en vertaling naar projectbeslissingen.  
**Bron:** [Jorik Leemans Voorstelformulier .NET MAUI app.docx](../Jorik%20Leemans%20Voorstelformulier%20.NET%20MAUI%20app.docx)

**Leeruitkomst:** LU Agile realisatie van een complex informatiesysteem binnen een .NET ontwikkelomgeving in een weinig gestructureerde en onvoorspelbare context.

---

## Vraag 1 — Gebruikers en doel

### Beoogde gebruikers

| Rol | Focus |
|-----|-------|
| **Directie** | Omzet, winst, doelen, trends |
| **Inkoop** | Verkoopcijfers, lage voorraad, bestsellers |
| **Voorraadbeheer** | Voorraadlocaties, openstaande transacties op terrein |
| **Verkoop** | Populaire artikelen, doelen, voortgang |
| **Financieel** | Uitgavenoverzicht |

### Core functionaliteit

Dashboard gefocust op **duidelijke weergave van NetSuite-data**:

- Omzet, winst, doelen, trends
- **Voorraad** en **lopende transacties** van artikelen op het terrein
- Live data via Web API (geen vervanging van NetSuite)

---

## Vraag 2 — User stories (~10)

De onderstaande stories komen rechtstreeks uit het voorstelformulier en zijn uitgewerkt in [user-stories.md](./user-stories.md).

| # | Story (kort) | Rol |
|---|--------------|-----|
| 1 | Inzicht in omzet | Directeur |
| 2 | Doelen per periode | Directeur |
| 3 | Veel verkocht + lage voorraad | Inkoop |
| 4 | Meest verkochte artikelen | Inkoop |
| 5 | Artikel op meerdere opslaglocaties | Voorraad |
| 6 | Artikel met veel openstaande transacties | Voorraad |
| 7 | Populaire artikelen | Verkoop |
| 8 | Doelen per artikel | Verkoop |
| 9 | Op schema lopen bij doel | Verkoop |
| 10 | Uitgavenoverzicht | Financieel |
| 11 | Barcode scannen → artikel opzoeken | Gebruiker (terrein) |

---

## Vraag 3 — Web API

### Rol van de API

- **Brug** tussen dashboard (Blazor/MAUI) en NetSuite
- NetSuite REST/SuiteTalk-poorten voor **live data**
- Realistische bedrijfsvraag → **echte integratie**, niet primair mocken
- Mock alleen voor **ontwikkeling/test** wanneer sandbox tijdelijk niet beschikbaar is

### Front-end

Blazor (Hybrid in MAUI + Web) met .NET back-end ondersteuning.

Zie [architectuur.md](./architectuur.md) en [netsuite-data-mapping.md](./netsuite-data-mapping.md).

---

## Vraag 4 — Mobile hardware & platformen

### Device feature: barcode / camera

Op het TEBI-terrein heeft elk artikel een **unieke barcode**. Huidige workflow gebruikt al scan-apparatuur; de MAUI-app moet dit ondersteunen:

| Feature | Platform | Implementatie |
|---------|----------|-------------------------|
| Barcode scannen | Android (primair) | MAUI `ZXing.Net.Maui`; de website gebruikt de telefooncamera |
| Barcode scannen | Windows | USB-scanner (keyboard wedge) en de camera via `IBarcodeScannerService` |
| Artikel opzoeken | Beide | Scan → API `GET /api/items/by-barcode/{code}` |

Na scan: toon **voorraad**, **verkoop/inkoop** en **openstaande transacties** voor dat artikel.

### Deploy-targets (min. 2)

| Platform | Reden |
|----------|-------|
| **Windows** | Alle kantoor-PC's bij TEBI |
| **Android** | Meeste werktelefoons op terrein |

---

## Impact op projectdocumentatie

| Document | Wijziging |
|----------|-----------|
| [user-stories.md](./user-stories.md) | 11 stories uit formulier + technische enablers |
| [ONTWERP.md](./ONTWERP.md) | Modules voorraad, transacties, barcode, winst/uitgaven |
| [netsuite-data-mapping.md](./netsuite-data-mapping.md) | Inventory, barcode, open transactions |
| [oo-and-patterns.md](./oo-and-patterns.md) | Barcode service + device abstraction |
| [lu3-assessment.md](./lu3-assessment.md) | Device feature motivatie (portfolio) |

---

## Relatie LU2 advies ↔ Voorstelformulier

| LU2 (adviesrapport) | Voorstelformulier (uitbreiding) |
|---------------------|----------------------------------|
| Omzet, afzet, trends, doelen | + winst, uitgaven, voorraad |
| Web-based dashboard | + MAUI terrein-app met barcode |
| Blazor + .NET API | Bevestigd; live NetSuite-data |
| Filters klant/leverancier | + artikel/barcode op terrein |
