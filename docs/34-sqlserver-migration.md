# SQL Server conversion

Authorized scope: platform registry and every registered merchant database, preserving existing data, credentials, tenant IDs, hostnames, uploaded images, encrypted email payloads and their existing key ring. The old SQLite files are retained. No existing SQL Server database is overwritten.

## Implementation and responsibilities

- DAL adds Microsoft.EntityFrameworkCore.SqlServer **7.0.20**, matching existing EF Core. PostgreSQL/SQLite providers remain supported. `SqlServerSchema` separately versions supplemental SQL Server tables; PostgreSQL migrations and SQLite upgrades are never applied to SQL Server.
- `StoreConnections` resolves SQL Server per merchant and records a distinct server/database identity. `PlatformDatabase` selects the registry provider explicitly. New SQL Server stores use the same provisioning, subscription and authorization boundaries.
- SQL dialect branches cover idempotent inserts, top-N reads, unique conflicts/deadlocks and connection-bound transactions. Business policies and public API routes/contracts remain unchanged. Case-sensitive binary collation preserves existing SQLite exact string comparisons; money retains the current precision.
- Platform operations journal, backup policy and security versions also move to SQL Server. Backups use native COPY_ONLY/CHECKSUM, followed by actual restoration to a randomly named database and DBCC CHECKDB. Only that disposable restore database is dropped. Existing offline replacement command remains SQLite-only; SQL Server live replacement is an operator task, as PostgreSQL already was.
- Native SQL backups currently require the API and SQL Server to share accessible local storage. Native scratch backups are kept in the dedicated `store-data/sqlserver-native` directory. Its Windows ACL grants access only to the current API account, SYSTEM and the SQL Server service; application secrets, uploads and key-ring ACLs are untouched. The native backup is retained alongside the application's hashed snapshot. Remote SQL Server needs a shared-storage procedure; no claim of remote backup support.

## Transfer tool and acceptance

`tools/Shop.DatabaseTransfer` acquires the same exclusive API instance lock. Stop all instances using this content root before running it. It snapshots the platform and all registered SQLite stores, creates **new named SQL Server databases**, initializes the SQL Server schema, imports data within per-database transactions, reenables/trusts foreign keys and compares canonical SHA-256 row multisets and row counts for every source table. AUTOINCREMENT high-water marks are preserved as SQL Server identity reseeds. Unknown tables/column mismatches abort; they are not silently skipped. Platform database identities are intentionally transformed after comparison. The operation is staged across separate databases, not presented as one distributed atomic transaction.

The utility writes snapshots, verification evidence and a proposed local configuration under `src/Shop.Api/obj/sqlserver-transfer/<prefix>`. It never activates the result automatically. A failed attempt leaves unpublished target databases for inspection and refuses to reuse their names. Source files are read-only; assets/key files remain in place and are hashed in the evidence.

After successful provider regression tests and content comparison, copy the proposed configuration to ignored `src/Shop.Api/sqlserver-local.json` and restart. Environment variables and private `email-secrets.json` take precedence. Visual Studio's existing full-UI launcher uses the same API configuration. Keep credentials out of committed appsettings.

## Tests

- Existing HTTP harness accepts `-SqlServerTests` and creates generated `matjark_test_<guid>` fixture databases, never production data. The same auth, product/gallery, checkout/replay/stock, returns, account-security, tenancy and storefront assertions run unchanged. SQL Server setup uses a longer test-only request timeout for database creation.
- Test-only expiry/delivery controls require both a temp security-test directory and a generated local test database name.
- Existing infrastructure transaction tests accept `SHOP_TEST_SQLSERVER`; the same rollback and competing-worker assertions execute against SQL Server.
- `tests/sqlserver-backup-checks.ps1` tests native backups/restores for platform and merchant databases.

## Rollback

Stop the API first. Before any SQL Server writes, remove/rename the local overlay and restart with original SQLite configuration. The old registry identities, stores, files and key ring remain available. **After new SQL Server writes, the old SQLite files are stale**: export/reconcile the new data before switching back; never advertise that as a lossless one-click rollback. Do not delete either set during review.

Future schema changes must update the SQL Server-specific schema path and tests as well as the retained provider paths. This task does not upgrade the framework or remove pre-existing dependency advisories.

References: [EF Core provider-specific migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers), [SQL Server native backup](https://learn.microsoft.com/en-us/sql/t-sql/statements/backup-transact-sql), [VERIFYONLY limitations](https://learn.microsoft.com/en-us/sql/t-sql/statements/restore-statements-verifyonly-transact-sql).

## Local activation and verified results — 2026-11-07

Activated ignored `src/Shop.Api/sqlserver-local.json` on the local **localhost** SQL Server 2022 instance, using Windows authentication. The final databases are:

| Database | Purpose | Source tables matched | Source rows matched |
|---|---|---:|---:|
| `matjark_20261107_platform` | Platform, subscriptions, accounts, operations | 11 | 168 |
| `matjark_20261107_store_0` | KidsClothingDemo | 31 | 209 |
| `matjark_20261107_store_1` | Original DefaultStore | 31 | 115 |

All 73 source tables matched by row count and canonical full-row hash before activation; 60 upload/key files hashed. The original files remain intact. SQL Server's provider-version marker is additional target metadata. Existing IDs, high-water marks and password hashes were retained. Database identity now uses SQL Server's authoritative server name so `localhost` and `.` aliases cannot register the same database twice.

Evidence is retained in ignored `src/Shop.Api/obj/sqlserver-transfer/matjark_20261107/verification.json`, source snapshots and the generated configuration next to it. Earlier unpublished staging databases (`matjark_sql_20261107_*`) were deliberately not overwritten or deleted.

Provider validation:

- 309 application tests and 17 architecture/contract regression tests pass.
- 11 final infrastructure tests pass, including SQL Server stock/order/return rollback, gallery preservation, competing outbox workers and server-alias identity. The pre-existing SQLite infrastructure suite also passed (10 tests before addition of the SQL-specific alias check).
- SQL Server HTTP suites: API contracts 151, localization/storefront 433, merchant onboarding 362, marketplace/plans 358, deployment profiles 396. Manifest: `src/Shop.Api/obj/refactoring-evidence/sqlserver-final-148ace2f109f43c4a9c944795c81a12e/manifest.json`.
- SQL Server returns/account/email/checkout chain: 678 HTTP assertions; tenant isolation chain: 318; native merchant/platform backup and actual restore/CHECKDB chain: 426. Counts overlap and include polling, not a percentage of unique coverage.
- Live migrated-data checks: platform sign-in and two stores, kids merchant sign-in and 16 products, existing image HTTP 200, original customer sign-in, both UI hosts HTTP 200. No test orders/products were created in migrated live databases.

Framework/package advisories and the previously documented Windows offline SQLite recovery-test issue remain separate; this conversion does not claim those resolved. No commit, push or external deployment performed.

Exact final assembly confirmation also passed the complete 318-assertion tenant chain after canonical server-name identity was introduced. Manifest: `src/Shop.Api/obj/refactoring-evidence/sqlserver-confirmation-3012d74fde664d9e98cd4346a6b18048/manifest.json`. Native backup required more than a 30-second client timeout in one preceding repeat; the SQL Server-only test harness allows 120 seconds for native backup/restore requests, with every assertion retained. SQLite test timeouts remain unchanged.
