# AI Usage Log

## Tools Used
- **ChatGPT free and Claude Free:** Used across project planning, Clean Architecture scaffolding (`Domain`, `Application`, `Infrastructure`, `Api`), PostgreSQL concurrency/expiry repository implementation, xUnit + Testcontainers integration tests, minimal React + Vite UI, Vitest unit tests, Docker setup, and documentation.

## Mistakes Caught and How They Were Fixed

1. **PowerShell Stripping `--` in `npm create vite`:**
   - **What happened:** Running `npm create vite@latest frontend -- --template react-ts` in PowerShell caused `--` to be consumed by PowerShell, scaffolding a vanilla TypeScript template without `react` or `react-dom`.
   - **How caught & fixed:** Inspected `frontend/package.json` and `frontend/src/` immediately after scaffolding, removed the vanilla folder, and re-scaffolded directly with `npx --yes create-vite@latest frontend --template react-ts`.

2. **Incompatible `Microsoft.AspNetCore.Mvc.Testing` Version:**
   - **What happened:** Installing `Microsoft.AspNetCore.Mvc.Testing` without pinning a version resolved `11.0.0` (targeting `net11.0`), failing NuGet restore with `NU1202` on `net10.0`.
   - **How caught & fixed:** Observed the restore error and explicitly installed `--version 10.0.6`.

3. **Infinite Polling Loop in React `useEffect`:**
   - **What happened:** Including the `activeHold` object reference inside the `useEffect` dependency array in `frontend/src/App.tsx` caused `setActiveHold(detail)` to re-trigger the effect continuously.
   - **How caught & fixed:** Caught when the Vitest fake-timer expiry test timed out at `5000ms` (`App.test.tsx`). Replaced the object dependency with a primitive boolean (`shouldPollSelectedHold`) and verified with `npm run lint` (`0` warnings) and `npm test` (`4` passed).

4. **PostgreSQL 18 Docker Volume Path & Windows CRLF Entrypoint:**
   - **What happened:** `postgres:18-alpine` stores cluster data under `/var/lib/postgresql/18/docker` rather than `/var/lib/postgresql/data`, and Windows CRLF line endings can break shell entrypoints inside Alpine containers.
   - **How caught & fixed:** Mounted `postgres_data:/var/lib/postgresql` in `docker-compose.yml` and stripped `\r` in `frontend/Dockerfile` (`sed -i 's/\r$//' /docker-entrypoint.sh`).
