# NetSuite data-mapping

Overzicht van welke NetSuite-gegevens nodig zijn voor het TEBI Dashboard en hoe deze worden gemapt naar applicatie-DTO's.

> **Status:** Concept — te valideren met TEBI / NetSuite-beheerder  
> **Actie:** Workshop plannen om veldnamen en saved searches te bevestigen

---

## Benodigde databronnen (Fase 1 inventarisatie)

| Domein | NetSuite-bron (indicatief) | Dashboard-gebruik |
|--------|---------------------------|-------------------|
| Omzet | Transaction (Sales Order / Invoice) | Omzetgrafiek, KPI |
| Afzet | Transaction Line / Item Fulfillment | Afzet per artikel |
| Klanten | Customer record | Filter dropdown |
| Leveranciers | Vendor record | Filter dropdown |
| Artikelen | Item record + Item Group | Filter + afzet-detail |
| Doelstellingen | Custom record / Budget | Doel vs. actueel |

---

## Mapping-tabel

### Omzet → `OmzetDto`

| DTO-veld | Type | NetSuite-veld (indicatief) | Opmerking |
|----------|------|---------------------------|-----------|
| `Id` | string | Internal ID | Unieke key |
| `Datum` | DateTime | `trandate` | Transactiedatum |
| `Bedrag` | decimal | `amount` / `total` | Inclusief/exclusief BTW afstemmen |
| `KlantId` | string | `entity` (Customer) | FK naar klant |
| `KlantNaam` | string | Customer `companyname` | Display |
| `ArtikelgroepId` | string? | Item `class` / custom field | Optioneel filter |

### Afzet → `AfzetDto`

| DTO-veld | Type | NetSuite-veld (indicatief) | Opmerking |
|----------|------|---------------------------|-----------|
| `Id` | string | Line internal ID | |
| `Datum` | DateTime | `trandate` | |
| `ArtikelId` | string | Item internal ID | |
| `ArtikelNaam` | string | Item `displayname` | |
| `Artikelgroep` | string | Item class / pricing group | |
| `Aantal` | decimal | `quantity` | Eenheid bevestigen |
| `Omzet` | decimal | Line `amount` | Voor top-N |

### Doel → `DoelDto`

| DTO-veld | Type | NetSuite-veld (indicatief) | Opmerking |
|----------|------|---------------------------|-----------|
| `Jaar` | int | Custom field | |
| `Periode` | string | Maand / kwartaal | |
| `Type` | string | Omzet / Afzet | |
| `DoelWaarde` | decimal | Budget / custom record | |
| `ActueleWaarde` | decimal | Berekend uit transacties | Niet uit NetSuite direct |
| `Percentage` | decimal | Berekend | Actueel / Doel × 100 |

### Filter-lijsten

| DTO | NetSuite-record | Weergaveveld |
|-----|----------------|--------------|
| `KlantFilterDto` | Customer | `companyname` |
| `LeverancierFilterDto` | Vendor | `companyname` |
| `ArtikelgroepFilterDto` | Classification / Item Group | `name` |

---

## Saved Searches (voorstel)

NetSuite-data wordt idealiter opgehaald via **Saved Searches** of **SuiteQL**:

### SS_TEBI_Omzet_Per_Periode

- Type: Transaction
- Filters: Type = Invoice, Date range
- Columns: Date, Customer, Amount, Class
- Gebruik: Omzet-dashboard

### SS_TEBI_Afzet_Per_Artikel

- Type: Transaction Line
- Filters: Item type, Date range
- Columns: Item, Quantity, Amount, Class
- Gebruik: Afzet-dashboard

### SS_TEBI_Doelen

- Type: Custom Record (naam TBD met TEBI)
- Columns: Year, Period, Target Value, Type
- Gebruik: Doelen-pagina

---

## Openstaande vragen voor TEBI

- [ ] Welke NetSuite-versie en API-toegang (REST vs SuiteTalk)?
- [ ] Is omzet inclusief of exclusief BTW?
- [ ] Waar staan doelstellingen opgeslagen (custom record, budget module)?
- [ ] Welke artikelgroep-definitie gebruikt TEBI (class, pricing group, custom)?
- [ ] Welke rollen mogen welke data zien (verkoper = eigen klanten)?
- [ ] Sandbox-credentials beschikbaar voor ontwikkeling?

---

## Mock-data (ontwikkeling)

Tot NetSuite-toegang beschikbaar is, gebruikt de Infrastructure-laag een `MockNetSuiteService` met voorbeelddata zodat front-end en API onafhankelijk ontwikkeld kunnen worden.

```
Infrastructure/
  Services/
    NetSuiteService.cs       → productie
    MockNetSuiteService.cs   → ontwikkeling
```

Switch via configuratie: `"NetSuite:UseMock": true`
