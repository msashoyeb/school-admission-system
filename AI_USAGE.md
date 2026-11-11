
## Tools Used
- **ChatGPT Free and Claude Free:** Used across project planning, Clean Architecture scaffolding (`Domain`, `Application`, `Infrastructure`, `Api`), PostgreSQL implementation, xUnit + Testcontainers integration tests, minimal React + Vite UI, Vitest unit tests, Docker setup, and documentation.

## Mistakes Caught and How They Were Fixed

1. **Wrong Vite template in PowerShell:** `npm create vite@latest frontend -- --template react-ts` created a plain TypeScript app without React because PowerShell ate `--`. I checked `package.json`, deleted the folder, and re-created it with `npx --yes create-vite@latest frontend --template react-ts`.

2. **Wrong test package version:** Adding `Microsoft.AspNetCore.Mvc.Testing` without a version pulled `11.0.0` (for .NET 11), which failed on .NET 10 (`NU1202`). I saw the restore error and installed `--version 10.0.6`.
