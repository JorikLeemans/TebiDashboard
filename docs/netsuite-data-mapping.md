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
| **Barcode** | Item `upccode` / custom barcode field | US-11 scan lookup |
| **Voorraad** | Inventory Balance / Location | Locaties, lage voorraad |
| **Open transacties** | Sales Order / Transfer Order (open) | US-06 terrein |
| **Uitgaven** | Vendor Bill / Expense | US-10 financieel |
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

### Voorraad → `InventoryItemDto` / `StockLocationDto`

| DTO-veld | Type | NetSuite-veld (indicatief) | Opmerking |
|----------|------|---------------------------|-----------|
| `ItemId` | string | Item internal ID | |
| `ItemName` | string | `displayname` | |
| `Barcode` | string | `upccode` / custom | US-11 lookup key |
| `QuantityOnHand` | decimal | Inventory balance | Per locatie of totaal |
| `LocationId` | string | Location internal ID | US-05 meerdere locaties |
| `LocationName` | string | Location `name` | Terrein/lmagazijn |
| `ReorderPoint` | decimal? | Item reorder point | Lage voorraad-drempel |

### Open transactie → `OpenTransactionDto`

| DTO-veld | Type | NetSuite-veld (indicatief) | Opmerking |
|----------|------|---------------------------|-----------|
| `TransactionId` | string | Internal ID | |
| `Type` | string | Sales Order / Transfer / etc. | |
| `ItemId` | string | Line item | |
| `QuantityOpen` | decimal | Open quantity | US-06 |
| `Status` | string | Order status | |

### Uitgaven → `ExpenseDto`

| DTO-veld | Type | NetSuite-veld (indicatief) | Opmerking |
|----------|------|---------------------------|-----------|
| `Id` | string | Internal ID | |
| `Date` | DateTime | `trandate` | |
| `Category` | string | Expense category / account | |
| `Amount` | decimal | `amount` | US-10 |
| `VendorName` | string? | Vendor | Optioneel |

### Barcode lookup → `ItemDetailDto`

Combineert item + inventory locations + open transaction count (US-11 scan result).

| DTO-veld | Bron |
|----------|------|
| `Item` | Item record via barcode |
| `Locations` | `StockLocationDto[]` |
| `OpenTransactionCount` | Aggregate open transactions |
| `RecentSales` | Optional summary |

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

### SS_TEBI_Voorraad_Locaties

- Type: Inventory Balance
- Columns: Item, Location, Quantity On Hand, Reorder Point
- Gebruik: US-03, US-05

### SS_TEBI_Open_Transacties

- Type: Transaction (open Sales/Transfer Orders)
- Filters: Status = open/pending
- Columns: Item, Quantity, Type, Date
- Gebruik: US-06

### SS_TEBI_Uitgaven

- Type: Vendor Bill / Expense Report
- Columns: Date, Category, Amount, Vendor
- Gebruik: US-10

---

## Openstaande vragen voor TEBI

- [ ] Welke NetSuite-versie en API-toegang (REST vs SuiteTalk)?
- [ ] Is omzet inclusief of exclusief BTW?
- [ ] Waar staan doelstellingen opgeslagen (custom record, budget module)?
- [ ] Welke artikelgroep-definitie gebruikt TEBI (class, pricing group, custom)?
- [ ] Welke rollen mogen welke data zien (verkoper = eigen klanten)?
- [ ] Sandbox-credentials beschikbaar voor ontwikkeling?
- [ ] Welk veld bevat de **barcode** per artikel (UPC vs custom)?
- [ ] Hoe worden **terreinlocaties** in NetSuite bijgehouden?
- [ ] Welke transactietypes tellen als "openstaand" op het terrein?

---

## Mock-data (ontwikkeling)

Tot NetSuite-toegang beschikbaar is, gebruikt de Infrastructure-laag een `MockNetSuiteService` met voorbeelddata. **Productie gebruikt live NetSuite** (voorstelformulier).

```
Infrastructure/
  Services/
    NetSuiteService.cs       → productie
    MockNetSuiteService.cs   → ontwikkeling
```

Switch via configuratie: `"NetSuite:UseMock": true`
