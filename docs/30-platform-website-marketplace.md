# Matjark public platform, plans and marketplace

The user selected **متجرك / Matjark** as the current platform name. Public platform entry is separate from merchant storefronts: `http://platform.localhost:4300/`, with tenant sites such as `http://kids.localhost:4300/` preserved. `Platform:PublicHost` configures the reserved host, defaulting to platform.localhost. It must never equal a registered merchant hostname; provisioning rejects that host. Standalone installations keep their storefront root and disable every `/api/platform` endpoint, including public marketing/marketplace APIs.

## Visitor journeys

- The platform home presents the introduction, about/mission, public plan cards, and Shop now. Public copy is bilingual and editable at `/platform/website`; updates are version checked and rendered as plain text.
- `/platform/shop` displays a live, paginated cross-store catalog with search and store attribution. Product links open the owning merchant hostname and product ID, so overlapping IDs remain unambiguous.
- A plan card links to `/merchant/register?planId=...`; the published plan is selected automatically. Customer and merchant access pages share a neutral visual shell. Merchant-specific fields remain different from customer fields.
- On the platform host, choosing Customer opens the marketplace. Customer accounts, carts, shipping, checkout and order tracking remain within the store the customer purchases from. There is **no shared customer login, combined multi-merchant cart, split payment, or platform checkout** in this increment.
- The original merchant provisioning process still requires the operator to configure a new, empty database reference before approval. No database connection information is accepted from applicants.

## Plan management

`/platform/plans` controls name, optional Arabic name, description/Arabic description, price in minor units/currency, period in months, trial days, product quota, active/published flags, and enforced optional features. No speculative paid prices were published; existing Starter/Growth/Pro drafts stay disabled. The existing Free Trial is the public default. Private demo offers remain unpublished.

Supported optional entitlements are Discount codes, Newsletter signups, Storefront customization, and Marketplace visibility. Products/inventory/orders remain core functionality, subject to the product quota and existing subscription expiry rules. Feature keys are server validated and snapshots are persisted on application/subscription. Plan edits do not silently alter an existing subscription or pending applicant's accepted terms. An explicit activation or renewal takes the current plan feature snapshot.

Enforcement is server-side: merchant discount endpoints, coupon resolution at quote/checkout, newsletter submission and subscriber access, and storefront writes check entitlements. Public storefronts hide unavailable newsletter signup. Marketplace filters out stores without the visibility entitlement. Frontend menus/route guards mirror these capabilities. Existing subscriptions are additively upgraded with their previous full feature access; standalone bypasses SaaS feature gates.

Published active paid plans can be selected before registration. Applications snapshot the advertised price, currency, billing period, trial duration, quota and feature keys. An applicant never supplies trusted prices or role grants. A paid plan without trial cannot be approved until the platform administrator explicitly confirms manual payment and records a reference. Trial offers start after approval; free plans need no payment. No payment gateway, automatic debit, refund, or recurring collection is simulated. Actual gateway work remains deferred.

## Cross-store public catalog

`GET /api/platform/marketplace` reads registered Active merchants via operator-owned connection references and projects only public, active catalog fields. Subscription-required merchants must have a current subscription and Marketplace feature. Merchant-owned storefront content has an opt-out (`IncludeInMarketplace`). Suspended, failed, opted-out and expired stores are excluded.

Results carry StoreId + ProductId, hostname, public brand/name/Arabic product name, image URL, currency, minimum active variant price, and availability. No customer identities, emails, orders, inventory internals, private settings or database references are exposed. The response explicitly reports partial results if a store is unavailable. Requests are rate limited. Read throughput scales with store count; a dedicated public search index is a future scale improvement, not required for the current small deployment. Checkout always reprices in the owning store.

## Local operations and verification

The dev frontend remains loopback-only on port 4300. Local allowedHosts are recorded in angular.json; Angular 18's application builder reports that this option is ignored, so it is not claimed as a security boundary. API CORS explicitly includes platform.localhost:4300. Public uploads use the owning store API hostname, preserving existing upload isolation. A registry SQLite backup was saved under ignored obj before applying additive plan/application columns and PlatformWebsite content storage.

Validation scripts use temporary registries and tenant databases and do not create live applicants or paid subscriptions:

- `tests/merchant-onboarding-checks.ps1`: existing signup/review/snapshot/standalone regression.
- `tests/platform-marketplace-checks.ps1`: public fields, paid plan selection/manual approval, platform content permissions/CAS, marketplace opt-out/suspension, real server feature gates, accepted paid price/duration, and platform vs merchant host capability.

Browser verification covers the public home in Arabic/English, plan selection into registration, the customer marketplace path, successful existing platform-owner login, and the admin plan editor. Additional viewport/screenshot validation is recorded with the delivered previews. Existing net7/package advisory build warnings predate this change.

Design references were the first-party Shopify pricing/product hierarchy (https://www.shopify.com/pricing) and the requested store-platform model. Existing own branding/content is used; no third-party logo, pricing or unsupported feature claim is copied.

Final validation: the marketplace/paid-plan suite passed 358 HTTP checks and onboarding regression passed 362 (overlapping baselines, not additive totals). Angular/API builds passed; 694 audited template phrases have Arabic coverage. Cold Arabic page load correctly registers its locale before prices render. Browser verification showed 31 real demo catalog products, successful cross-store search, loaded hero images, preselected plan registration, and 390px RTL home/marketplace layouts without horizontal overflow. Screenshots are saved in docs/previews/matjark-*.jpg.
