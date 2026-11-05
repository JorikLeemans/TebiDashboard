# OO Principles & Design Patterns

Motivation document for the LU-3 portfolio assessment.

> **Requirement:** Demonstrate motivated use of OO principles and design patterns.  
> **Language:** Code in English; this document may be Dutch for portfolio readers.

---

## Architecture style: Clean Architecture

**Choice:** Layered Clean Architecture (Api → Application → Infrastructure).

**Why:**
- **Separation of concerns:** UI (MAUI/Blazor) does not depend on NetSuite or HTTP details.
- **Testability:** Application services can be unit-tested with mocked `INetSuiteService`.
- **TEBI advice alignment:** Modular, maintainable, extensible (MoSCoW Should/Could later).
- **Portfolio:** Clear motivation for architectural style (LU-3 requirement).

```
┌─────────────────────────────────────┐
│  Presentation (MAUI, Blazor Web)    │
├─────────────────────────────────────┤
│  TebiDashboard.UI (components)     │
├─────────────────────────────────────┤
│  TebiDashboard.Api (HTTP)          │
├─────────────────────────────────────┤
│  TebiDashboard.Application         │  ← business rules, interfaces
├─────────────────────────────────────┤
│  TebiDashboard.Infrastructure      │  ← NetSuite, cache, persistence
└─────────────────────────────────────┘
```

---

## OO principles applied

### Single Responsibility Principle (SRP)

Each class has one reason to change.

| Class | Single responsibility |
|-------|----------------------|
| `DashboardService` | Aggregate dashboard KPIs |
| `DatabaseNetSuiteService` | Read dashboard figures from the local database |
| `NetSuiteRestClient` | HTTP calls to NetSuite during sync |
| `PeriodExcelWriter` | Write the period Excel workbook |
| `DashboardHome.razor` | Show the dashboard home screen |

### Open/Closed Principle (OCP)

Extend behaviour without modifying existing code.

**Example:** `INetSuiteService` with implementations:
- `DatabaseNetSuiteService` (PostgreSQL or SQLite)
- `MockNetSuiteService` (in-memory tests and local demo)

New data sources stay a new implementation. `DashboardService` does not change.

### Liskov Substitution Principle (LSP)

Any `INetSuiteService` implementation can replace another without breaking callers.

**Example:** Automated tests run with `Database:Provider=InMemory`, which registers `MockNetSuiteService`. A normal run registers `DatabaseNetSuiteService`.

### Interface Segregation Principle (ISP)

Small, focused interfaces instead of one large "god interface".

```csharp
public interface IDashboardService { /* period overview */ }
public interface IPeriodReportService { /* Excel and PDF source data */ }
public interface INetSuiteSyncService { /* import from NetSuite */ }
public interface IGoalStore { /* saved targets */ }
```

`INetSuiteService` stays the data facade. Sync, goals, users, and the period report are separate interfaces.

### Dependency Inversion Principle (DIP)

High-level modules depend on abstractions, not concretions.

**Example:** `DashboardService` depends on `INetSuiteService`, not on `DatabaseNetSuiteService`.  
Registered in `TebiDashboard.Infrastructure/DependencyInjection.cs`:

```csharp
services.AddScoped<INetSuiteService, MockNetSuiteService>();      // InMemory
services.AddScoped<INetSuiteService, DatabaseNetSuiteService>();  // PostgreSQL or SQLite
```

---

## Design patterns used

### 1. Dependency Injection (DI)

**Where:** ASP.NET Core `Program.cs`, MAUI `MauiProgram.cs`  
**Why:** Loose coupling, testability, configuration-based swapping of implementations.

### 2. Repository / Gateway pattern

**Where:** `INetSuiteService` + Infrastructure implementation  
**Why:** Hides NetSuite REST/OAuth complexity from Application layer.  
**TEBI context:** Single secure gateway to ERP data (ADR-003).

### 3. DTO (Data Transfer Object)

**Where:** `TebiDashboard.Shared` — `RevenueDto`, `GoalDto`, etc.  
**Why:** Stable contract between API, UI, and external system; no NetSuite types leak upward.

### 4. Mapper pattern

**Where:** `NetSuiteSuiteQlRowReader` maps SuiteQL rows onto dashboard records  
**Why:** SRP — row mapping stays out of the HTTP client and the dashboard services.

### 5. Options pattern

**Where:** `NetSuiteOptions` and `DatabaseOptions` bound from `appsettings.json`  
**Why:** Type-safe configuration; secrets via User Secrets / environment.

### 6. MVVM-like separation (Blazor)

**Where:** `.razor` pages + injected services (no code-behind business logic)  
**Why:** UI binds to service calls; logic stays in Application layer testable without UI.

---

### 7. Strategy / device abstraction pattern

**Where:** `IBarcodeScannerService` in the shared UI, `BarcodeScannerService` in the MAUI project  
**Why:** The scan page does not know whether the camera is Android or Windows. The MAUI service asks for the camera permission and returns the code. The website leaves the camera empty and uses the browser scanner.

```csharp
public interface IBarcodeScannerService
{
    Task<string?> ScanAsync(CancellationToken cancellationToken = default);
}
```

UI (`BarcodeScanner.razor`) calls the service, then the API looks up the item.

---

## What we deliberately avoid

| Pattern | Reason |
|---------|--------|
| Singleton for stateful services | Use DI scoped/singleton correctly via framework |
| God classes | Split by domain (revenue, goals, filters) |
| Direct NetSuite calls from MAUI | Security + DIP violation |

---

## Portfolio demonstration tips

During assessment, be prepared to explain:
1. Why Clean Architecture fits a NetSuite dashboard add-on.
2. One example of SRP (e.g. mapper vs. service).
3. How `MockNetSuiteService` enables tests without NetSuite sandbox.
4. How DI allows switching mock/production in one registration line.

See also [ONTWERP.md](./ONTWERP.md) ADR section and [lu3-assessment.md](./lu3-assessment.md).
