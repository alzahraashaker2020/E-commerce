# Deployment mode and store activity

The same application supports two independent choices. Installation operators select `Saas` or `Standalone`; merchants select `Commerce` or `Catalog` under **Storefront & design → Activity mode**. Existing installations default to SaaS and existing stores to Commerce. This change does not switch the running demo away from SaaS, rename URLs, copy databases or delete subscription history.

## Operator-owned installation configuration

Set these keys in the normal server configuration or environment before starting the API; the example JSON files in this directory are documentation, not automatically loaded overrides.

- `Deployment:Mode`: exactly `Saas` (default) or `Standalone`.
- `Deployment:StandaloneStoreId`: selected persistent store ID, default `default`. For an existing merchant use the actual registry ID, never a hostname or a new generated ID.
- `Deployment:BusinessMode`: optional `Commerce` or `Catalog` override. Omit it to allow the merchant's saved activity setting. An override is read-only in the dashboard and enforced by the server.

Invalid values stop startup. Deployment configuration cannot be changed by merchant API payloads or browser flags.

SaaS retains host-based resolution, platform administration, subscriptions, quota enforcement and separate tenant databases. Standalone resolves only the selected existing store and its registered host; other hosts do not fall through to that database. Existing store suspension still applies. Platform login/APIs and merchant subscription APIs return 404. No subscription or product quota is required, including when an old subscription record is expired or cancelled. Returning to SaaS reapplies its unchanged subscription state.

## Standalone installation and migration boundaries

A fresh standalone installation can use store ID `default`, the configured default database connection and `Platform:DefaultStoreHost`. It creates local store identity metadata and the existing demo seed; replace demo accounts and sample data as part of production provisioning. For a non-default store, provision the store first. A missing non-default database stops startup instead of recreating data.

The local registry file remains an installation metadata store: it supplies identity, activity journal, backup schedule and session revocation versions. It requires no central SaaS server, operator account, plan or network subscription service. Existing operator and subscription rows are preserved but unused by standalone; new standalone installations do not bootstrap operators or seed plans.

When delivering a merchant installation, package only that merchant's database, uploads under its original store ID, local identity/security metadata, required email encryption keys and private server configuration. Do not distribute a multi-tenant registry or other merchants' files. Physical export/copy and domain/connection changes are deployment operations, not an automatic side effect of changing modes. Database identity checks remain enabled. Keep an independent backup before migration.

Database upgrades, outbox processing and scheduled store backups filter to the selected store in standalone. Other registered stores are not initialized or processed. The selected host and deployment origins must also be configured at the web server/CORS level when moving to a real domain.

## Commerce versus Catalog

The store activity is persisted in the versioned storefront document, defaulting to Commerce for old documents. It works in both SaaS and standalone.

- Commerce retains the existing cart, authoritative checkout pricing and order flow.
- Catalog retains product pages, prices, collections, media and management. Purchase buttons/cart links disappear; cart and checkout page navigation returns to the catalog. Direct cart requests and checkout writes return 403 at the API before order creation.
- Historical orders, private tracking, returns, refunds, stock management, customer accounts, galleries and email functionality remain available. Existing cart contents and order records are not deleted. Switching back to Commerce restores the purchase flow.

Catalog is a display mode for the current product system; it does not yet add service bookings, custom quotations or a portfolio editor.

## Owner operations

Standalone owners use `/merchant/operations` with their existing merchant session. `/api/merchant/operations` offers the selected store's backup creation, isolated restore rehearsal, schedule and activity/error review. IDs and query parameters cannot widen access to platform or other-store backups/events. Anonymous/customer access remains denied. These owner-operation routes are disabled in SaaS; its platform operations remain unchanged.

Automatic backups run only while the API is running. Offline replacement still uses `scripts/restore-store.ps1`; restore rehearsal does not overwrite the live database. Off-server storage and production PostgreSQL cutover remain separate operator tasks.

## Public capabilities and verification

`GET /api/deployment` exposes only the installation mode. `GET /api/store` additionally exposes the resolved store's effective activity and capabilities. Angular loads these before routing and uses the same rules for links and page guards. Server checks are authoritative.

Regression suite: `tests/security-smoke.ps1 -AdditionalChecks tests/deployment-profile-checks.ps1`. It exercises host/token isolation, standalone with a cancelled subscription, ignored quotas, real checkout, owner-only backups and cross-store rejection, persistent catalog mode, an operator override, and return to SaaS. Run PostgreSQL coverage through `tests/postgres-schema-checks.ps1 -ApiIsolation -AdditionalChecks tests/deployment-profile-checks.ps1` with an isolated PostgreSQL runtime.
Validated: API and Angular production builds; 393 SQLite profile/permissions HTTP checks including fresh standalone and invalid configuration; 380 PostgreSQL profile/permissions HTTP checks plus 33 PostgreSQL schema checks; 402 product, stock, shipping and storefront HTTP checks. Live data preserved: 16 kids products and 15 original-store products.
