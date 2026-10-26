# Merchant applications and customer registration

Customer and merchant identities stay separate. `/register` explains the choice, `/register/customer` creates a Customer account immediately, and `/merchant/register` accepts a SaaS merchant application. Both forms offer an exclusive account-type radio selector. Customers retain their own orders, returns and review portal; submitting a merchant application does not upgrade a customer token or grant any access to the currently viewed shop.

## Merchant workflow

The applicant supplies name, email, phone, merchant username, password (12+ characters), store name, business category/description, and Commerce or Catalog activity. A published free trial and acknowledgment are required. Passwords are hashed using the existing password service; neither plaintext nor hashes are exposed in review/status responses. Application submission returns Pending and no token.

`/merchant/register` also provides a password-protected application-status lookup. Approval exposes the assigned store hostname and original username. This lookup uses the application password, independently of subsequent merchant password changes. Application password recovery and applicant email verification are not implemented in this increment; the operator must verify contact details during manual review. No approval email or Gmail delivery is claimed.

`/platform/applications` is PlatformAdmin-only. Reviewers see applicant/business details and the accepted trial snapshot. Approval requires a valid hostname and an operator-configured, new empty database reference, reusing existing provisioning and isolation safeguards. Database credentials never come from the applicant. Successful approval creates a Merchant account with the applicant's chosen password, initializes the requested store name/activity, grants the accepted trial, and activates the store. A rejection requires a note visible to the applicant. Rejected emails cannot silently submit a second request; operator-assisted reconsideration is outside this version.

Provisioning uses Pending → Provisioning → Approved/Failed, with optimistic version checks. Duplicate/stale approvals cannot grant another store or trial. Validation failures before store registration return to Pending; refresh review data before retrying. A process interruption or provisioning failure requires operator inspection: never delete or automatically reuse a partially created database. Failed stores remain unavailable. Rejection is only allowed while Pending. The review screen currently shows the latest 200 requests, prioritizing pending applications.

## Free trial

A one-time additive registry upgrade seeds a public **Free Trial**, initially 14 days / 50 products / zero price. Existing draft and demo plans remain unchanged. A marker prevents restarts from recreating a renamed or disabled offer. `/platform/plans` manages duration, limits, active status, and the separate **Show this free trial on merchant registration** option. Only active, explicitly published, zero-price, positive-trial-day plans are eligible. Private demo plans are excluded even when free.

The applicant submits the displayed plan version; stale offers must be refreshed. Plan name, days and product limit are snapshotted on submission. Later edits cannot silently change those accepted terms. Approval still requires the source plan to remain active and public. The trial starts at successful approval, not application time. Store subscription and trial history use existing mechanisms; expiry blocks selling/merchant writes under existing subscription rules and does not delete products/orders. No payment data is requested and no automatic charge or renewal is introduced.

## Deployment and authorization

All onboarding endpoints live under `/api/platform/onboarding`; the existing standalone middleware returns 404 for the entire platform surface. Standalone owners use their installed merchant credentials. Public application submission/status are rate limited by remote IP independently of Host, so varying the Host does not bypass their limits. Application details/list/review require the appropriate password or PlatformAdmin role. Customer/Merchant tokens cannot approve applications. Merchant tokens remain scoped to their own store after approval.

Registry changes are additive SQLite schema changes in SubscriptionSchema. No tenant business tables are recreated. Approvals work with the configured tenant provider through existing SQLite/PostgreSQL provisioning; this increment's new workflow integration checks use isolated SQLite tenants. Existing provider support remains in the shared provisioning code.

## Validation

- API and Angular production builds.
- Arabic UI coverage audit.
- `tests/security-smoke.ps1 -ApiDll src/Shop.Api/obj/verification-output/Shop.Api.dll -AdditionalChecks tests/merchant-onboarding-checks.ps1` in temporary databases, including baseline tenant permissions/subscriptions.
- Tests cover pending/no-token signup, public-role injection, duplicate email, consent, stale offers, private demo filtering, protected status/review, approval/rejection, original password, trial snapshots/start date, repeat approval, cross-store denial, catalog mode, and standalone refusal.

The local demo update is preceded by a registry backup under ignored `src/Shop.Api/obj/`; tests do not populate the live applicant list or create live stores.

Final local verification: 362 isolated HTTP checks passed, API/Angular builds passed, and all 628 audited template phrases have Arabic translations. Browser checks covered Arabic/English switching, both account-type radio transitions, 390px phone width without horizontal overflow, and visibility of only the published Free Trial. Live demo counts stayed 16 kids products and 15 original products; the live application list was left empty.
