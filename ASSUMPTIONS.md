# Assumptions & Design Decisions

## 1. Multi-Process Concurrency Safety (Bonus)
- **Assumption / Implementation:** In-memory locks (`lock` or `SemaphoreSlim`) only protect a single process and fail when two backend processes share one PostgreSQL database.
- **How it is ensured:** `POST /classes/{classId}/holds` runs inside a PostgreSQL transaction (`IsolationLevel.ReadCommitted`) and executes `SELECT ... FROM classes WHERE id = @classId FOR UPDATE`. PostgreSQL acquires a row-level exclusive lock on that class row until commit/rollback, serializing concurrent seat allocations for the same class across **all** connections and **all** backend processes while allowing different classes to process in parallel.
- **Database defense-in-depth:** A PostgreSQL partial unique index `ux_holds_active_class_student` on `(class_id, student_id) WHERE status IN ('HELD', 'CONFIRMED')` guarantees at the database level that a student can never have more than one active or confirmed seat in the same class, even under simultaneous requests across processes.

## 2. Class Creation & Naming
- Each shift (e.g., `Class 6 - Morning Shift`, `Class 6 - Day Shift`) is stored as an independent `classes` record.
- `totalSeats` must be greater than `0`, and `name` is trimmed and required (`<= 200` characters).
- Class names are not enforced as unique in the database so repeated test runs or separate cohorts using the same name do not fail unexpectedly.

## 3. Student Identity (`X-Student-Id`)
- The frontend enforces and guides a 17-digit numeric Birth Registration Number (`^\d{17}$`) and persists it in `localStorage` so applications survive page refreshes and browser restarts.
- On the backend API, `X-Student-Id` is required to be non-empty (`<= 64` characters) without rejecting shorter synthetic IDs, so automated API tests using values like `student_1` work seamlessly alongside 17-digit IDs.

## 4. Hold Lifecycle, Expiry, Confirmation & Cancellation
- **Automatic expiration without API calls:** `HoldExpirationBackgroundService` runs every 1 second to mark `HELD` rows with `expires_at <= now()` as `EXPIRED`. Additionally, all read queries and transactional hold/confirm/cancel methods evaluate `expires_at <= now()` immediately so there is zero stale window between worker ticks.
- **Re-applying after expiry or cancellation:** Only `HELD` and `CONFIRMED` holds block a duplicate application in the same class; students whose hold became `EXPIRED` or `CANCELLED` may apply again if seats are available.
- **Ownership & idempotency:**
  - `POST /holds/{holdId}/confirm` and `DELETE /holds/{holdId}` return `403 Forbidden` if `X-Student-Id` does not match the hold owner.
  - Repeating `POST /holds/{holdId}/confirm` on an already `CONFIRMED` hold returns `200 OK` (`CONFIRMED`) idempotently without double-counting `confirmedSeats`.
  - Confirming a `CANCELLED` or `EXPIRED` hold returns `409 Conflict`.
  - Cancelling a `CONFIRMED` or `EXPIRED` hold returns `409 Conflict`; repeating `DELETE /holds/{holdId}` on an already `CANCELLED` hold returns `204 No Content`.

## 5. Docker Configuration & Persistence
- `postgres_data` mounts `/var/lib/postgresql` (the PostgreSQL 18 Docker volume directory) so data survives `docker compose down` and `docker compose up`.
- Host port `5433` maps to container port `5432` to avoid conflicts with any local PostgreSQL installation on port `5432`, while backend communicates internally over `postgres:5432`.
- Backend runs EF Core migrations automatically on startup (`DatabaseInitializer`) with retry backoff so it waits cleanly if PostgreSQL is still initializing.
