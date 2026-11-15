# Unified local database on MAXSYSTEM

User authorization on 2026-11-09: consolidate platform and all merchants into **one SQL Server database**, preserving separation and existing data. Target instance is `.\MAXSYSTEM`, database `Matjark`. No existing source database is deleted or overwritten.

## Storage and isolation

This is schema-per-merchant storage inside one physical database, **not shared business tables with a TenantId column**. Existing primary/foreign keys and overlapping product/order IDs remain unchanged. Platform tables use `dbo`; DefaultStore uses `tenant_defaultstore`; KidsClothingDemo uses `tenant_kidsclothingdemo`.

Each tenant schema has a different SQL principal created WITHOUT LOGIN, with that schema as its default and its own owner. The store DbContext impersonates this principal on both synchronous and asynchronous connection opens. SQL permissions reject reads into the platform or another merchant schema. Pooling is disabled for scoped connections so impersonation cannot leak through connection reuse. Platform administrative connections remain separate logical connections to the same database. The application's existing host, token/store claims, role and subscription checks remain enforced. Database administrators retain normal administrative access.

`SqlServerTenantScope` supplies the scoped connection/factory and explicit new-schema provisioning. Unqualified EF and existing raw SQL identifiers resolve to the selected principal's default schema, keeping HTTP DTOs/routes and business logic stable. No global changes to SQL Server settings, TRUSTWORTHY or cross-database ownership chaining are needed. Schema owners are distinct. Temporary CREATE TABLE permission is revoked after provisioning. A failed initialization is not silently adopted on retry.

Configuration layout `Database:Layout=SharedSchemas` rejects store references without a dedicated schema or pointing to another database. Store database identities include authoritative server, database and schema. Duplicate aliases to the same schema are rejected; separate schemas in the same database are accepted. New merchants still need a server-configured schema reference, the existing approval workflow and a valid subscription.

## Transfer and verification

`tools/Shop.UnifyDatabase` takes the API's exclusive content-root lock, reads the active source configuration, exports consistent per-database schema archives, creates only a **new** database, initializes platform and tenant tables, imports all rows, preserves identity high-water marks and checks full-row SHA-256 multisets and counts. Constraints are reenabled and trusted. The platform's database identities are intentionally changed after data comparison; hosts, store IDs, customer IDs, password hashes and uploaded paths are retained.

Final staged evidence: `src/Shop.Api/obj/unified-transfer/Matjark-20261109013835` contains the source archives, proposed local configuration and `verification.json`. Verified source contents: dbo 11 tables / 174 rows; KidsClothingDemo 32 tables / 210 rows; DefaultStore 32 tables / 116 rows. The source counts include existing SQL Server version metadata. The two tenants retain 16 and 15 products respectively. SQL-level probes denied peer-schema and platform reads.

The earlier `Matjark_Stage*` databases are unpublished transfer attempts, retained for inspection. Original separate databases on the default SQL Server instance and older SQLite files are retained. They are not automatically synchronized after activation.

## Backup behavior

A merchant backup in shared mode is a logical `database.schema.json` snapshot containing only that merchant's tables, plus its existing assets and encryption-key copy. Verification restores it into a randomly named separate database, compares every row and identity, and validates constraints; it never restores over live Matjark. Platform backups use SQL Server native COPY_ONLY/CHECKSUM for the entire physical database and an isolated restore/CHECKDB. Only the temporary restore database created by the verification is dropped.

Schema import uses parameterized inserts in one transaction; this avoids auxiliary metadata-session waits observed with SqlBulkCopy while schema locks were held. DateTime2 parameters explicitly retain seven fractional digits. Snapshot formats are versioned and checksum-verified. Unknown source tables or incompatible columns stop the import rather than being omitted.

After consolidation, an old native full-database merchant backup does not become a schema backup. Actual recovery is an operator procedure; do not restore an old per-store `.bak` over the shared live database. A complete standalone installation must be exported with only the chosen tenant's logical data, not the multi-tenant database.

## Rollback and contracts

Before any new shared-database writes, stop the API and restore the saved local configuration to return to the untouched source databases. After writes, reconcile/export those writes first; switching to the old copies would otherwise lose changes. No commit, push or external deployment is included. Application/frontend behavior, password hashes, order/stock/refund rules and public URLs are preserved.

References: [Microsoft SQL Server schema ownership](https://learn.microsoft.com/en-us/sql/relational-databases/security/authentication-access/ownership-and-user-schema-separation), [schema permission and ownership-chain considerations](https://learn.microsoft.com/en-us/sql/t-sql/statements/grant-schema-permissions-transact-sql), [EXECUTE AS](https://learn.microsoft.com/en-us/sql/t-sql/statements/execute-as-transact-sql).

## Activation and completed validation

Activated the ignored local overlay on 2026-11-09. The preceding overlay is saved beside the transfer evidence as `previous-sqlserver-local.json`. The current application and Visual Studio full-UI launcher use **Matjark on .\MAXSYSTEM**. Public API routes and frontend URLs remain unchanged; no new package dependency was introduced for consolidation.

Verified final storage: 75 tables / 500 source rows: dbo11/174, kids32/210, default32/116. JSON archives retain per-table row hashes and identity high-water marks. SQL permission checks reject cross-schema SELECTs under each store principal. The original separate databases remain untouched; staging databases are not used by the application.

Completed isolated HTTP regression chains on a single database with tenant schemas: tenancy/subscriptions/quotas/checkout/email/return/backup/design/session-revocation **318** assertions; checkout/stock/uploads/account recovery/encrypted outbox/returns chain **679**; merchant schema-only and platform native backup/actual isolated restore **426**. Counts overlap and include polling. Existing architecture/contract regression tests: **17 passed**. Test-only email polling allows up to30 seconds for the shared schema tests; the original mode's bounds are unchanged. Earlier attempts exposed transactional bulk-copy metadata waits and DateTime parameter precision loss in restore; parameterized DateTime2 inserts fixed both and all repeated chains passed. No regression assertions removed.

Live read-only checks against migrated data: platform account signs in and sees two stores; kids merchant signs in and sees16 products/two orders; existing image returns200; original customer signs in; storefront returns200. No test orders/products were added to the live Matjark database. Password hashes, tenant IDs, old orders and image paths remain unchanged. API/UI launcher28612 was started (verify identity before using this PID later).

Known old framework/package advisories and the separate Windows SQLite offline-recovery issue remain out of scope. No commit/push/external deployment performed.
