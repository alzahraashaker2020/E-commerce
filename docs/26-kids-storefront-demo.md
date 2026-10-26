# Kids clothing storefront demo

The Little Days demo uses `kids.localhost` and its own `KidsClothingDemo` connection / `shop_kids_demo.db`. The original localhost store retains its classic theme and data. Women's homewear remains a separate future demo.

## Open locally

- Store: http://kids.localhost:4300/
- Merchant login: http://kids.localhost:4300/merchant/login
- Design editor: http://kids.localhost:4300/merchant/storefront
- Private merchant credentials: `src/Shop.Api/obj/kids-demo-access.json` (ignored, owner-restricted; do not commit).

Start the API in Development on port 5000. Start the UI from `client` with `npx ng serve --host 127.0.0.1 --port 4300 --allowed-hosts kids.localhost`. Port 4300 avoids another local application on IPv4 port 4200. The API's Development configuration permits the demo origin.

The demo contains 16 clothing products, six variants per product, uploaded product galleries, age/type collections, two campaign banners and three curated product selections. It has a 90-day trial demo subscription, not a commercial pricing package. Sample stock, prices and Cairo delivery charges are editable.

## Merchant management

`Storefront & design` manages the theme, brand name/subtitle/color, announcement/delivery text, promotion strip, navigation, photo cards, age cards, banners and their colors/links, featured product selections, additional product collections, compare-at prices, badges, service highlights, support details, footer and newsletter headings. Items can be added, removed and reordered. Images use the existing store-scoped upload service. Concurrent stale saves return 409 instead of overwriting another edit.

Products, prices, sizes, colors, stock and galleries remain managed under Products and the existing configuration screens. Displayed currency follows store settings. Compare-at prices describe savings; the actual payable price remains the product/variant price. Collection assignments support both an age group and a clothing type for the same product.

Newsletter signup records consented addresses privately for this merchant. It does not send marketing campaigns. Do not describe the demo as having verified Google reviews, real branches or a reference-brand affiliation.

## Reproduce demo data

Run `pwsh -File scripts/setup-kids-demo.ps1` after the API is available and the private local platform access file exists. The script creates only the dedicated demo store and trial, downloads image assets, uploads them through the normal image validation service and seeds sample clothing. An already populated kids-themed store is preserved, including merchant edits. The script is resumable after partial setup; it is not a reset command.

The visual reference and temporary demo photography come from [OurKids](https://shop-ourkids.com/) and the supplied screenshots. Product source URLs are recorded in `scripts/demo-data/kids-products.json`; the downloaded-to-uploaded asset mapping is retained privately in `src/Shop.Api/obj/kids-demo-image-sources.json`. Replace reference photography with merchant-owned/licensed assets before any public launch. Little Days is an editable demo identity.

## Data and verification

SQLite schema version 9 and PostgreSQL migration `20261026050000_ManagedStorefront` add versioned store content and newsletter subscribers. Store content, product feeds, uploaded image paths and subscriber lists are isolated by the resolved store database and merchant authorization.

Validated: Angular production build, API build, 402 storefront/product HTTP checks, 24 SQLite schema assertions, 33 PostgreSQL migration assertions and 315 PostgreSQL tenant/permissions HTTP checks. Browser inspection verified the home page, uploaded clothing imagery, size availability and the 10-product offers collection. The original local store still contains 15 products.
