# School Admission System

**Tech Stack:** .NET 10, Clean Architecture, PostgreSQL 18, React, TypeScript, Vite, and Docker.

## 1. Run with Docker

Start the database, backend, and frontend:
```
docker compose up --build
```

**Application URLs:**
- Frontend: [http://localhost:3000](http://localhost:3000)
- Backend API: [http://localhost:8080](http://localhost:8080)

Stop the containers:
```
docker compose down
```

**Configure reservation hold duration (default: 600 seconds):**
```
HOLD_DURATION_SECONDS=30 docker compose up --build
```

## 2. Run Backend Tests

Requires Docker (uses Testcontainers with PostgreSQL 18):
```
dotnet test backend/SchoolAdmission.slnx
```

Alternatively, use an existing PostgreSQL database:
```
TEST_DATABASE_URL="Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres" dotnet test backend/SchoolAdmission.slnx
```

## 3. Run Frontend Tests

```
cd frontend
npm install
npm test
```

**Testing Tools:** xUnit, Vitest, and React Testing Library.