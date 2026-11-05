# NetSuite data-mapping

Welke SuiteQL-gegevens de sync ophaalt, en in welke dashboard-DTO ze terechtkomen.

> **Status:** november 2026. De queries staan in `NetSuiteSuiteQlQueries`. Een workshop met TEBI over btw, doelen en rollen per verkoper is nog open.
> Veldnamen volgen de Records Browser en de SuiteQL-query's in de code.

De sync schrijft PostgreSQL. De pagina's lezen die tabellen via `DatabaseNetSuiteService`. Tests met `Database:Provider=InMemory` gebruiken `MockNetSuiteService`.

---

## Bronnen

| Domein | SuiteQL | Dashboard |
|--------|---------|-----------|
| Klanten | `customer` + default billing address book | `/customers` |
| Leveranciers | `vendor`, inclusief inactieve vendors | `/suppliers` |
| Artikelen | `item` (`InvtPart`, `Assembly`), `upcCode` | Voorraad en barcode |
| Voorraad | `inventorybalance`, `itemlocationconfiguration` | Locaties en reorder point |
| Omzet | `invoice` + `transactionline` | Omzet, bestsellers, sold in period |
| Uitgaven | `transaction` type `VendBill` | Uitgaven |
| Open orders | `salesorder` en `purchaseorder` + `transactionline` | Open transacties |
| Doelen | Niet uit NetSuite | Opgeslagen in de dashboard-database |

---

## Omzet → `RevenueDto`

Eén factuurregel. `tl.amount` is negatief op een normale verkoopregel, dus de query draait het teken om. `transactionline.quantity` is negatief als de voorraad daalt. De query vermenigvuldigt met `-1`, zoals in het Oracle-voorbeeld `quantity * -1`.

| DTO | SuiteQL |
|-----|---------|
| `InvoiceId` | `invoice.id` |
| `InvoiceNumber` | `invoice.tranId` |
| `Id` | Factuurregel `transactionline.id` |
| `Date` | `invoice.tranDate` |
| `CustomerId` | `invoice.entity` |
| `CustomerName` | Naam uit de customer-sync, niet uit de factuurquery |
| `ItemId` / `ItemName` | `transactionline.item` en `BUILTIN.DF(item)` |
| `ProductGroupId` / `ProductGroupName` | `item.class` en `BUILTIN.DF(class)` |
| `Amount` | `-NVL(transactionline.amount, 0)` |

## Afzet en sold in period → `InventoryItemDto`

| DTO | Bron |
|-----|------|
| `ItemId` | `item.id` |
| `ItemName` | `item.itemId` (SKU), anders `displayName` |
| `Barcode` | `item.upcCode` |
| `QuantityOnHand` | Som van `inventorybalance.quantityonhand` |
| `RecentSalesVolume` | Op de voorraadlijst: factuurbedrag in de gekozen periode. Op low-stock: de hoeveelheid uit de laatste sync |
| `UnitsSold` | Som van de positieve factuurhoeveelheid in de periode. 0 als er geen factuurregels zijn |
| `IsLowStock` | On-hand op of onder het reorder point |

Locaties staan op `StockLocationDto`: `inventorybalance` voor de hoeveelheid, `itemlocationconfiguration.reorderpoint` en `preferredstocklevel` voor de drempels. `BUILTIN.DF(location)` is de locatienaam.

## Doel → `GoalDto`

Doelen komen uit de dashboard-database (`IGoalStore`), niet uit een NetSuite-budget. `ActualValue` en `Percentage` rekent `DashboardService` uit de omzet in die periode. Admin en Management schrijven targets via `PUT /api/goals`.

| DTO | Betekenis |
|-----|-----------|
| `Year`, `Period` | Kalenderjaar en periode |
| `Type` | `Revenue` of een artikeldoel |
| `ItemId`, `ItemName` | Leeg bij een bedrijfsdoel |
| `TargetValue` | Ingevoerde doelwaarde |
| `ActualValue`, `Percentage`, `Status` | Berekend (`on-track`, `ahead`, `off-track`) |

## Klant en leverancier

| DTO | SuiteQL |
|-----|---------|
| `CustomerDto.Name` | `entityId` plus `glommedName` als `isPerson = T`, anders plus `companyName` |
| `CustomerDto.City` | Stad van het default billing address book |
| `CustomerDto.Category` | `BUILTIN.DF(customer.category)` |
| `SupplierDto.Name` | `entityId` plus `companyName` |
| `SupplierDto.City` | Stad van het vendor billing address book |

Inactieve vendors blijven in de query. Een vendor bill houdt `transaction.entity` ook als de vendor inactief is. `BUILTIN.DF(transaction.entity)` vult het label op de bill als de vendorrij ontbreekt.

## Open transactie → `OpenTransactionDto`

| DTO | SuiteQL |
|-----|---------|
| `TransactionId` | `transactionline.transaction` |
| `Type` | `Sales Order` of `Purchase Order` |
| `ItemId`, `ItemName` | Regel-item en `BUILTIN.DF` |
| `CustomerId` of `SupplierId` | `salesorder.entity` of `purchaseorder.entity` |
| `QuantityOpen` | `quantity - quantityshiprecv`, alleen als er nog iets open staat |
| `Amount` | `transactionline.amount` |
| `Status` | Orderstatus. Gesloten en geannuleerde sales orders vallen uit de query |

## Uitgaven → `ExpenseDto`

| DTO | SuiteQL |
|-----|---------|
| `Id` | `transaction.id` |
| `Date` | `trandate` |
| `Amount` | `NVL(foreigntotal, 0)`. `transaction.total` is niet bruikbaar in SuiteQL |
| `SupplierId` | `transaction.entity` |
| `VendorName` | Leveranciersnaam uit de sync, anders `BUILTIN.DF(entity)` |
| `SupplierName` | Naam uit de supplier-tabel. Leeg als die rij ontbreekt. Het interne id wordt hier niet in gekopieerd |
| `Category` | Op een gesynchroniseerde bill: `Vendor Bill` |

## Barcode → `ItemDetailDto`

`GET /api/items/by-barcode/{barcode}` zoekt `upcCode`. Het antwoord bevat het artikel, de locaties, de open transacties en de factuurregels van de gevraagde periode (`PeriodInvoices`, `PeriodSalesAmount`).

---

## Nog open met TEBI

- [ ] Is het factuurbedrag inclusief of exclusief btw voor de KPI?
- [ ] Blijven doelen in het dashboard, of moeten ze uit NetSuite komen?
- [ ] Ziet een verkoper alleen de eigen klanten?
- [ ] Welke orderstatussen op het terrein tellen als openstaand, naast de huidige sales- en purchase orders?

Barcode, locaties, inactieve vendors en het teken van factuurhoeveelheden zijn in de sync verwerkt.
