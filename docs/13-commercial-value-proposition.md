# 13 — Commercial Value Proposition

> **Companion documents:** [11 — Competitive Gap Analysis](./11-competitive-gap-analysis.md) · [12 — Website Migration Strategy](./12-website-migration-strategy.md)

A professional proposal to build a superior, owned e-commerce platform (Angular frontend, ASP.NET Core Web API backend, PostgreSQL database) and to migrate the store owner's existing site to it. This document is written for a decision-maker and is deliberately honest about tradeoffs. It makes **no unverified negative claims** about the existing site.

---

## 1. Executive summary

We propose to replace the owner's current Shopify-based store (platform **inferred**, not observed — see [Doc 11](./11-competitive-gap-analysis.md)) with a purpose-built, fully owned platform on Angular + ASP.NET Core Web API + PostgreSQL. The aim is a commerce experience the owner **controls end-to-end** — customer journey, admin workflows, data, analytics, and AI-assisted operations — without per-app subscription sprawl.

This is a strategic tradeoff, not a slam-dunk. A managed platform like Shopify offers real strengths (fast setup, PCI-compliant hosted checkout, managed hosting, mature integrations). A custom platform trades those conveniences for control, differentiation, and owned data — at the cost of build effort and ongoing hosting/maintenance/security responsibility (see §10). We recommend migration **if** the owner values control, differentiation, and owned analytics enough to accept that ongoing responsibility.

All benefit figures in this document are **directional** and require a measured baseline before they can be quantified. No revenue increase or cost saving is guaranteed.

## 2. Current platform assessment

- **Platform basis (Inference):** The reference site (`makani-group.com/collections/all`) is **inferred** to run on Shopify, based on the `/collections/all` URL pattern and a public agency portfolio listing. It could **not** be crawled (HTTP 429), so we hold no observed on-page data. See [Doc 11 methodology](./11-competitive-gap-analysis.md).
- **Framed as "what a typical Shopify setup provides," not as negatives:** A standard Shopify store typically offers a catalog with collections, product pages, cart, hosted PCI-compliant checkout, native analytics, discounting, and app-based extensions. These are genuine strengths we acknowledge.
- **Fresh-start assumption (Assumption):** The project workspace was empty at the start of this engagement. There were **no previously agreed requirements**. The committed stack (Angular + ASP.NET Core Web API + PostgreSQL) and the requirement set are being **established fresh** in these three documents. The owner should treat them as proposals to confirm, not as pre-existing commitments.
- **What we did not assess:** We make no claim about the current site's speed, mobile behavior, conversion, or app configuration, because we could not measure them. Any such comparison would require the owner's analytics and access.

## 3. Business problems and opportunities

Framed as opportunities a purpose-built platform can address (not as defects of the current site):

- **Control & cost structure:** Reduce dependence on multiple paid apps by building needed capabilities into one owned system.
- **Owned data & analytics:** Query the PostgreSQL store of record directly for profitability, funnel, and retention insight.
- **Differentiated customer journey:** Tune filtering, pagination with preserved browse position, search, recommendations, and checkout to this specific catalog.
- **Operational efficiency:** Consolidate inventory alerts, reordering signals, fulfillment workflow, and reporting.
- **Automation & AI:** Add abandoned-cart recovery, authorized WhatsApp messaging, and AI-assisted catalog/admin tooling.

Each opportunity's value depends on the owner's current baseline, which must be measured first.

## 4. Our proposed solution

A single owned platform on **Angular + ASP.NET Core Web API + PostgreSQL**.

**Customer-facing feature set**
- Catalog with collections and server-driven **faceted filtering**.
- **Pagination that preserves browse position** on back-navigation.
- Rich product detail pages with media, variants, and stock.
- Persistent cart and **streamlined guest checkout**.
- Order confirmation emails, **order tracking**, and status notifications (email/WhatsApp, authorized).
- Relevance-tuned product search, wishlist.
- Smart recommendations, upsell/cross-sell, bundles (Phase 2).
- Abandoned-cart recovery and personalized promotions with privacy/consent controls (Phase 2).

**Admin-facing feature set**
- Revenue dashboard and sales overview.
- Product admin with bulk updates and import/export.
- Smart inventory with low-stock alerts and reorder signals.
- Order management and configurable fulfillment workflow.
- Discount management with performance analysis.
- Customer management with segmentation/retention views.
- Email template management, store branding config.
- Role-based permissions (RBAC) and audit logs.
- **AI-assisted admin:** product descriptions, tagging, category suggestions, natural-language reporting, and sales-trend explanations.
- Analytics: product profitability, conversion funnel, discount performance, returns/cancellation analysis.

## 5. Unique competitive advantages

- **Full ownership of data and experience** — no locked hosted checkout, no app-gated features.
- **Integrated AI-assisted admin** built into the product rather than bolted on via apps.
- **Owned analytics** against PostgreSQL — profitability, funnel, retention, discount ROI in one place.
- **Journey tuned to this catalog** — filtering, preserved-position pagination, recommendations.
- **No per-app fee sprawl** for the differentiated capabilities we build in.

These are stated as advantages of ownership and integration, **not** as claims that the reference site lacks equivalents.

## 6. Benefits for customers

- Faster, clearer discovery (faceted filters, tuned search).
- Less frustration browsing (pagination preserves position).
- Lower-friction, guest-friendly checkout.
- Transparency after purchase (confirmation, tracking, proactive notifications).
- Relevant recommendations and offers, with privacy respected.

## 7. Benefits for administrators

- One console for catalog, orders, inventory, discounts, customers, and reporting.
- Bulk operations and AI drafting reduce repetitive catalog work.
- Low-stock alerts and reorder signals reduce stockouts/overstock.
- RBAC and audit logs for safer, accountable operations.
- Natural-language reporting for non-analysts.

## 8. Benefits for business owners

- Direct visibility via an owned revenue dashboard and automated reports.
- Owned data for profitability and retention decisions.
- Differentiation that is the owner's asset, not a rented feature.
- Reduced dependence on multiple recurring app subscriptions (offset by hosting/maintenance — see §10).

## 9. Expected operational improvements (directional, baseline-first)

> **No guarantees.** Each item requires measuring the current baseline before improvement can be estimated. Direction only.

| Area | KPI | Direction (baseline-first) |
|---|---|---|
| Checkout | Checkout completion rate | Potential increase |
| Cart | Cart abandonment rate | Potential decrease |
| Conversion | Conversion rate | Potential increase |
| Order value | AOV | Potential increase (recommendations/bundles) |
| Retention | Repeat purchase rate | Potential increase |
| Operations | Order processing time | Potential decrease (automation) |
| Inventory | Inventory accuracy | Potential improvement (alerts) |
| Support | Support response time / WISMO volume | Potential decrease (tracking/notifications) |
| Marketing | Revenue per campaign | Potential improvement (segmentation) |
| Margin | Gross profit margin | Better decisions once cost data loaded |

See [Doc 11 — Measurable Business Outcomes](./11-competitive-gap-analysis.md) for problem/solution/effort/dependency/risk detail.

## 10. Cost and maintenance considerations (honest tradeoff)

A custom, self-hosted platform is **not automatically cheaper** than Shopify. The honest tradeoff:

| Dimension | Managed (Shopify-typical) | Our custom platform |
|---|---|---|
| Upfront cost | Low (theme/setup) | Higher (design + build) |
| Recurring platform fee | Monthly plan + app fees | No platform fee; hosting + maintenance instead |
| Checkout/PCI | Managed by platform | We integrate PCI-compliant providers; shared responsibility |
| Hosting/scaling | Managed | **Our responsibility** (infra, scaling) |
| Security/patching | Managed | **Our responsibility** (ongoing) |
| Feature extension | Install app (fee) | Build once (owned, but we maintain it) |
| Customization ceiling | Platform-limited | Effectively open |
| Data ownership | Platform-mediated | Full ownership |

**Bottom line:** migration makes sense when the value of control, differentiation, and owned data outweighs the ongoing cost and responsibility of running the platform. This is a business judgment for the owner; we present it as a tradeoff, not a foregone conclusion.

## 11. Migration strategy

A safe, authorized migration is detailed in **[Doc 12 — Website Migration Strategy](./12-website-migration-strategy.md)**. Summary:

- Authorized exports/integrations only; phased **export → transform → load → verify → parallel run → cutover → rollback**.
- **Passwords are never migrated in plaintext** — customers re-authenticate via forced reset / magic link / OAuth.
- **Payment credentials are never moved** — providers are re-onboarded.
- **SEO protected** via URL preservation and 301 redirects.
- Count + content reconciliation gates, a risk register, and a go/no-go checklist ensure **no loss of important data or organic traffic**.

## 12. Implementation roadmap

Mapped to the demonstration flows (§14).

**MVP (first release)**
- Catalog, collections, faceted filtering, pagination with preserved position.
- Product detail, cart, streamlined guest checkout.
- Order confirmation emails, first-party order tracking.
- Product search, bulk product updates.
- Revenue dashboard, RBAC.
- SEO URL preservation + 301 redirects, performance budgets.

**Phase 2 (follow-on)**
- Order status notifications (email/WhatsApp, authorized), wishlist.
- Smart recommendations, upsell/cross-sell, bundles.
- Abandoned-cart recovery, personalized promotions (privacy-controlled).
- Smart inventory/reordering, fulfillment workflow.
- Automated reporting; analytics (profitability, funnel, discount performance, returns, segmentation).
- Email template management, store branding config, audit logs.
- AI-assisted admin (descriptions, tagging, category suggestions, NL reporting, trend explanations).

Priorities align with [Doc 11 — main comparison table](./11-competitive-gap-analysis.md).

## 13. Risks and mitigations

| Risk | Mitigation |
|---|---|
| Migration data loss / mismatch | Reconciliation gates; see [Doc 12 risk register](./12-website-migration-strategy.md) |
| SEO/traffic loss | URL preservation + 301s; Search Console monitoring |
| Customer login friction at re-auth | Clear comms + magic-link fallback |
| Payment misconfiguration | Sandbox + small live tests in parallel run |
| Ongoing maintenance burden underestimated | Scope hosting/security/maintenance explicitly (see §10) |
| Benefits assumed rather than measured | Baseline-first KPIs; directional claims only |
| Build scope creep | MVP/Phase 2 split; confirm requirements (fresh-start assumption) |
| Cutover downtime | Low DNS TTL, rehearsed off-peak cutover, rollback path |

## 14. Recommended sales demonstration

A single scripted walkthrough that proves the value story end-to-end.

**Customer journey**
1. **Catalog** — open the storefront catalog.
2. **Category filter** — apply a category/faceted filter.
3. **Paginated browse with preserved position** — page through results, open an item, go back, and show the browse position restored.
4. **Product detail** — show media, variants, stock.
5. **Cart** — add to cart.
6. **Guest checkout** — complete a streamlined guest checkout.
7. **Order confirmation** — show the confirmation email/screen.
8. **Order tracking** — open the first-party tracking view.

**Admin journey**
9. **Sales overview** — revenue dashboard.
10. **Product admin** — edit a product; show bulk update + AI-assisted description/tagging.
11. **Inventory alerts** — show low-stock alert and reorder signal.
12. **Order management** — move an order through fulfillment statuses.
13. **Discount management** — create a discount; show performance view.
14. **Customer management** — show a customer and a segment.
15. **Email notifications** — show a transactional template and a triggered notification.
16. **Analytics** — show profitability, conversion funnel, and NL reporting.

The demo advantages are framed as capabilities we deliver, **without** asserting the existing site lacks them.

---

## Assumptions & open questions

- Confirm the source platform is Shopify (currently a **strong inference**).
- Confirm the fresh-start assumption: no prior agreed requirements; stack and scope set here.
- Confirm current baseline metrics — required before any benefit can be quantified.
- Confirm catalog/variant/order scale (drives roadmap and migration effort).
- Confirm acceptance of the ongoing hosting/maintenance/security responsibility (see §10).
- Confirm consent/privacy posture for personalized promotions and WhatsApp messaging.
- Confirm MVP vs Phase 2 scope and target timeline.
