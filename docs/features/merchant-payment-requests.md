# Merchant Payment Requests

## Business Rules

### Merchant Registration & Lifecycle
- A merchant is an independent entity — not a regular user role
- Any user can submit a merchant registration request (self-service)
- A merchant must be approved by an admin before becoming active
- Admin approval triggers automatic creation of an `OpenIddict Application` for that merchant
- Each merchant gets a unique `client_id = merchant-{merchantId}` and a generated `client_secret`
- The `client_secret` is returned **once only** at approval time — it is never stored in plain text
- A merchant can be suspended at any time — this immediately disables their `OpenIddict Application` and revokes all active tokens
- Each merchant has exactly **one receiving wallet** — defined at registration time via `PhoneNumber`
- The receiving wallet's currency defines the currency of all payment requests from that merchant

### Payment Request Lifecycle
- Only an **Active** merchant (authenticated via `OAuth2 Client Credentials`) can create a payment request
- A merchant can have **only one pending payment request at a time** per customer phone number
- The customer phone number must resolve to an existing, active wallet
- The resolved wallet's currency must match the merchant's receiving wallet currency
- Self-payment is forbidden — the merchant's own wallet phone number cannot be the target
- A payment request expires automatically after **2 minutes** from creation
- Expiry is enforced by a `Hangfire` background job that runs on a schedule
- Once expired, the request status changes to `Expired` — it is never deleted
- A customer can **Approve** or **Reject** a pending (non-expired) request
- If the customer's balance is insufficient at approval time — the approval fails with a validation error and the request remains `Pending` until it expires
- All payment request records are append-only — statuses are updated, rows are never deleted

### Transfer on Approval
- Approval triggers a full transfer flow (same as the existing Transaction module Saga)
- The transfer uses the customer's wallet as source and the merchant's wallet as destination
- On successful transfer → request status becomes `Completed`
- On failed transfer (e.g. race condition, concurrency) → request status becomes `Failed`, customer is notified
- `IdempotencyKey` for the underlying transfer is derived from the `PaymentRequest.Id` — no duplicate transfers possible

### Webhook (Callback) on Resolution
- When a payment request is resolved (Completed / Rejected / Expired / Failed) — the system fires a webhook to the merchant's registered `CallbackUrl`
- Webhook payload is signed using `HMAC-SHA256` with the merchant's `WebhookSecret`
- The signature is sent in the `X-Webhook-Signature` header
- Retry policy: **10 attempts**, **2 minutes apart**, handled by `Hangfire`
- After 10 failed attempts → webhook status becomes `CallbackFailed`, stored in DB for audit
- The merchant verifies the signature on their side before processing the webhook

---

## Edge Cases

- **Customer not found**: phone number does not resolve to any wallet → `400 Bad Request`
- **Currency mismatch**: customer wallet currency ≠ merchant wallet currency → `400 Bad Request`
- **Duplicate pending request**: merchant already has a pending request for the same phone number → `409 Conflict`
- **Expired request approval attempt**: customer tries to approve/reject an expired request → `400 Bad Request`
- **Insufficient balance at approval**: transfer fails → request stays `Pending`, customer gets error response
- **Merchant wallet inactive**: merchant's receiving wallet is deactivated → `400 Bad Request` on request creation
- **Callback URL unreachable**: retry 10 times with 2-minute intervals → mark as `CallbackFailed`
- **Self-payment**: merchant's own wallet phone number used as customer target → `400 Bad Request`
- **Suspended merchant token**: any request with a token from a suspended merchant → `401 Unauthorized` (handled by OpenIddict)

---

## Modules Involved

- **Merchants Module** — merchant registration, approval, OpenIddict application management, webhook dispatch
- **Wallets Module** — phone number resolution, wallet currency lookup, balance check
- **Transactions Module** — executes the actual transfer Saga on approval
- **Notifications Module** — real-time SignalR notification to customer on new payment request + resolution
- **Identity Module** — admin role check for approval endpoint; OpenIddict application CRUD
- **BuildingBlocks** — Result pattern, domain events, outbox

---

## Database Schema

### Schema: `merchants`

```sql
merchants (
  id                  uniqueidentifier  PRIMARY KEY,
  business_name       nvarchar(200)     NOT NULL,
  owner_user_id       uniqueidentifier  NOT NULL,   -- FK → identity.users
  receiving_wallet_id uniqueidentifier  NOT NULL,   -- FK → wallets.wallets
  callback_url        nvarchar(2000)    NOT NULL,
  webhook_secret_hash nvarchar(500)     NOT NULL,   -- hashed, used for HMAC signing
  status              nvarchar(20)      NOT NULL,   -- Pending | Active | Suspended
  created_at          datetimeoffset    NOT NULL,
  approved_at         datetimeoffset    NULL,
  approved_by         uniqueidentifier  NULL        -- FK → identity.users (admin)
)
```

### Schema: `merchants.payment_requests`

```sql
payment_requests (
  id                      uniqueidentifier  PRIMARY KEY,
  merchant_id             uniqueidentifier  NOT NULL,   -- FK → merchants
  merchant_wallet_id      uniqueidentifier  NOT NULL,   -- snapshot at creation time
  customer_phone_number   nvarchar(20)      NOT NULL,
  customer_wallet_id      uniqueidentifier  NOT NULL,   -- resolved from phone number
  amount                  decimal(18,4)     NOT NULL,
  currency                nvarchar(10)      NOT NULL,
  status                  nvarchar(20)      NOT NULL,   -- Pending | Approved | Rejected | Expired | Completed | Failed
  idempotency_key         uniqueidentifier  NOT NULL    UNIQUE,
  expires_at              datetimeoffset    NOT NULL,   -- created_at + 2 minutes
  resolved_at             datetimeoffset    NULL,
  failure_reason          nvarchar(500)     NULL,
  created_at              datetimeoffset    NOT NULL
)
```

### Schema: `merchants.webhook_deliveries`

```sql
webhook_deliveries (
  id                  uniqueidentifier  PRIMARY KEY,
  payment_request_id  uniqueidentifier  NOT NULL,
  merchant_id         uniqueidentifier  NOT NULL,
  attempt_number      int               NOT NULL,
  status              nvarchar(20)      NOT NULL,   -- Pending | Delivered | Failed | CallbackFailed
  hangfire_job_id     nvarchar(100)     NULL,
  response_status     int               NULL,       -- HTTP status code from merchant
  error_message       nvarchar(1000)    NULL,
  attempted_at        datetimeoffset    NULL,
  next_retry_at       datetimeoffset    NULL,
  created_at          datetimeoffset    NOT NULL
)
```

---

## API Endpoints

### Merchant-facing (authenticated via `Client Credentials` token)

```
POST   /api/v1/payment-requests
GET    /api/v1/payment-requests/{id}
```

### Customer-facing (authenticated via standard JWT)

```
GET    /api/v1/payment-requests/pending          ← list of pending requests for the customer
POST   /api/v1/payment-requests/{id}/approve
POST   /api/v1/payment-requests/{id}/reject
```

### Admin-facing (authenticated via JWT + Admin role)

```
POST   /api/v1/merchants                         ← register merchant (or self-service)
GET    /api/v1/merchants/{id}
PATCH  /api/v1/merchants/{id}/approve            ← triggers OpenIddict Application creation
PATCH  /api/v1/merchants/{id}/suspend
```

---

## Key Decisions

| Decision | Choice | Reason |
|---|---|---|
| Merchant identity | Independent `Entity` in `Merchants Module` | Needs `CallbackUrl`, `WebhookSecret`, `OpenIddict` app — too complex for a role |
| Auth flow | `OAuth2 Client Credentials` via `OpenIddict` | Industry standard, full revocation control |
| OpenIddict apps | One `Application` per merchant | Instant per-merchant revocation, isolated secrets |
| `client_secret` storage | Hashed in DB, returned plain once at approval | Security standard — no plain text secrets at rest |
| Webhook security | `HMAC-SHA256` signature in `X-Webhook-Signature` | Merchant can verify authenticity without sharing a password |
| Webhook retry | 10 attempts × 2-minute intervals via `Hangfire` | Money-critical — must not be lost silently |
| Expiry enforcement | `Hangfire` scheduled job | Reliable, survives process restarts |
| Transfer on approval | Reuses existing `Transactions Module` Saga | No duplicate logic — single source of truth |
| `IdempotencyKey` for transfer | Derived from `PaymentRequest.Id` | Guarantees exactly-one transfer per approval |
| Currency enforcement | Derived from merchant's receiving wallet | No cross-currency transfers, consistent with system rules |
| Request uniqueness | One pending request per merchant per customer phone | Prevents flooding the customer with requests |
| Notification channel | `SignalR` for web, `Push Notification` deferred for mobile | Incremental delivery — web first |
| Records policy | Append-only, never deleted | Full audit trail |

---

## Flow Diagrams

### Payment Request Creation & Approval

```
Merchant                      System                         Customer
   │                             │                               │
   │── POST /payment-requests ──▶│                               │
   │                             │── resolve PhoneNumber         │
   │                             │── validate currency match     │
   │                             │── check no pending duplicate  │
   │                             │── create PaymentRequest       │
   │                             │── schedule Expiry Job (2 min) │
   │                             │──── SignalR notification ────▶│
   │◀── 201 Created ─────────────│                               │
   │                             │                  sees request │
   │                             │◀──── POST /approve ──────────│
   │                             │── run Transfer Saga           │
   │                             │── update status → Completed   │
   │                             │── dispatch Webhook Job        │
   │                             │──── SignalR notification ────▶│
   │◀──── Webhook Callback ──────│                               │
```

### Webhook Retry Flow

```
Hangfire Job
   │
   │── POST {CallbackUrl} ──▶ Merchant Server
   │                                │
   │◀── timeout / 5xx ─────────────│
   │
   │── wait 2 min ── retry #2 ──▶ Merchant Server
   │                                │
   │◀── timeout / 5xx ─────────────│
   │
   │── ... (up to 10 attempts)
   │
   └── mark webhook_deliveries.status = CallbackFailed
```

---

## Webhook Payload Structure

```json
{
  "eventType": "payment_request.completed",
  "paymentRequestId": "...",
  "merchantId": "...",
  "amount": 150.00,
  "currency": "EGP",
  "customerPhoneNumber": "+201XXXXXXXXX",
  "resolvedAt": "2026-05-09T10:00:00Z",
  "status": "Completed"
}
```

Header sent with every webhook:
```
X-Webhook-Signature: sha256={HMAC-SHA256(payload, webhookSecret)}
X-Webhook-Timestamp: {ISO8601 timestamp}
```
