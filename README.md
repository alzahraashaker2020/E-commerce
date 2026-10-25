# MakaniShop — E-Commerce Demo Platform

A full-stack e-commerce demonstration platform: an **Angular 18** storefront + admin
console, an **ASP.NET Core 7 Web API** backend, and **EF Core 7** over **PostgreSQL**
(with a zero-install **SQLite** demo mode). Built to showcase a purpose-built
alternative to a hosted store, with a clean N-layer architecture.

> **Demonstration data.** All catalog, order, customer, and review data is seeded
> demo data (`IsDemoData = true`) and clearly labeled in the UI. No real
> transactions occur.

---

## Architecture

The backend follows an N-layer solution (`Shop.sln`) with four projects:

| Project | Responsibility |
|---|---|
| `Shop.DAL` | Data access: EF Core entities (`Models/`), DTOs/view models (`ViewModels/`), `ShopContext` + deterministic seed (`Data/`), `Migrations/` |
| `Shop.BLL` | Business logic: repositories + unit-of-work (`IRepoWrapper`/`RepoWrapper`), password hashing, email service, `ApiResponse<T>` envelope |
| `Shop.SL` | Services layer: JWT token issuing (`TokenManager/`) |
| `Shop.Api` | Web API host: controllers, AutoMapper profile, middleware, DI composition (`Program.cs`) |

**Patterns:** Repository + Unit-of-Work, DTO/ViewModel separation, AutoMapper,
JWT bearer auth (separate **Admin** and **Customer** roles), a standard
`ApiResponse<T>` envelope.

The frontend (`client/`) is Angular 18 standalone components with a `core/`
(models, services, interceptors, guards) + `features/` structure and lazy routes.

---

## Prerequisites

- **.NET SDK 7.x** (`dotnet --version` → 7.0.x)
- **Node.js** (18+; developed against v24) and **npm**
- PostgreSQL is **optional** — the demo runs on SQLite by default.

---

## Running the demo (SQLite — zero database install)

By default the API uses SQLite and creates + seeds a local `shop_demo.db` on
startup, so no database server is needed.

**1. Backend API**

```bash
cd src/Shop.Api
dotnet run
```

The API starts on `http://localhost:5000`. On first run it builds and seeds
`shop_demo.db` automatically (via `EnsureCreated()`). To reseed from scratch,
stop the API, delete `src/Shop.Api/shop_demo.db` (and the `-shm`/`-wal` files),
and run again.

**2. Frontend**

```bash
cd client
npm install
npm start        # ng serve, dev server on http://localhost:4200
```

Open `http://localhost:4200`. The storefront talks to the API at
`http://localhost:5000/api` (configured in `client/src/environments/environment.ts`;
CORS for `http://localhost:4200` is enabled in the API).

> API docs: with the API running in Development, Swagger UI is at
> `http://localhost:5000/swagger`.

---

## Demo credentials

| Role | Login | Password |
|---|---|---|
| **Admin** | `admin` | `Admin@12345` |
| **Customer** | `jordan@demo.local` | `Admin@12345` |

Admin signs in at `/admin/login`; customers at `/login` (or register a new account).

---

## Switching to PostgreSQL (production target)

PostgreSQL is the intended production database. To use it:

1. Set the provider in `src/Shop.Api/appsettings.json`:
   ```json
   "Database": { "Provider": "Postgres" }
   ```
2. Set the connection string under `ConnectionStrings:DefaultConnection`
   (default placeholder: `Host=localhost;Port=5432;Database=online_shopping;Username=postgres;Password=postgres`).
3. Apply the EF migration (repo-local `dotnet-ef` tool, pinned to 7.0.20):
   ```bash
   dotnet tool restore
   dotnet dotnet-ef database update -p src/Shop.DAL -s src/Shop.Api
   ```

After changing entities, regenerate the migration:

```bash
dotnet dotnet-ef migrations add <Name> -p src/Shop.DAL -s src/Shop.Api
```

(The SQLite demo path does not require migrations — it uses `EnsureCreated()`.)

---

## Feature notes & demo-mode behavior

Some features are wired as **demo-safe** implementations so the platform runs
without external accounts. Each has a documented path to production.

### Email (order confirmation & status updates)
- An `IEmailService` sends an order-summary email on checkout and on order-status
  changes.
- **Default is demo mode**: the email (to / subject / body) is **logged** (console
  and a file under the API content root) instead of being sent. Email failures
  never block checkout.
- To send real email, set `Email:Enabled = true` and provide SMTP settings
  (`SmtpHost/Port/User/Password/From`) in `appsettings.json`.

### Payment
- Checkout records a **payment method** (`Cash` or `Card`) on the order and
  simulates the payment status (Card → paid; Cash → cash-on-delivery/pending).
- **No real card data is collected or stored.** A real gateway (e.g. Stripe) is
  the production step; this demo only records the chosen method.

### Stock at order confirmation
- Pending checkout validates available stock but does not reserve it. Stock is
  deducted atomically when the order first enters Confirmed (or fulfillment if
  confirmation is skipped). A competing order can fail confirmation with HTTP 409.
- A persisted marker prevents repeated deduction on later status changes or
  server restart. Cancelling before shipping restores stock once; shipped or
  delivered orders require a return workflow, which is still pending.
- Restart the API after updating. Existing SQLite databases receive an additive
  StockDeducted column without reseeding/deleting data. Historical orders are not
  automatically debited. PostgreSQL still requires the broader schema migration
  work documented in the backlog, in addition to the new marker migration.
- To run the image/stock regression checks after building, use:
  `pwsh -File tests/security-smoke.ps1 -AdditionalChecks tests/product-order-checks.ps1`.
  These checks create their own temporary database and upload directory.

### Product and variant photo galleries
- Admins can select multiple files, up to 20 photos for the product and 20 for
  each size/color variant. The first photo is primary; use Make primary or Remove
  before saving. Save is unavailable while files are uploading.
- The storefront gallery switches to the selected variant's photos, falling back
  to product photos. Existing single-photo variants remain compatible.
- Restart the API to add the variant gallery column to existing SQLite databases
  without deleting data, then refresh the frontend.

### Currency
- A single **store-level currency** (code + symbol) is chosen by the admin in the
  Settings page (default `USD` / `$`) and applied to all displayed prices; each
  order stores its currency code.
- This is a display/label currency, **not** live foreign-exchange conversion.

### Admin-managed configuration
- Manage colors and sizes from the dashboard shortcuts or sidebar.
- `/admin/currencies` manages currency names, codes, symbols and availability,
  and selects the store currency. Changing it does not convert catalog prices;
  existing orders retain their recorded currency. Sales totals cover the selected
  store currency only, rather than combining currencies.
- `/admin/order-statuses` manages status names, badge colors, order and availability.
  New statuses map to protected fulfillment behaviors so stock still deducts or
  restores once. In-use statuses cannot be deleted or have their behavior changed.
- Status names flow through order management, tracking and email. A configurable
  transition graph and status history remain separate backlog items.
- See [admin configuration roadmap](docs/16-admin-managed-configuration.md).

### Delivery locations and shipping
- Open `/admin/delivery`, add governorates, then their areas, shipping charges,
  currency and availability. No delivery rates are automatically invented or seeded.
- New checkout requires an available area with a rate in the current store
  currency. A configured zero rate explicitly means free delivery. The server
  calculates products minus discounts plus shipping, ignoring client price inputs.
- Checkout displays a server quote. A changed final price or currency requires
  the customer to review the updated quote before placing the order again.
- Orders retain the shipping charge and address names recorded at checkout.
  Editing/deactivating an area does not recalculate old orders. Used locations
  cannot be deleted; disable them instead.
- Shipping appears in order confirmation, admin orders, tracking and account
  history. Sales overview separates shipping collected from product sales.
- Existing SQLite databases receive additive delivery tables and a nullable
  order-area reference on API restart; old orders are not repriced. The separate
  PostgreSQL migration path still needs its previously documented schema work.

### Social login
- Google / Facebook sign-in is currently unavailable. The API returns 503 and
  never accepts a fabricated identity string or links accounts by unverified email.
- Email/password registration and login remain available. Social sign-in must
  verify provider-issued credentials server-side before it can be enabled.

### Admin access and security checks
- All `api/admin/*` controllers require the `Admin` role. Customer tokens are
  rejected with 403; unauthenticated requests are rejected with 401.
- After building the backend, run `pwsh -File tests/security-smoke.ps1` to check
  admin access, local authentication, and rejection of fabricated social credentials.
  The script starts its own API on loopback with a fresh temporary SQLite database,
  keeps test logs in that temporary directory, and stops only its own API process.
- The consolidated implementation plan is in [docs/15-product-backlog.md](docs/15-product-backlog.md).

---

## What's included

**Customer storefront:** catalog with category filtering and preserved-position
pagination, category → subcategory drill-down, product detail with attributes
(colour/size/width/height) and reviews, cart, guest checkout (with shipping
address + payment method), order confirmation, order tracking (by order number
**or** email), customer accounts (register / login / social / profile / order
history / reviews).

**Admin console:** sales overview dashboard, product management (incl. attributes
and stock quantity), inventory low-stock alerts, order management (full status
workflow), discount management, category/subcategory management, customer list,
and store settings (currency).

---

## Related documents

Business and strategy documents live in [`docs/`](./docs):

- `11-competitive-gap-analysis.md`
- `12-website-migration-strategy.md`
- `13-commercial-value-proposition.md`

---

## Project layout

```
online_shopping/
├─ Shop.sln
├─ src/
│  ├─ Shop.DAL/        # entities, view models, DbContext + seed, migrations
│  ├─ Shop.BLL/        # repositories, unit-of-work, email, security, ApiResponse
│  ├─ Shop.SL/         # JWT token service
│  └─ Shop.Api/        # controllers, mapping, middleware, Program.cs
├─ client/             # Angular 18 storefront + admin
└─ docs/               # strategy documents
```
