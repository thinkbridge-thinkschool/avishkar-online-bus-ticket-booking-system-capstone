# ADR-001: Seat Reservation Locking Strategy

| Field       | Value |
|-------------|-------|
| **Date**    | 2026-06-22 |
| **Status**  | Accepted |
| **Deciders** | Avishkar Patil |
| **Supersedes** | — |

---

## Context

The core booking flow requires a seat to move through three states before a booking is fully committed:

```
Available → Reserved (seat held for user, 10-min TTL)
          → Booked   (payment / confirm step completes)
          → Released (TTL expired or booking cancelled)
```

Two correctness requirements drive this decision:

1. **No double-booking.** Two concurrent users must not both successfully book the same seat on the same schedule.
2. **No dead-hold.** An abandoned reservation must not block a seat indefinitely; it must return to Available automatically.

The system is a single Azure App Service (B1/B2), backed by Azure SQL, with no Redis Cache or external coordination service in the current infrastructure stack. App Service can scale to multiple instances under load.

### What the existing code does

`Schedule.ReserveSeats(seatNumbers)` — called inside `CreateBookingHandler`:

```csharp
// 1. Load schedule + seats (EF query)
var schedule = await scheduleRepo.GetByIdWithSeatsAsync(scheduleId, ct);

// 2. Reserve in memory — sets Seat.Status = Reserved, Seat.LockedAt = UtcNow
var seatPrices = schedule.ReserveSeats(command.Seats.Select(s => s.SeatNumber).ToList());

// 3. Create Booking aggregate
var booking = Booking.Create(command.UserId, command.UserEmail, command.ScheduleId, bookedSeats);

// 4. Persist — EF RowVersion on Seat catches concurrent writes
await scheduleRepo.SaveChangesAsync(ct);  // throws DbUpdateConcurrencyException on conflict
await bookingRepo.AddAsync(booking, ct);
await bookingRepo.SaveChangesAsync(ct);

// 5. Publish domain event to Service Bus
await publisher.PublishAsync(new BookingConfirmedEvent(...), ct);
```

**Seat entity concurrency token:**
```csharp
public byte[] RowVersion { get; private set; } = [];  // [Timestamp] in EF config
```

**Background expiry service (`SeatExpiryService`):**
```csharp
// Every 5 minutes:
var schedules = await db.Schedules.Where(s => s.IsActive).ToListAsync(ct);
foreach (var schedule in schedules)
    foreach (var seat in schedule.GetExpiredReservations())
        seat.Release();
if (released > 0)
    await db.SaveChangesAsync(ct);
```

---

## Decision

**Use optimistic concurrency (EF RowVersion) on `Seat` combined with an in-entity `LockedAt` timestamp (10-minute TTL) and a periodic in-process background service (`SeatExpiryService`) to release expired reservations.**

This is the strategy already implemented. This ADR formalises it and records the tradeoffs evaluated, the one known weakness (§ Consequences — Negative), and the mitigation path.

---

## Consequences

### Positive

- **No additional Azure resources.** No Redis Cache, no external lock service. Keeps the infrastructure cost profile consistent with the student subscription budget (~$45/mo after prod deletion).
- **Matches user expectations.** A 10-minute hold is the industry norm for bus/flight booking (Redbus, MakeMyTrip). Users see the seat as reserved during checkout.
- **EF RowVersion is authoritative.** The database enforces the invariant. Even if the application layer has a bug, two `SaveChangesAsync` calls on the same seat row cannot both succeed — SQL Server's `UPDATE … WHERE RowVersion = @expected` guarantees one writer wins.
- **Domain model is clean.** `Seat.Reserve()`, `Seat.Book()`, `Seat.Release()` are pure state transitions; locking logic lives in the entity and is unit-testable without a database (see `SeatConcurrencyTests`).
- **Auto-expiry is transparent.** Users whose sessions time out have their seats reclaimed without any explicit cancel action.

### Negative

- **SeatExpiryService has a multi-instance race.** When App Service scales to two instances, both background workers run independently. If both scan at the same time they will both attempt `SaveChangesAsync` on the same released seats. EF RowVersion will throw `DbUpdateConcurrencyException` on the second writer; the current catch-and-retry-in-5-minutes behaviour means the second instance's release is delayed, not dropped — but the logging is noisy and the error rate will appear in App Insights.

- **Hold window is approximate, not exact.** The lock expires at `LockedAt + 10 min`, but `SeatExpiryService` polls every 5 minutes. Worst case: a seat is held for 14 min 59 sec instead of exactly 10 min.

- **Conflicting `CreateBookingHandler` calls fail with 409, not a retry.** The endpoint maps `DbUpdateConcurrencyException` → `InvalidOperationException` → HTTP 409. The user must retry manually. Under very high concurrency for a popular seat this could degrade UX. Acceptable for a capstone; a production system would add a client-side retry loop.

### Mitigation for the multi-instance race

Replace the load-modify-save batch in `SeatExpiryService` with a single atomic SQL UPDATE:

```sql
UPDATE Seats
SET    Status   = 'Available',
       LockedAt = NULL
WHERE  Status   = 'Reserved'
  AND  LockedAt < DATEADD(minute, -10, GETUTCDATE())
```

This statement is idempotent and safe to execute concurrently from multiple instances — the last writer wins the same no-op result. Implement via `db.Database.ExecuteSqlRawAsync(...)` in the background service. The RowVersion column does not need to be checked because we are releasing rows that are in a well-known intermediate state (Reserved + expired), not resolving a conflict between two competing reservations.

---

## Alternatives Considered

### Option A — Pessimistic Locking (SQL `UPDLOCK` / `HOLDLOCK`)

```sql
SELECT * FROM Seats WITH (UPDLOCK, ROWLOCK)
WHERE ScheduleId = @id AND SeatNumber IN (@seats)
```

**Pros:** Prevents any concurrent read-modify-write race at the database level. No lost-update problem.

**Cons:**
- Holds a DB-level row lock for the duration of the request. If the user lingers on the payment page, the lock is held for seconds, blocking every other transaction on those rows.
- Risk of deadlocks if two concurrent requests compete for overlapping seat sets in different orders.
- EF Core does not model `UPDLOCK` natively; requires raw SQL or a `DbCommandInterceptor`, adding complexity that leaks infrastructure concerns into the repository.
- Does not solve the "dead-hold" problem: if the process crashes while holding the lock, SQL Server will release it when the connection closes, but the `Seat.Status` will still be `Reserved` in the table — requiring the same expiry mechanism anyway.

**Verdict:** Rejected. Adds deadlock risk and connection-hold time without eliminating the need for expiry.

---

### Option B — Distributed Lock via Azure Redis Cache

```csharp
// Acquire a per-seat lock (SETNX with TTL)
var lockKey  = $"seat-lock:{scheduleId}:{seatNumber}";
var acquired = await redis.StringSetAsync(lockKey, instanceId,
    expiry: TimeSpan.FromMinutes(10), when: When.NotExists);
if (!acquired) throw new SeatAlreadyReservedException(...);
```

**Pros:**
- Lock is held outside the database; no SQL-level locking contention.
- Multi-instance safe by design — Redis `SETNX` is atomic across all clients.
- Expiry is exact (Redis TTL), not approximate.
- Industry standard for distributed seat/inventory locking.

**Cons:**
- Adds Azure Cache for Redis (~$15/mo for Basic C0, ~$60/mo for Standard C1) to the infrastructure bill. On an Azure for Students subscription running at $45/mo, this more than doubles the monthly cost.
- Introduces a new failure mode: Redis unavailability causes the entire booking flow to fail. Requires a fallback policy or circuit breaker.
- Doubles the infrastructure surface area for a capstone project; the principal engineering challenge is the booking domain, not lock coordination.
- The Redis lock and the EF `RowVersion` would both need to exist (belt + suspenders), or the RowVersion could be removed — neither is obviously simpler.

**Verdict:** Rejected for this project scope. Correct architectural choice for a production system expecting >100 concurrent booking attempts per popular seat. Revisit if the capstone extends to load-testing at scale.

---

### Option C — No Reservation Window (First-Write-Wins)

Remove the `Reserved` state entirely. The seat transitions directly from `Available` to `Booked`:

```csharp
// Attempt to book atomically; fail if already booked
schedule.BookSeatsDirectly(seatNumbers);
await db.SaveChangesAsync(ct);  // RowVersion still guards concurrent writes
```

**Pros:**
- Simpler domain model (three states instead of four).
- No `SeatExpiryService` needed.
- No race condition in expiry.

**Cons:**
- Seats disappear between "search results" and "confirm payment." A user selects Seat 5, navigates to the payment page, and finds it gone. This is the worst UX outcome for a booking system.
- In practice, flight and bus booking systems universally use a reservation hold for this reason.
- Does not change the concurrency challenge — EF RowVersion is still needed for two simultaneous `SaveChangesAsync` calls.

**Verdict:** Rejected. The 10-minute hold is a UX requirement, not just a technical choice.

---

## Summary

| Criterion | Optimistic + LockedAt (chosen) | Pessimistic UPDLOCK | Redis Distributed Lock | No Reservation |
|-----------|-------------------------------|---------------------|----------------------|----------------|
| No double-booking | ✓ (RowVersion) | ✓ | ✓ | ✓ (RowVersion) |
| Multi-instance safe expiry | ✗ (race, mitigatable) | N/A | ✓ | N/A |
| Additional Azure cost | $0 | $0 | +$15–60/mo | $0 |
| Deadlock risk | Low | Medium | Low | Low |
| UX hold period | ✓ 10 min | ✓ | ✓ | ✗ none |
| Complexity | Low | Medium | High | Lowest |
| Domain model clarity | High | Medium | High | High |

**Chosen:** Optimistic concurrency + LockedAt. Simple, zero cost, correct for single-instance and low-concurrency multi-instance deployments. The SQL UPDATE mitigation makes it safe for horizontal scaling without adding infrastructure.
