# School Admission System

Online first-come, first-served school admission reservation system built with .NET 10 (Clean Architecture), PostgreSQL 18, React (TypeScript + Vite), and Docker.

## Run With Docker

Prerequisites: **Docker** (Docker Desktop or Docker Engine with Compose).

1. Start PostgreSQL, backend, and frontend together from the repository root:
   ```bash
   docker compose up --build
   ```
2. Open the applications:
   - **Frontend UI:** http://localhost:3000
   - **Backend API:** http://localhost:8080
3. Stop containers (data persists in the `postgres_data` Docker volume):
   ```bash
   docker compose down
   ```

### Configurable Environment Variables

- `HOLD_DURATION_SECONDS` (backend, default `600`): hold expiration duration in seconds.
  ```bash
  HOLD_DURATION_SECONDS=30 docker compose up --build
  ```
- `ConnectionStrings__DefaultConnection` or `DATABASE_URL` (backend): PostgreSQL connection string.
- `VITE_API_BASE_URL` (frontend, default `http://localhost:8080`): backend base URL injected into the frontend container at startup.

## Run Automated Tests

### Backend Tests (xUnit + PostgreSQL)

The backend test suite includes 7 integration tests covering high-concurrency seat holding, two-instance multi-process concurrency, automatic hold expiration without API calls, confirmation idempotency, cancellation, and ownership rules.

- With Docker running (uses Testcontainers `postgres:18-alpine` automatically):
  ```bash
  dotnet test backend/SchoolAdmission.slnx
  ```
- Or against an existing PostgreSQL instance:
  ```bash
  TEST_DATABASE_URL="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres" dotnet test backend/SchoolAdmission.slnx
  ```

### Frontend Tests (Vitest + React Testing Library)

```bash
cd frontend
npm install
npm test
```

## Project Structure

- `backend/`
  - `SchoolAdmission.Domain/`: Entities, enums, domain exceptions.
  - `SchoolAdmission.Application/`: DTOs, service interfaces, validation logic.
  - `SchoolAdmission.Infrastructure/`: EF Core `AdmissionDbContext`, PostgreSQL migrations, `FOR UPDATE` repository, background hold-expiration worker.
  - `SchoolAdmission.Api/`: Controllers, exception middleware, OpenAPI/Swagger UI (Development), `Dockerfile`.
  - `SchoolAdmission.Tests/`: PostgreSQL integration & concurrency tests.
- `frontend/`: React + TypeScript + Vite SPA, Vitest suite, Nginx `Dockerfile`.
- `docker-compose.yml`: PostgreSQL 18, backend (`8080`), and frontend (`3000`) orchestration.
