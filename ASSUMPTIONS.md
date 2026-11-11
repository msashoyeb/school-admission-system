## 1. Concurrency & Seat Allocation

- Uses PostgreSQL transactions and `SELECT ... FOR UPDATE` to prevent multiple requests from booking the same seats.
- Row-level locking works across multiple backend processes while allowing different classes to process requests in parallel.
- A partial unique index (`ux_holds_active_class_student`) prevents a student from having more than one active or confirmed hold in the same class.

## 2. Class Creation

- Class names are required, trimmed, and limited to 200 characters.
- `totalSeats` must be greater than zero.
- Duplicate class names are allowed.

## 3. Student Identity

- The frontend requires a 17-digit numeric Birth Registration Number.
- The ID is stored in `localStorage` to keep it after page refreshes or browser restarts.
- The backend accepts any non-empty `X-Student-Id` up to 64 characters to support automated tests and other clients.

## 4. Hold Lifecycle

- A background service checks expired holds every second.
- API operations and database queries also check expiration immediately.
- Only `HELD` and `CONFIRMED` holds prevent duplicate applications. Students can apply again after expiration or cancellation.
- Only the hold owner can confirm or cancel a hold. Otherwise, the API returns `403 Forbidden`.
- Repeated confirmation or cancellation requests are handled safely without counting seats twice.
- Invalid state changes return `409 Conflict`.

## 5. Docker & Database

- PostgreSQL data is stored in the `postgres_data` volume and survives container restarts.
- Host port `5433` maps to PostgreSQL container port `5432` to avoid local port conflicts.
- The backend connects to PostgreSQL using `postgres:5432`.
- EF Core migrations run automatically when the backend starts, with retry logic while PostgreSQL initializes.