# ADR-0001: Outbox Processor Pattern

## Status
Accepted

## Context
- Outbox table stores domain events for reliable async publish.
- Need deterministic retry (exp backoff, max 3) and dead‑letter handling.
- Existing infrastructure provides MassTransit (`IBus`) and `ICQRSManager`.
- Must stay within Clean Architecture: Core.Infrastructure → depends only on Core.Domain.

## Decision
- Implement **Outbox Publish Pattern** using `IBus.Publish` via a new `IOutboxPublisher` service.
- Add `IOutboxRepository` methods `UpdateRetryAsync` and `MoveToDlqAsync`.
- Retry logic: exponential backoff (2^attempt seconds) up to 3 attempts, stored in `OutboxMessage.RetryCount` (new column).
- After max attempts, move message to **DLQ** table `OutboxDlqMessage` (same schema).
- Processing performed in `OutboxProcessorBackgroundService`.

## Consequences
### Positive
- Guarantees at‑least‑once delivery with bounded retries.
- DLQ enables manual inspection/replay.
- No new external dependencies.
### Negative
- Additional DB column & DLQ table require migration.
- Slight latency due to backoff delays.
### Risks & Mitigations
- Risk: DB lock contention – use batch size 50 and `FOR UPDATE SKIP LOCKED` in repo implementation.
- Risk: Message deserialization errors – catch `JsonException`, treat as failed and move to DLQ.

## Alternatives Considered
| Option | Reason Rejected |
|--------|-----------------|
| Fire‑and‑forget publish inside domain transaction | Breaks atomicity, risk of lost messages |
| Use external library (e.g., Hangfire) | Adds new dependency, violates constraint |

## Compliance
- CI check ensures `IOutboxRepository` resides in Core.Domain and implementations in Infrastructure.
- Unit tests verify retry count and DLQ flow.
