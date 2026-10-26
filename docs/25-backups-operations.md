# Backups, recovery and operation history

Platform owners use `/platform/operations`. Merchants only see their own management history at `/merchant/activity`. Database snapshots, encryption keys and error history are not accessible with merchant or customer tokens.

## What a snapshot includes

Each snapshot is under `src/Shop.Api/store-data/operations/backups/BACKUP_ID`: a consistent SQLite online backup or PostgreSQL custom-format dump, that store's immutable uploaded files, the email payload key ring, and a SHA-256 manifest. The original demo store also includes legacy uploads outside the `stores` directory; other stores' directories are excluded. The platform registry can be backed up separately, including subscriptions, operation history and session generations.

Snapshot storage is outside `wwwroot`. The operations directory is restricted to the running Windows account and SYSTEM on Windows, or owner-only permissions on Unix. This is access control, not independent encryption of database files. Protect the disk, restrict the server account, and arrange encrypted off-server copies before production. Backups omit SMTP passwords, database passwords, JWT signing secrets and application configuration: escrow these separately. The shared email key ring is secret and must remain protected with the snapshots.

Manual creation and optional scheduling are available. Scheduling checks persisted policy while the API is running; it catches up after a restart but does not wake a stopped server. Default policy is disabled until configured. No automatic deletion is performed. Retention, storage quotas, replication and external uptime monitoring are not implemented.

## Restore rehearsal

`Test restore in a separate copy` first checks all payload hashes and rejects linked/traversing paths. SQLite is copied into a separate private restore directory and checked for database integrity and foreign keys. PostgreSQL is restored with `pg_restore` into a uniquely generated scratch database, inspected, then dropped. Original store databases are never overwritten by a rehearsal. Product, order and table counts plus the check state are shown in the dashboard; corrupt snapshots are marked Failed. This verifies storage/database recovery, not the complete application journey.

Set `Operations:PostgresBin` to the installed PostgreSQL tools directory (for local validation: `obj/postgres-runtime/pgsql/bin` relative to the API root). Tools must match the server's supported dump format. Credentials are passed in child-process environment variables, not command arguments or logs. The validation role currently uses the store's configured connection and needs permission to create/drop scratch databases; production should use a dedicated restricted recovery operator/procedure. Tool calls time out after two minutes.

## Actual SQLite recovery

This replaces business data and is an explicit operator procedure, not a web button. Stop all API instances and other writers first. The app holds `store-data/api-instance.lock` while running; the recovery command refuses to start against the same running content root. This is a single-deployment filesystem guard, not a distributed database lock.

From the repository in PowerShell 7:

```powershell
pwsh -NoProfile -File scripts/restore-store.ps1 -BackupId 'BACKUP_ID' -StoreId 'STORE_ID'
```

Use an existing snapshot ID and its exact matching store ID (`default` for the demo). For a release/deployed build supply `-ApiDll` explicitly. The command performs a rehearsal, validates the registry's database identity, restores missing immutable files/keys, and refuses conflicts. It rotates the store's centrally stored session generation before replacement, so old merchant/customer sessions stay invalid even if replacement fails. It preserves the former database/WAL/SHM files beside the original for operator rollback, installs the snapshot and checks integrity. Old customer action links are deleted and queued/failed emails become `RecoveryHeld`, preventing replay after a historical restore. RecoveryHeld messages require explicit operator review; the dashboard does not offer one-click resend of them. Missing files copied before a failed replacement may remain unreferenced; existing assets are never overwritten.

Start the API again, sign in, inspect catalog/images/orders/inventory and confirm account recovery before reopening access. A recovery snapshot cannot restore transactions made after its creation. Actual SQLite replacement, refusal while running and session revocation are tested only on isolated disposable stores, never on the user's working database.

## PostgreSQL and platform-registry replacement

Production PostgreSQL replacement and platform-registry replacement remain database-operator procedures. Verify the bundle first, stop writers, preserve the current database, restore the dump into an empty recovery database, verify it, then perform a reviewed connection/rename cutover while preserving registry identity checks. Do not run an unreviewed `--clean` restore against a live database. Before PostgreSQL cutover revoke merchant/customer sessions through the platform-only `POST /api/platform/operations/stores/STORE_ID/revoke-sessions`. In the restored store delete `CustomerActions` and hold Pending/Sending/Failed rows in `EmailOutbox` as RecoveryHeld before starting workers. A restored platform registry can revive historical security generations; rotate signing secrets and revoke store sessions before reopening it. Preserve the current registry independently before replacing it. There is no automatic production PostgreSQL cutover endpoint.

## Activity and errors

The central journal records management writes, their actor/role, static route pattern, status, UTC time and correlation reference. It excludes request/response bodies, query strings, credentials, email bodies and customer details. It records failed backups/restores, HTTP 5xx and email delivery failures. Error review is an acknowledgement, not a fix. Merchant activity is filtered to its resolved store and Merchant role; the client cannot choose another store.

This is a metadata audit trail, not a field-by-field change history or tamper-proof external log. Persistence is best effort when the registry itself is unavailable, with a local warning fallback. Alerts are displayed in the dashboard; external email/on-call alerts, fine-grained staff permissions and monitoring outside the API process remain future work.

## Validation

`tests/security-smoke.ps1 -AdditionalChecks tests/operations-checks.ps1` exercises private snapshots, assets, corruption rejection, scheduling, recovery, revoked sessions, error acknowledgement and journal privacy. PostgreSQL tenancy tests additionally use actual `pg_dump`/`pg_restore` and verify tenant activity isolation. All mutable test data is isolated from the normal project database.
