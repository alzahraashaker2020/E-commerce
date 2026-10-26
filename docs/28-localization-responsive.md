# Arabic, English and responsive storefront

The customer storefront, customer account/recovery/returns pages, merchant console and platform console now offer Arabic and English. The language switch saves a per-host preference, updates document language/direction and formats dates and currency with the selected locale. Switching languages does not reload forms, modify API identifiers or translate customer names, contact/address information, review comments, SKUs or order references.

## UI and merchant content

UI messages live in `client/src/app/core/i18n/ar.ts`, generated from the reviewed `scripts/localization/ar-ui.tsv` dictionary. The Arabic dictionary is loaded on demand; the English startup bundle does not include it. Noto Sans Arabic is served locally from `client/public/fonts` with its OFL license, sourced from the official Google Fonts repository: https://github.com/google/fonts/tree/main/ofl/notosansarabic . No font service is called by the running storefront.

Merchant-owned translations are managed at `/merchant/languages` (**Languages & translations / اللغات والترجمة**). The editor discovers brand/home copy, collection links, banners, product names/descriptions/brands, size and color labels, delivery locations and order status names from this store. Keep the English source under Products / Storefront & design and add Arabic here. Description previews and storefront descriptions use Angular's normal HTML sanitization.

Translations are saved in the existing versioned storefront document, so no new database migration is required. Concurrent stale saves return 409. Source phrases are matched exactly and identical phrases share a translation. Editing an English source requires translating the new phrase; otherwise the original text is displayed. This is an editable translation catalog, not automatic machine translation. User reviews and entered addresses retain their original language.

Public translation output is filtered to publicly referenced storefront content and active products/labels. Merchant discovery includes draft product text, but public responses exclude private inactive-product translations and unreferenced phrases. Dictionaries and discovery remain store-isolated. Arabic catalog search matches translated product/brand phrases while retaining canonical English product data and identifiers in the API. Composed cart names translate their product and size/color parts for display; order snapshots remain unchanged.

The Little Days demo includes 106 editable Arabic phrases covering its current discovered content. Source translations for rebuilding the demo are in `scripts/demo-data/kids-arabic.json`; the setup script preserves already populated stores and merchant edits.

## Email behavior

The client sends `Accept-Language`. Newly composed verification/reset and order messages use that request's supported language, with English fallback; store translations can supply product/status names. A queued message retains the language and text originally composed through retries. This does not add a separate persisted customer language preference for later merchant-initiated status messages: those follow the language of the initiating action. Existing messages are not rewritten. Actual delivery still requires the private sender/SMTP configuration; development copies are not proof of inbox delivery.

## Responsive behavior

The kids phone header has a hamburger menu with keyboard focus handling and Escape/overlay closing, a search field beside the language switch, and a two-by-two promotion strip with subtitles. Clothing cards use four columns for photo categories and two columns for product cards on phones. Tablet/desktop layouts retain their larger grids. RTL flips layout and carousel controls. The merchant navigation collapses on phones, and forms/tables accommodate narrow screens without changing their data/actions.

Browser inspection verified English/Arabic, saved language on reload, menu navigation, translated cart/variant names, merchant login/editor, and no horizontal page overflow at 320, 390, 768 and 1280 viewport widths. Temporary viewport overrides were reset after verification. Preview artifacts are under `docs/previews/`.

## Validation

- API and Angular production builds succeeded; no new runtime dependency was installed.
- `node scripts/localization/check-coverage.mjs`: 577 template phrases audited, none missing from the UI dictionary (excluding protocol/date-format/language-choice literals).
- Localization/content/product/stock/shipping suite: 433 HTTP checks, including Arabic source persistence/search, private-draft filtering and queued Arabic verification subjects and complete Arabic order/variant messages.
- PostgreSQL: 33 schema checks and 318 tenant/permissions checks, including translation isolation.
- Email/security/outbox suite: 648 HTTP checks, covering encrypted persistence, SMTP failure, restart and retry behavior.
- Deployment profiles: 396 HTTP checks, including standalone, catalog, SaaS restoration and fresh startup.

These suites overlap; their counts are not a combined total. Local demo data remains 16 kids products and 15 original-store products. The deployment remains SaaS + Commerce. Other merchants' business copy needs their own Arabic translations through the editor.
