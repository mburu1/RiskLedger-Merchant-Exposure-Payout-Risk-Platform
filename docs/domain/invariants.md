# RiskLedger financial invariants

Status: normative domain contract for the simulation. Domain code and database constraints must enforce these rules; API/UI validation is only an additional boundary.

## 1. Money and currency

- Monetary arithmetic uses C# decimal; binary floating point is prohibited for money.
- Every amount carries an explicit supported currency. No implicit FX conversion is allowed.
- The initial supported-currency catalogue is KES, USD, EUR and GBP (2 minor-unit digits), JPY (0), and KWD (3). Extend it deliberately with tests and documentation.
- Constructor/input amounts with more fractional digits than the currency permits are rejected. They are not silently rounded.
- Calculated values that require rounding must call the explicit rounding operation using MidpointRounding.ToEven (banker's rounding).
- Addition and subtraction require identical currencies. FX requires a separately specified rate, source, timestamp, and balanced accounting treatment; conversion is never implicit.
- Amount scale is a currency property, not a universal two-decimal assumption.

## 2. Journal and posting

- A journal entry has at least two lines. Each line has exactly one positive debit or credit and both sides of a line use the same currency.
- For every currency represented in an entry, total debits equal total credits and are greater than zero. A balanced KES movement cannot offset an unbalanced USD movement.
- A posted entry is immutable through normal commands. Corrections are new compensating entries that preserve and reference the original.
- Every posting command requires a stable idempotency key. Persistence must enforce uniqueness within the defined tenant/accounting scope and atomically store the key with the journal entry.
- Posting and idempotency lookup must occur in the same database transaction. Concurrent requests with the same key return the original result; they must not create additional financial movements. Reuse of a key with a different canonical request payload must be rejected.
- Account ownership, currency restrictions, period status, and any tenant boundary must be checked before posting.
- The journal is the accounting source of truth. A cached balance is a projection, never a replacement for the journal.
- Production persistence must use a database transaction and appropriate unique constraints/locking or optimistic concurrency. The current in-memory adapter is development-only and cannot prove cross-process safety or restart recovery.

## 3. Funds and exposure vocabulary

These values are separate measures and must not be collapsed into one balance:

| Measure | Normative meaning |
| --- | --- |
| Gross supplier obligations | Sum of eligible recognized supplier liabilities before eligible coverage. Exclude cancelled/void obligations; handle disputed or contingent liabilities under a versioned rule. |
| Covered exposure | Portion of eligible gross obligations matched to qualifying, available funds explicitly allocated to those obligations. Each unit of funds may cover only one obligation at a time. |
| Uncovered exposure | max(0, gross eligible obligations - qualifying allocated coverage), calculated per currency unless a documented FX valuation is used. |
| Required reserve | Amount restricted by the active reserve policy for future obligations, disputes, refunds, or other named risk categories. It is a restriction on eligible funds, not an additional supplier liability. |
| Available payout balance | Settled, merchant-attributable funds less outstanding payout commitments, required reserves, explicit holds, and other policy restrictions. Pending funds are excluded until the authoritative settlement event is recorded. |

### Lifecycle treatment

- Pending inbound funds: not available for payout or obligation coverage unless a separately versioned policy explicitly defines a narrowly scoped use; default is excluded.
- Settled inbound funds: become eligible only after a verified settlement event is recorded and reconciled.
- Supplier obligations: recognized according to explicit lifecycle states and eligibility rules. A payment/settlement reduces the outstanding obligation and is itself recorded through the ledger.
- Reserves: allocations move funds from an unrestricted category to a restricted category; they do not create or destroy total funds. Release requires re-evaluation against current obligations and policy.
- Pending payouts: reserve/commit the requested amount before provider submission so parallel requests cannot spend the same funds.
- Successful payouts: settle the commitment through ledger postings based on an authoritative provider result.
- Declined payouts: release the commitment only after the decline is definitive.
- Timed-out/unknown payouts: remain in an explicit unknown/reconciliation state. Do not blindly retry a transfer or release its funds while the provider outcome is ambiguous.
- Currency conversion: never sum currencies as if equivalent. Any reporting-currency valuation needs a documented rate source, rate timestamp, valuation timestamp, and rounding rule.

The available payout calculation must be derived from authoritative ledger and lifecycle state, not from a client-provided amount or mutable UI field.

## 4. Risk decisions

Every assessment is an immutable historical record containing:
- assessment identifier and merchant/organizer reference;
- policy identifier and immutable policy version;
- canonical input snapshot or a durable reference plus content hash sufficient to reproduce the inputs;
- decision outcome (Approve, Review, or Restrict);
- stable reason codes and human-readable explanation;
- assessed timestamp and policy effective timestamp;
- actor/system identity, correlation identifier, and relevant source record versions.

Changing a policy creates a new version; it never rewrites an earlier decision. A decision must be deterministic for the same policy version and canonical inputs. Missing, stale, or contradictory evidence results in a safe documented outcome, not a guessed approval. Overrides require an authorized actor, reason, timestamp, before/after decision references, and audit event.

## 5. Payout eligibility

A low risk score is never sufficient to authorize a payout. Eligibility requires all applicable checks to pass:

1. Merchant is active and required verification is current and approved.
2. Currency is supported and requested amount is positive and valid at that currency's scale.
3. Authoritative settled funds cover the request after outstanding commitments, reserves, holds, and other restrictions.
4. No policy restriction, unresolved high-severity dispute, or mandatory manual-review hold blocks payout.
5. Risk assessment is current under the applicable policy and has an eligible outcome.
6. Payout lifecycle permits a new request; idempotency key is new or exactly matches the same canonical request.
7. Funds are committed atomically with request creation. Concurrent requests cannot overdraw the same available funds.
8. Provider submission uses a stable provider idempotency/reference key where supported. Ambiguous outcomes require lookup/reconciliation before retry.
9. Every state transition and override is auditable.

Eligibility is rechecked against authoritative state inside the same transaction that commits the payout reservation. A previous UI or API pre-check is not a reservation.

## 6. Audit and operational guarantees

Financially sensitive actions capture actor/system, action, target, timestamp, reason, correlation ID, and stable references to before/after state. Logs must not contain credentials, access tokens, bank secrets, or sensitive verification evidence. Duplicate/out-of-order events are deduplicated; reconciliation discrepancies remain visible and actionable.

## 7. Current implementation boundary

The current in-memory ledger demonstrates domain validation only. It is not durable accounting storage and does not provide multi-instance idempotency, transactional persistence, restart recovery, database-enforced immutability, or payout safety. Those guarantees remain blocked until PostgreSQL migrations, atomic repository operations, concurrency tests, and recovery workflows are implemented and verified.
