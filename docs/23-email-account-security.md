# Email notifications and account security

Customers can verify their email and reset forgotten passwords. Verification links expire after 24 hours; password reset links expire after 15 minutes. Tokens are single use, stored as hashes, bound to the store and current password, and removed from the browser address after opening. Resetting a password invalidates existing customer sessions. Existing customers may need to sign in again following this upgrade. Verification does not currently block checkout.

Merchants manage recipients and preferences at `/merchant/notifications`: customer order confirmations, customer status updates, and new-order messages to the merchant. SMTP credentials remain server configuration and are never returned by this API. A configured status means configuration is complete, not that delivery was verified.

## Connect Gmail locally on Windows

From the repository root in PowerShell 7:

```powershell
pwsh -NoProfile -File scripts/configure-gmail.ps1 -GmailAddress 'your-address@gmail.com'
```

Use your actual sender address. The script securely prompts for a Google App Password; do not paste it into chat. For another store add `-StoreId` with its registry ID. It stores credentials in ignored `src/Shop.Api/email-secrets.json`, restricts file access to the current Windows account and SYSTEM, and sends no test email. The running API reloads this configuration. Set the merchant recipient separately in the dashboard, then verify delivery with a controlled test order.

Google App Passwords require two-step verification and may be unavailable for some accounts: [Google account instructions](https://support.google.com/accounts/answer/185833?hl=EN). Gmail SMTP uses `smtp.gmail.com`, port 587 with STARTTLS: [Google SMTP documentation](https://developers.google.com/workspace/gmail/imap/imap-smtp). Use an App Password, not the normal account password.

Production deployments should inject protected configuration such as `Email__SmtpPassword`; each additional store uses its own `StoreEmail__STORE_ID__...` settings rather than inheriting the default store's sender. Other required fields are Enabled, SmtpHost, SmtpPort, SmtpUser and From. Set `StoreLinks:STORE_ID:Origin` to the registered store HTTPS origin for account links.

## Delivery limits and verification

Without SMTP configuration, development writes private files under `src/Shop.Api/store-data/STORE_ID/sent-emails`; production does not use this fallback. Message bodies and recovery tokens are not written to ordinary application logs.

Account and business emails now use an encrypted durable outbox in each store database. Orders, checkout receipts and notification rows commit together; order status and return updates also enqueue within their transaction. No SMTP connection is made during checkout. A worker opens an independent scope for each registered active or suspended store. Credentials are resolved afresh each cycle.

Delivery has five attempts per cycle, with delays of 30, 60, 120 and 240 seconds. Failed unexpired messages can be retried manually from `/merchant/notifications`; previous attempt history is retained. Account messages keep their token expiry; ordinary messages expire after two days. Expired payloads are removed. Delivery clears the body but retains recipient/subject/state and attempt metadata. A two-minute lease recovers interrupted sends. An attempt left as Started indicates interruption before its outcome was recorded.

SMTP acceptance is shown as Sent; it does not prove delivery to the recipient inbox. DevelopmentOnly explicitly means a private local file, never a successful real email. SMTP cannot guarantee exactly-once delivery after a crash between provider acceptance and database acknowledgement; a recovered attempt can produce a duplicate. Checkout replay does not create another outbox entry.

The payload key ring is under `store-data/email-keys`. Restrict filesystem access and preserve these keys with database backups; losing them makes queued payloads unreadable. Deployment encryption/access controls for key storage remain an operator responsibility. History APIs never return bodies, tokens or SMTP credentials. Real Gmail delivery has not been tested because sender credentials have not been supplied.

Run `tests/security-smoke.ps1` with `-AdditionalChecks tests/email-outbox-checks.ps1` for isolated SMTP failure, retry, restart and account checks. Failure simulation connects only to a closed loopback port. SQLite upgrades to version 8 with a backup before migration; PostgreSQL deployments must apply all migrations through OrderReturns (including DurableEmailOutbox).
