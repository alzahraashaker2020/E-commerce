# 12 — Website Migration Strategy

> **Companion documents:** [11 — Competitive Gap Analysis](./11-competitive-gap-analysis.md) · [13 — Commercial Value Proposition](./13-commercial-value-proposition.md)

## Purpose

A safe, authorized plan to migrate the store owner's existing e-commerce site to our platform (Angular + ASP.NET Core Web API + PostgreSQL) **without losing important business data or organic search traffic**. The strategy uses only **authorized exports and official integrations**. It never assumes that passwords, payment credentials, or other sensitive secrets can be moved directly.

### Source-platform basis

The reference site is inferred to be **Shopify** (`/collections/all` URL pattern plus a public agency portfolio listing). See [Doc 11 — methodology](./11-competitive-gap-analysis.md) for the evidence and labels. This plan is written against **Shopify-typical export mechanisms** (admin CSV exports, the Shopify Admin API, and official data exports). If the platform differs, the entity mapping and extraction steps adjust, but the phased approach and safety rules stand.

---

## Core safety principles (non-negotiable)

1. **Authorized access only.** Use the owner's own Shopify admin exports and API credentials issued by the owner. No scraping, no circumventing rate limits (note the 429 seen during research — see [Doc 11](./11-competitive-gap-analysis.md)).
2. **Passwords are never migrated in plaintext.** Shopify does not expose customer password hashes, and even if a hash existed it must not be trusted or reused. Customers are re-authenticated via **forced password reset / email-based re-auth / OAuth** (see "Customer account migration" below).
3. **Payment credentials are never moved.** Card data and gateway secrets stay with the PCI-compliant providers. We **re-onboard** payment providers on the new platform; we do not copy credentials or stored payment methods.
4. **No loss of indexed URLs.** Every indexed URL is either preserved or **301-redirected**. SEO continuity is a release gate, not an afterthought.
5. **Reversible cutover.** We keep a tested rollback path until the new platform is proven in production.
6. **Verify before trust.** Every migrated entity is reconciled by count and by sampled content before go-live.

---

## Phased plan

```
Export  →  Transform  →  Load  →  Verify  →  Parallel run  →  Cutover  →  (Rollback if needed)
```

### Phase 0 — Preparation
- Obtain written authorization and admin/API access from the owner.
- Freeze non-essential catalog changes window; document the current URL structure and sitemap.
- Capture **baseline metrics** (traffic, top landing pages, conversion, AOV) for post-migration comparison — see [Doc 11 outcomes](./11-competitive-gap-analysis.md).

### Phase 1 — Export (authorized extraction)
- Export products, variants, collections, customers (profile fields only — **no passwords**), orders, and images via Shopify admin CSV export and/or the Shopify Admin API.
- Export the current URL list and sitemap for the redirect map.
- Store exports in a secured, access-controlled location; treat customer data as PII.

### Phase 2 — Transform
- Normalize exported data into our PostgreSQL schema (see data-mapping table).
- Resolve variants into our product/variant model; map option sets.
- Generate the **redirect map** (old URL → new URL) and flag any URL slug changes.
- Scrub/transform metadata; convert image references to our storage paths.
- Mark customer records as **requiring re-authentication** (no password field imported).

### Phase 3 — Load
- Bulk-load into PostgreSQL via idempotent, re-runnable import jobs (ASP.NET Core import services).
- Load images into our media storage; attach to products/variants.
- Load SEO metadata and the redirect map.
- Keep import logs with per-entity success/failure counts.

### Phase 4 — Verify (reconciliation)
- Reconcile record counts: products, variants, collections, customers, orders, images.
- Sample-check content fidelity (descriptions, prices, variant options, order totals).
- Validate every redirect resolves (old URL → 301 → correct new URL).
- Validate SEO metadata (titles, descriptions, canonicals) on key pages.
- Run data-quality checks (orphaned variants, missing images, null required fields).

### Phase 5 — Parallel / shadow run
- Run the new platform alongside the live site on a staging domain.
- Shadow-test real journeys: catalog → filter → paginated browse (preserved position) → PDP → cart → guest checkout → confirmation → tracking.
- Re-onboard and test payment providers in sandbox, then with small live-value tests.
- Load/performance test against performance budgets.
- Sign off against the go/no-go checklist.

### Phase 6 — Cutover (minimal downtime)
- Schedule a low-traffic window; announce to the owner.
- Final **delta export** of orders/customers created since the last export; load deltas.
- Lower DNS TTL in advance; switch DNS / domain to the new platform.
- Publish 301 redirects at the edge/app layer immediately on cutover.
- Monitor error rates, checkout success, and search console for crawl errors.

### Phase 7 — Rollback (if needed)
- Keep the old platform live and reachable (unchanged) until sign-off.
- Rollback = revert DNS to the old site (TTL kept low for fast propagation).
- Because we only **read** from the source and never mutate it, the old store remains a valid fallback.
- Define a rollback decision window (e.g., first 24–72h) and the metrics that trigger it.

---

## Data-mapping table (Shopify entity → our PostgreSQL schema)

> Target table/column names are indicative and align to the platform build; adjust to the final schema.

| Shopify entity / field | Our PostgreSQL target | Transform / notes |
|---|---|---|
| Product | `products` | Core product record |
| Product title / body_html | `products.name` / `products.description_html` | Sanitize HTML |
| Product handle (slug) | `products.slug` | Feed redirect map if slug changes |
| Product vendor / type / tags | `products.brand` / `products.category` / `product_tags` | Tags → join table |
| Variant | `product_variants` | One row per option combination |
| Variant SKU / price / compare_at | `product_variants.sku` / `.price` / `.compare_at_price` | Validate numeric/currency |
| Variant options (size/color) | `variant_options` / `variant_option_values` | Normalize option sets |
| Inventory quantity | `inventory.quantity_on_hand` | Snapshot at export; reconcile at cutover |
| Collection | `collections` | Map smart/manual collections |
| Collection membership | `product_collections` | Join table |
| Product image | `product_media` | Download, re-host, link to product/variant |
| Customer (profile) | `customers` | **No password field imported** |
| Customer email / name / phone | `customers.email` / `.full_name` / `.phone` | PII — handle securely; flag re-auth required |
| Customer addresses | `customer_addresses` | Default/billing/shipping flags |
| Customer marketing consent | `customers.marketing_consent` | Preserve consent state; do not opt-in by default |
| Order | `orders` | Historical orders (read-only import) |
| Order line items | `order_items` | Link to product/variant snapshots |
| Order totals / taxes / shipping | `orders.subtotal` / `.tax` / `.shipping` / `.total` | Reconcile totals exactly |
| Order status / fulfillment | `orders.status` / `fulfillments` | Map status vocabulary |
| Order financial status | `orders.payment_status` | Historical record only; no gateway secrets |
| Discounts / codes | `discounts` / `discount_codes` | Recreate rules; validate redemption history as historical |
| URL / sitemap entries | `seo_redirects` | Old URL → new URL, 301 |
| SEO title / description | `seo_metadata` | Per product/collection/page |
| Payment gateway config | **Not migrated** | Re-onboard providers; credentials never copied |
| Customer passwords | **Not migrated** | Forced reset / email re-auth / OAuth |
| Stored payment methods | **Not migrated** | Re-captured by PCI-compliant provider |

---

## Customer account migration with secure authentication transition

- **Import:** profile fields only (email, name, phone, addresses, consent state). **No passwords.**
- **Authentication transition options (choose per owner preference):**
  1. **Forced password reset** — on first login, the customer receives a reset link to set a new password on our platform.
  2. **Email-based re-auth (magic link)** — passwordless first login via a one-time email link, then set credentials.
  3. **OAuth / social login** — let customers link an external identity provider.
- **Communications:** send a clear, authorized "we've upgraded — please reset your password" notice. Respect marketing consent state; this is a transactional/service message, not marketing.
- **Rationale:** Shopify does not expose reusable password material, and reusing any credential material would be a security risk. Re-authentication is the only safe path.

---

## Payment & shipping reconfiguration

- **Payment:** Re-onboard each payment provider on the new platform using the owner's accounts. Validate with sandbox then small live-value transactions during the parallel run. **Never** migrate gateway secrets or stored card data.
- **Shipping:** Recreate shipping zones, rates, and carrier settings in our configuration. Validate rate calculation against known cases before cutover.

---

## SEO continuity

- Build the **redirect map** in Phase 2; publish 301s at cutover (Phase 6).
- Preserve URL slugs wherever possible; only redirect where a change is unavoidable.
- Preserve/transfer SEO titles, meta descriptions, and canonical tags.
- Submit the updated sitemap and monitor Search Console for crawl/indexing errors post-cutover.
- Treat any drop in impressions/clicks as a rollback-window signal to investigate.

---

## Risk register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Loss of organic traffic from broken URLs | Medium | High | Complete redirect map; validate all 301s in Phase 4; monitor Search Console |
| Data mismatch (counts/totals) after load | Medium | High | Count + sample reconciliation gate before go-live |
| Customer login confusion at re-auth | Medium | Medium | Clear comms + magic-link fallback; support playbook |
| Payment misconfiguration at cutover | Low–Medium | High | Sandbox + small live tests in parallel run; provider sign-off |
| Order/customer delta created during cutover | Medium | Medium | Final delta export + load immediately before DNS switch |
| Image loss or broken media links | Medium | Medium | Re-host and verify media; automated broken-link check |
| Rate limiting / access issues during export | Low–Medium | Medium | Use authorized API with backoff; schedule off-peak |
| PII mishandling during migration | Low | High | Encrypted, access-controlled storage; delete working copies post-verify |
| Extended downtime at cutover | Low | High | Low DNS TTL, rehearsed cutover, off-peak window |
| Inability to roll back | Low | High | Keep source read-only and live until sign-off; DNS revert path |

---

## Go / no-go checklist

- [ ] Written authorization and credentials obtained from owner.
- [ ] All entities exported; export counts recorded.
- [ ] Record counts reconciled (products, variants, collections, customers, orders, images).
- [ ] Content fidelity sample-check passed (descriptions, prices, variants, order totals).
- [ ] Redirect map complete; 100% of sampled 301s resolve correctly.
- [ ] SEO metadata validated on key pages; sitemap ready.
- [ ] Payment providers re-onboarded and tested (sandbox + small live).
- [ ] Shipping zones/rates recreated and validated.
- [ ] Customer re-auth flow tested; comms drafted and approved.
- [ ] Parallel/shadow run passed for all key journeys.
- [ ] Performance budgets met under load.
- [ ] Rollback path tested; DNS TTL lowered.
- [ ] Monitoring/alerting in place for errors, checkout success, crawl errors.
- [ ] Owner sign-off recorded.

---

## Assumptions & open questions

- Confirm the source platform is Shopify and which export/API access the owner can grant.
- Confirm catalog size, variant complexity, and historical order volume (drives effort and timeline).
- Confirm which URLs are indexed and must be preserved/redirected.
- Confirm preferred re-authentication method (forced reset / magic link / OAuth).
- Confirm payment and shipping providers to re-onboard.
- Confirm acceptable cutover window and rollback decision window.
- Confirm data-retention/PII-handling requirements for the migration working copies.
