# Phase 0 — baseline and JWT configuration validation

Date: 2026-10-27. Baseline HEAD: 4a4a1a05b180a63963d77fcaecd9104100b34c61.

## Scope and changes

Typed JwtOptions in Shop.SL and JwtOptionsValidator in API replace repeated raw token-service reads. API validates immediately after configuration loading, before deployment setup, directory creation or database initialization; failures exit 1 with field names only. Options validation is also registered in DI. Both token generation and bearer validation consume the same startup snapshot. Existing signing algorithm, lifetime/clock skew, claim names, roles, authentication events and middleware order remain unchanged. No dependency/project/package/frontend/schema change or key rotation.

Reject: blank or less-than-32-UTF8-byte key; known placeholder markers CHANGE_ME, REPLACE_WITH, placeholder, dev_only, do_not_use_in_production (case insensitive); blank issuer/audience; nonpositive or non-Int32 expiry. A missing expiry retains 120 minutes. Positive Int32 values remain supported; no arbitrary new upper lifetime cap. No HTTPS issuer requirement. Length is not an entropy guarantee. Supplied key/issuer/audience values are not trimmed/transformed.

security-smoke now supplies generated per-run test-only key and explicit issuer/audience/expiry, restoring previous environment values afterward. No existing assertions removed. The added baseline runner saves sanitized per-suite manifests/logs under ignored obj/refactoring-evidence. The new JWT script tests invalid configuration before file/DB effects, valid override, omitted expiry, custom 37-minute expiry, three role claims, wrong issuer/audience/key and valid token access.

## Actual verification

SDK selected: 10.0.402; installed target runtime ASP.NET/Core 7.0.20. Both original and modified API built successfully with --no-restore into separate obj/refactoring-baseline-output and obj/refactoring-verified-output. No running app output overwritten. Initial sandbox build failed without compiler errors; identical elevated isolated build passed. dotnet --info also encountered restricted Service Control Manager access; installed SDK/runtime values were still reported.

| Suite | Original DLL | Modified DLL |
|---|---|---|
| checkout-integrity (includes product/gallery/shipping checks) | PASS 413 HTTP checks | PASS 413 |
| deployment-profile (includes tenant/subscription/features/recovery checks) | PASS 396 | PASS 396 |
| returns (includes outbox/account-email/upload/product checks) | PASS 697 | PASS 696 |

Returns script count varies with email polling, not removed assertions; both runs completed all scenario checks. Assertions were not modified. JWT focused HTTP suite separately passed 172 checks. Final expanded startup/lifetime matrix: PASS 14 cases, including omitted/default 120 minutes and configured 37 minutes.

Evidence manifests: src/Shop.Api/obj/refactoring-evidence/before-7b5a414f29a44724b179cbc9682d2b74/manifest.json and after-6a4393213b8b4056b41e1952f5b323f4/manifest.json. They contain DLL SHA256, schema source hashes, SDK, HEAD, exit code, check count and durations. Baseline binaries were built before production source edits. Harness signing configuration was made explicit for reproducibility on both versions.

All recorded DAL Data/Migrations source hashes and Angular package-lock hash remained identical. No schema upgrade/migration/live data operation, frontend/dependency change, commit/push/deploy or live server restart performed. Tests used loopback hosts and disposable SQLite databases. PostgreSQL and independent remaining feature extensions were not rerun: Phase 0 does not change persistence, and this is not a claim of a full all-provider release certification. They remain mandatory for relevant later adapter work.

## Existing warnings / limits

Build reports existing NETSDK1138 (net7.0), NU1903 (AutoMapper 12.0.0) and NU1902 (System.IdentityModel.Tokens.Jwt 7.0.3). No package upgrade or warning suppression performed. Package/runtime remediation requires a separate compatible proposal. No broad API snapshots/unit test framework was introduced; that is Phase 1.

## Operational impact and rollback

Committed development keys now intentionally fail startup. Provide an existing private key through environment/local ignored secrets before the next API launch; do not rotate an actual key just to apply this refactor. The current running service was not restarted. README documents fresh-install generation and configuration. Rollback is a reviewed code/wiring restoration, not data rollback or key rotation. No git operation authorized.
