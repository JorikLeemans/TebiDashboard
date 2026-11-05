# Code Review TO-DO

Action items from the code review of the TEBI Dashboard backend/UI. Ordered by
severity. Check off items as they are addressed.

## Critical — Authentication & Authorization

- [x] **Hash passwords instead of storing plaintext.** Replace plaintext storage
  and comparison with a strong password hash (BCrypt, PBKDF2, or Argon2) and a
  constant-time verify. Migrate seeded/demo accounts accordingly.
  - `src/TebiDashboard.Infrastructure/Services/EfUserStore.cs`
    (`TryRegister`, `TryAuthenticate`, `TryUpdatePassword`)
  - `src/TebiDashboard.Infrastructure/Services/InMemoryUserStore.cs`
  - `src/TebiDashboard.Infrastructure/Data/DemoDataSeeder.cs` (seeded passwords)

- [x] **Issue and validate a real auth token.** The login token is currently a
  random GUID (`demo-token-...`) that nothing validates, and no authentication
  scheme is registered. Add real authentication (JWT bearer or ASP.NET Core
  Identity), sign a token carrying the user's role claim, and register
  `AddAuthentication`/`AddAuthorization` + `UseAuthentication`/`UseAuthorization`.
  - `src/TebiDashboard.Api/Controllers/AuthController.cs`
  - `src/TebiDashboard.Api/Program.cs`

- [x] **Stop trusting the client-supplied role header.** Authorization derives
  admin access from the `X-Tebi-Role` request header set by the client, so any
  caller can send `X-Tebi-Role: Admin` and gain admin access. Derive the role
  from validated server-side claims and remove the header-based check.
  - `src/TebiDashboard.Api/Authorization/AdminRequestContext.cs`
  - `src/TebiDashboard.UI/Services/AdminApiClient.cs`
  - Consumers: `AdminUsersController`, `NetSuiteController`, `AdminDemoDataController`

- [x] **Protect the data endpoints.** Dashboard/business endpoints have no
  authorization and are readable anonymously. Add `[Authorize]` (and role
  requirements where appropriate) once real auth is in place.
  - `DashboardController`, `RevenueController`, `CustomersController`,
    `SuppliersController`, `InventoryController`, `ExpensesController`,
    `ItemsController`, `GoalsController`

## Medium

- [x] **Tighten CORS.** `SetIsOriginAllowed(_ => true)` combined with
  `AllowCredentials()` reflects any origin with credentials. Replace with an
  explicit origin allowlist (per environment).
  - `src/TebiDashboard.Api/Program.cs`

- [x] **Gate the `demo`/`demo` backdoor.** The hardcoded demo login is always
  active, including in Production. Restrict it to Development
  (`IWebHostEnvironment.IsDevelopment()`) or a config flag.
  - `src/TebiDashboard.Api/Controllers/AuthController.cs`

## Admin UI

- [x] **Show NetSuite setup status on the admin page.** Load configured / not configured
  plus last sync when the page opens, and disable Test connection / Sync when credentials
  are missing so admins do not have to guess whether sync can work.
  - `GET /api/netsuite/setup`
  - `src/TebiDashboard.UI/Pages/AdminDashboard.razor`

## Low — Correctness & robustness

- [x] **Keep SuiteQL queries parameterized.** Date values are interpolated into
  SuiteQL strings. It is safe today because the input is a strongly-typed
  `DateOnly`, but add a guard/comment so any future user-supplied filter is
  parameterized rather than string-formatted (SuiteQL injection risk).
  - `src/TebiDashboard.Infrastructure/NetSuite/NetSuiteSuiteQlQueries.cs`

- [x] **Guard against null passwords.** `TryRegister` and `TryUpdatePassword`
  call `password.Length` before any null check, so a null `Password` in the
  request body throws a 500 instead of returning a clean 400. Add an
  `IsNullOrEmpty` guard.
  - `src/TebiDashboard.Infrastructure/Services/EfUserStore.cs`

## Product / UX

- [ ] **Do not make whole rows clickable.** Only the part that actually goes
  somewhere should be a link. If a row leads to a customer, that customer name
  (or similar) is clickable — not the entire row.
  - Open-transaction rows on item detail currently navigate on the whole row.

- [x] **Get clear what recent sales actually means** in this dashboard (period,
  quantity vs amount, and which NetSuite source). Users should not have to guess
  the KPI.
  - Inventory and bestsellers label this **Sold in period**: invoice amount for
    the selected period, and units from invoice quantities. Zero when that item
    has no invoices in the period.
  - Low-stock still uses the quantity stored by the last sync, not the period filter.
  - Item detail lists the invoice lines that add up to Sold in period.

- [x] **Take another look at customer names.** Combine `entityId` (K######) with
  `glommedName` when `isPerson = T` and with `companyName` when `isPerson = F`.
  - `CustomerDisplayName` + SuiteQL `isPerson` / `companyName` on customer sync.

- [ ] **Think about extra "quick" filters** so users can find what they want
  faster, or exclude certain results (status, type, location, low stock, party).

- [ ] **Think about what each role can and cannot do.** Spell out permissions
  per role (view vs manage, admin vs management vs other) and align the UI and
  API with that.

- [ ] **Extra ways of showing data** (charts, grouping, exports, alternate
  layouts). Not a high priority.

- [x] **Check what "down" means on the Revenue board** on the dashboard home
  page. The card shows the percent change versus the previous window
  (`PeriodTrendWindows.DescribeChange`), for example `-12%` or `+8%`.

- [x] **Check vendor names on the dashboard.** A bill without a supplier master
  row showed the internal id twice, and the supplier page said it could not be
  found. The id is shown once. The company name comes from the supplier row, or
  from the bill label after sync. The link opens the stored bills when the
  master row is still missing. Inactive vendors are included in the vendor sync.

- [ ] **When going live, filter test cases out of the results** so the
  dashboard only shows real, useful data (no demo/test customers, vendors, or
  transactions).
