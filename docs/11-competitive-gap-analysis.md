# 11 — Competitive Gap Analysis

> **Companion documents:** [12 — Website Migration Strategy](./12-website-migration-strategy.md) · [13 — Commercial Value Proposition](./13-commercial-value-proposition.md)

## Purpose

This document compares the store owner's existing e-commerce site against the platform we propose to build (Angular frontend, ASP.NET Core Web API backend, PostgreSQL database). Its goal is to identify where a purpose-built platform can add differentiated business value, and to give the owner an honest, evidence-labeled basis for a migration decision.

It deliberately separates what we have **verified** from what we **infer or assume**, and it avoids claiming the reference site "lacks" capabilities we could not observe.

---

## Research methodology and limitations

**Reference site:** `https://makani-group.com/collections/all`

- **Crawl limitation (Verified):** The reference URL could not be crawled during research. Automated fetches returned **HTTP 429 Too Many Requests**. As a result, we hold **no verified live DOM data** — no observed product counts, prices, page structure, script bundles, or checkout behavior. Any statement about specific on-page content would be fabrication, so none is made here.
- **Platform basis (Inference, strong):** Public search results indicate the MAKANI brand runs on **Shopify**. Two signals support this: (a) the `/collections/all` route is Shopify's standard collection URL pattern, and (b) a development agency's public portfolio lists a MAKANI Shopify storefront. We therefore treat *"the reference site is a Shopify store"* as a **strong inference**, explicitly labeled — not an observed fact.
- **Comparison framing (constraint):** Because Shopify provides a large, mature feature set out of the box, this analysis compares **what a typical Shopify store provides** against **what our platform adds or differentiates**. We do **not** assert that the reference site is missing a feature merely because it is not publicly visible or could not be crawled.
- **Evidence labels:** Every competitive claim below is tagged `(Verified)`, `(Inference)`, or `(Assumption)`.
  - `(Verified)` — supported by public evidence or by the nature of our own platform (which we control).
  - `(Inference)` — reasoned from the Shopify basis and the URL pattern.
  - `(Assumption)` — a working premise that still needs the owner's confirmation.

### Verified findings

- The reference URL uses Shopify's conventional `/collections/all` path. **(Verified)**
- The reference URL returned HTTP 429 to automated fetches, blocking data collection. **(Verified)**
- Shopify is a mature, managed SaaS commerce platform with a broad native feature set and a large app ecosystem. **(Verified, general platform knowledge)**
- Our platform's capabilities are fully under our control and can be specified exactly, because we are building them. **(Verified)**

### Assumptions / Inferences

- The reference site is a Shopify store. **(Inference, strong)**
- The store sells a physical-goods catalog with collections, product detail pages, cart, and Shopify-hosted checkout — the standard Shopify customer journey. **(Inference)**
- The owner currently relies on Shopify's native admin plus some apps for operations, marketing, and reporting. **(Assumption)**
- The project workspace was empty at the start of this engagement, so there were no "previously agreed requirements." The committed stack (Angular + ASP.NET Core Web API + PostgreSQL) and the requirement set are being **established fresh** in these documents. **(Assumption — see [Doc 13 §2](./13-commercial-value-proposition.md).)**

---

## Competitive analysis by axis

Each axis is framed as *Shopify-typical capability* vs *our platform's differentiation*. No negative claim is made about the reference site that we could not verify.

### Existing business functionality
- **Shopify-typical (Inference):** Catalog, collections, product pages, cart, hosted checkout, basic discounts, order management, native analytics, and third-party apps for extended needs.
- **Our differentiation (Verified — we build it):** The same core journey, plus deep customization of data model, workflows, and AI-assisted admin without per-app subscription sprawl or platform fee structures.

### Strengths of the existing setup
- **Shopify-typical (Verified, general):** Fast time-to-market, PCI-compliant hosted checkout, reliable managed hosting, strong uptime, mature payment/shipping integrations, and a large app marketplace. These are genuine strengths and are acknowledged honestly; migration must clear a high bar to be worthwhile.

### Weaknesses / constraints to weigh honestly
- **Framed as tradeoffs, not defects (Inference):** SaaS platforms impose recurring platform/app fees, limits on checkout and data-model customization (outside enterprise tiers), and reliance on third-party apps whose behavior and pricing the merchant does not control. These are structural tradeoffs of the managed model, not observed faults of this specific store.

### Customer experience limitations
- **Framing:** We do not claim the reference CX is poor. Our differentiator is **control over the end-to-end experience** — e.g., pagination that preserves scroll/browse position, tightly integrated search and recommendations, and a unified wishlist — tuned to this catalog rather than to a generic theme. **(Our capability: Verified; competitor state: not asserted.)**

### Administrative limitations
- **Framing (Inference):** Shopify admin is capable; extending it typically means installing and paying for apps. Our platform can build role-based admin, audit logs, bulk operations, and AI-assisted tooling directly into the product. **(Our capability: Verified.)**

### Operational inefficiencies
- **Framing:** Opportunity to consolidate inventory alerts, reordering signals, shipping workflow, and reporting into one owned system rather than several apps. We target reduced manual steps; **the current baseline must be measured before any time-savings estimate.** **(Directional.)**

### Missing automation opportunities
- **Framing:** Positioned as *automation we will add* (abandoned-cart recovery, low-stock alerts, automated reporting, AI content generation), **not** as automation the competitor lacks. **(Our capability: Verified; competitor state: not asserted.)**

### Reporting / analytics gaps
- **Framing:** Our platform can expose product profitability, conversion funnel, discount performance, and returns analysis directly against the PostgreSQL store of record. Whether the current setup surfaces these depends on its apps, which we did not inspect. **(Our capability: Verified; competitor state: not asserted.)**

### Marketing limitations
- **Framing:** We will add personalized promotions (with privacy controls) and authorized WhatsApp Business messaging. We make no claim about the reference site's current marketing stack. **(Our capability: Verified.)**

### Mobile experience
- **Framing:** Angular lets us build and measure a responsive, performance-budgeted mobile experience. We did not observe the reference site's mobile behavior (429), so we assert nothing about it. **(Our capability: Verified; competitor state: not asserted.)**

### Checkout friction
- **Framing:** Shopify's hosted checkout is strong and PCI-compliant. Our differentiator is the **freedom to design** guest checkout and reduce steps/fields to fit this business, subject to measuring the current completion rate first. **(Directional; competitor state: not asserted.)**

### Order tracking
- **Framing:** We will provide first-party order status and tracking with notifications. Shopify also supports tracking natively; our value is integration depth and owned data, not a claim of absence. **(Our capability: Verified.)**

### SEO & performance
- **Framing:** Migration risk is real: we must **preserve indexed URLs and add 301 redirects** to avoid traffic loss (see [Doc 12](./12-website-migration-strategy.md)). Our platform can meet explicit performance budgets. We do not claim the current site is slow or poorly optimized — we did not measure it. **(Our capability: Verified; competitor state: not asserted.)**

---

## Main comparison table

**Legend:** Priority = **MVP** (first release) or **Phase 2** (follow-on). "Reference Website (Shopify)" entries are **Inference** unless noted; "Our Platform" entries are capabilities we commit to build **(Verified — under our control)**.

| Feature | Reference Website (Shopify) | Our Platform | Business Value | Priority (MVP / Phase 2) |
|---|---|---|---|---|
| Catalog & collections | Native collections (Inference) | First-party catalog on PostgreSQL, fully modeled | Owned data model, flexible merchandising | MVP |
| Category filtering | Theme/app filtering (Inference) | Server-driven faceted filters tuned to catalog | Faster product discovery | MVP |
| Pagination with preserved browse position | Theme-dependent (Inference) | Pagination that restores scroll/browse position on back-nav | Lower friction, less rage-quit | MVP |
| Product detail page | Native PDP (Inference) | Custom PDP (media, variants, stock, cross-sell slots) | Higher engagement/conversion potential | MVP |
| Shopping cart | Native cart (Inference) | First-party cart with persistence | Reliable, customizable journey | MVP |
| Guest checkout | Supported natively (Inference) | Streamlined guest checkout, minimal fields | Reduce abandonment (baseline first) | MVP |
| Checkout friction reduction | Hosted checkout, strong (Inference) | Configurable step/field reduction + validation | Higher completion (baseline first) | MVP |
| Order confirmation emails | Native (Inference) | Templated transactional email on order placement | Trust, fewer "did it work?" tickets | MVP |
| Order status notifications | Native/app (Inference) | Event-driven status notifications (email/WhatsApp) | Proactive comms, fewer WISMO tickets | Phase 2 |
| Order tracking | Native/app (Inference) | First-party tracking page tied to order events | Transparency, retention | MVP |
| Wishlist | App-provided (Inference) | Built-in wishlist for logged-in users | Repeat visits, intent capture | Phase 2 |
| Product search | Native/app (Inference) | Relevance-tuned search over catalog | Discovery, conversion | MVP |
| Smart recommendations | App-provided (Inference) | Recommendation slots (related/also-bought) | AOV uplift (baseline first) | Phase 2 |
| Upsell / cross-sell | App-provided (Inference) | Rule-based upsell/cross-sell placements | AOV uplift (baseline first) | Phase 2 |
| Product bundles | App-provided (Inference) | Native bundle definitions & pricing | Higher AOV, inventory movement | Phase 2 |
| Abandoned cart recovery | App-provided (Inference) | Automated recovery (email/WhatsApp, authorized) | Recover lost orders (baseline first) | Phase 2 |
| Personalized promotions (privacy-controlled) | App-provided (Inference) | Segment-based promos with consent/privacy controls | Targeted uplift, compliant | Phase 2 |
| WhatsApp Business integration (authorized only) | App-provided (Inference) | Opt-in, authorized WhatsApp messaging | Direct, high-open-rate comms | Phase 2 |
| Smart inventory & reordering / low-stock alerts | App-provided (Inference) | Low-stock thresholds + reorder signals | Fewer stockouts, less overstock | Phase 2 |
| Bulk product updates | CSV/app (Inference) | Bulk edit UI + import/export | Admin time savings (baseline first) | MVP |
| Shipping workflow | Native/app (Inference) | Configurable fulfillment workflow & statuses | Faster, consistent fulfillment | Phase 2 |
| Automated reporting | Native/app (Inference) | Scheduled report generation & delivery | Owner visibility without manual pulls | Phase 2 |
| Revenue dashboard | Native analytics (Inference) | Real-time revenue dashboard on owned data | At-a-glance performance | MVP |
| Product profitability | App/manual (Inference) | Margin analysis using cost + price data | Focus on profitable SKUs | Phase 2 |
| Sales trends | Native analytics (Inference) | Trend views with AI-assisted explanations | Faster insight, planning | Phase 2 |
| Conversion funnel | App-provided (Inference) | Funnel from view → cart → checkout → order | Pinpoint drop-off | Phase 2 |
| Customer retention / segmentation | App-provided (Inference) | RFM-style segments for targeting | Retention, repeat purchase | Phase 2 |
| Discount performance | Native/app (Inference) | Discount ROI & redemption analysis | Stop unprofitable promos | Phase 2 |
| Returns / cancellation analysis | App/manual (Inference) | Return & cancellation reporting | Reduce leakage, spot issues | Phase 2 |
| Role-based permissions | Tier/app-dependent (Inference) | Built-in RBAC for staff roles | Least-privilege, safer admin | MVP |
| Store branding config | Theme editor (Inference) | Admin-managed branding/config | Self-serve brand control | Phase 2 |
| Email template management | Native/app (Inference) | Admin-editable transactional templates | Consistent, on-brand comms | Phase 2 |
| Audit logs | Tier/app-dependent (Inference) | First-party audit trail of admin actions | Accountability, compliance | Phase 2 |
| AI-assisted admin (descriptions, tagging, category suggestions) | App-provided (Inference) | Built-in AI drafting for product content | Faster catalog ops | Phase 2 |
| AI NL reporting & sales-trend explanations | App-provided (Inference) | Natural-language queries + trend narratives | Insight for non-analysts | Phase 2 |
| SEO URL preservation & 301 redirects | Native (Inference) | Redirect map + canonical URLs at migration | Protect organic traffic | MVP |
| Performance (budgets) | Platform-managed (Inference) | Explicit performance budgets, measured | Speed → conversion/SEO | MVP |

---

## Measurable Business Outcomes

For each high-value improvement: the business problem, proposed solution, expected benefit (directional — a baseline must be measured before any improvement can be estimated), KPI, implementation effort, dependencies, and risks.

> **Baseline rule:** No figure below is a guarantee. Each requires measuring the current baseline on the existing platform (where possible) or on launch before improvement can be quantified.

| Improvement | Business problem | Proposed solution | Expected benefit (directional, baseline-first) | KPI | Effort | Dependencies | Risks |
|---|---|---|---|---|---|---|---|
| Checkout friction reduction | Potential drop-off at checkout | Streamlined guest checkout, fewer fields | Possible lift in completed checkouts once baseline is known | Checkout completion rate | Medium | Payment provider onboarding | Payment edge cases; measure before/after |
| Guest checkout | Forced-account friction | One-step guest path | Possible reduction in abandonment | Cart abandonment rate | Low–Medium | Checkout build | Fraud controls still required |
| Smart recommendations | Flat AOV | Related/also-bought slots | Possible AOV uplift | Average Order Value (AOV) | Medium | Catalog + order data | Cold-start quality |
| Abandoned cart recovery | Lost in-progress orders | Automated authorized recovery messages | Possible recovery of a share of abandoned carts | Cart abandonment / recovered revenue | Medium | Email/WhatsApp, consent | Message fatigue; consent compliance |
| Order status notifications | WISMO support load | Event-driven notifications | Possible reduction in support contacts | Support response time / ticket volume | Medium | Order events, comms channel | Over-messaging |
| Low-stock alerts & reorder signals | Stockouts / overstock | Threshold alerts + reorder view | Possible improvement in stock accuracy | Inventory accuracy | Medium | Inventory data model | Threshold tuning |
| Automated reporting | Manual report pulls | Scheduled reports | Possible reduction in order/report processing time | Order processing time | Low–Medium | Reporting module | Report correctness validation |
| Personalized promotions | Generic discounting | Segment-based promos | Possible improvement in campaign efficiency | Revenue per campaign | Medium | Segmentation, consent | Privacy compliance |
| Product profitability view | Promo/mix erodes margin | Margin analytics | Better margin decisions once cost data loaded | Gross profit margin | Medium | Cost price data | Data completeness |
| Retention / segmentation | Low repeat rate | RFM segments + targeting | Possible repeat-purchase improvement | Repeat purchase rate | Medium | Order history | Attribution clarity |
| Performance budgets | Speed affects conversion/SEO | Measured budgets in Angular | Possible conversion/SEO benefit | Conversion rate / Core Web Vitals | Medium | Front-end build | Regression discipline needed |

KPI set in scope: conversion rate, cart abandonment, checkout completion, AOV, repeat purchase rate, order processing time, inventory accuracy, support response time, revenue per campaign, gross profit margin.

---

## Top 5 improvements most likely to persuade the owner to migrate

Ranked on seven lenses: **business value, customer impact, competitive differentiation, implementation complexity, maintenance cost, time to deliver, measurability.**

### 1. Streamlined guest checkout + checkout friction reduction
- **Business value:** High — checkout is nearest to revenue. **Customer impact:** High — fewer steps. **Differentiation:** High — we control the full flow, not a locked hosted checkout. **Complexity:** Medium. **Maintenance:** Medium (payment edge cases). **Time to deliver:** MVP. **Measurability:** Strong (checkout completion, abandonment). **Why it wins:** Directly tied to money, demoable, and measurable from day one.

### 2. First-party order tracking + status notifications
- **Business value:** High (cuts WISMO support load). **Customer impact:** High (transparency builds trust). **Differentiation:** Medium–High (owned, integrated). **Complexity:** Medium. **Maintenance:** Low–Medium. **Time to deliver:** MVP (tracking) + Phase 2 (notifications). **Measurability:** Strong (support response time, ticket volume). **Why it wins:** Visible customer benefit plus a tangible operational saving.

### 3. Owned revenue dashboard + automated reporting
- **Business value:** High for the owner personally. **Customer impact:** Indirect. **Differentiation:** Medium (data is ours, no app fees). **Complexity:** Medium. **Maintenance:** Low–Medium. **Time to deliver:** MVP (dashboard) + Phase 2 (automation). **Measurability:** Strong (order processing time). **Why it wins:** Speaks straight to the decision-maker's daily visibility and removes recurring manual effort.

### 4. AI-assisted admin (descriptions, tagging, NL reporting)
- **Business value:** Medium–High (catalog-ops time savings). **Customer impact:** Indirect (better content). **Differentiation:** High (a standout demo moment). **Complexity:** Medium. **Maintenance:** Medium. **Time to deliver:** Phase 2. **Measurability:** Medium (admin time, content throughput). **Why it wins:** Memorable differentiator that lowers the owner's day-to-day workload.

### 5. Smart recommendations + abandoned cart recovery
- **Business value:** High (AOV + recovered revenue). **Customer impact:** Medium–High. **Differentiation:** Medium (Shopify apps also do this). **Complexity:** Medium. **Maintenance:** Medium. **Time to deliver:** Phase 2. **Measurability:** Strong (AOV, recovered revenue). **Why it wins:** Clear revenue upside measured against a known baseline.

**Why these five:** They cluster around revenue and the owner's own workload — the two levers a store owner feels most — while each is demoable and measurable. Items 1–3 are largely MVP-deliverable, giving early proof; 4–5 supply differentiation and upside in Phase 2. All comparisons are framed as additions we commit to build, not as deficiencies claimed about the reference site.

---

## Assumptions & open questions

- Confirm the reference site is Shopify (currently a **strong inference**, not observed).
- Confirm which Shopify apps/integrations are in use today, so comparisons reflect reality rather than defaults.
- Confirm current baseline metrics (conversion, abandonment, AOV, support volume) — required before any benefit can be quantified.
- Confirm catalog size, variant complexity, and historical order volume (drives migration effort — see [Doc 12](./12-website-migration-strategy.md)).
- Confirm consent/privacy posture for personalized promotions and WhatsApp messaging.
- Confirm the fresh-start assumption: there were no prior agreed requirements; the stack and scope are established in these documents.
