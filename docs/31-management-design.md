# Management visual system

Platform operators and merchant management use the shared Matjark management shell and scoped `management-theme` styles. Public storefront themes remain independent.

- Platform navigation covers stores, merchant applications, plans, public website content and operations.
- Merchant management uses the same default colors, form controls, cards and tables, with the current store name in the header.
- Store provisioning groups store details and owner details separately.
- Plan editing uses two columns on desktop and one on mobile.
- Mobile navigation collapses; large tables scroll within their own region. Desktop sidebars scroll when needed.
- Arabic and English use the existing translation service and document direction.

Validation: production Angular build passed; localization audit found zero missing phrases across 719 template phrases. Browser checks covered platform overview/store form and merchant products on desktop, both mobile menus, and 390px viewport layouts (375px content width, no document overflow).

Preview images are in docs/previews: platform-management-desktop-ar.jpg, platform-create-store-ar.jpg, platform-management-mobile-ar.jpg, merchant-management-products-ar.jpg and merchant-management-mobile-ar.jpg.

This visual change does not alter authorization, subscription rules, order processing or product data.
