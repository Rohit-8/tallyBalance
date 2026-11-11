# TallyBalance AI

An enterprise accounts-payable platform for the Indian market: invoice capture,
GST/TDS-aware validation, approvals, payments, and AI-assisted document
intelligence. Built as a **.NET 10 modular monolith** with a **React 19**
frontend, developed strictly phase-by-phase.

> **Status: Phase 1 — Development environment and solution setup.**
> The infrastructure, solution, and skeleton are in place and verified. No
> business features exist yet. See `project-overview.md` for the full phase plan.

---

## Hard constraints (read before running anything)

These are not preferences. They are the environment this machine actually has, and
violating them is how a setup that "should work" fails for an hour.

- **No administrator / no elevation.** Every install is user-level. Never use a
  machine-wide installer or anything that triggers a UAC prompt.
- **Containers run on Podman, not Docker.** There is no Docker daemon, no Docker
  CLI, and no Docker Desktop. The `docker-compose` binary drives the Podman engine
  over the Docker-compatible npipe. See `docs/adr/ADR-008-container-runtime-podman.md`.
- **The WSL machine is sized via `%USERPROFILE%\.wslconfig`**, not `podman machine
  set` (which is unsupported for WSL machines). It is configured for 8 GiB / 4 CPUs
  because SQL Server for Linux alone reserves ~2 GiB.
- **Mirrored networking mode** is required for the host to reach published
  container ports. See the troubleshooting note at the bottom of this file.

---

## Prerequisites

| Tool | Version | Notes |
|---|---|---|
| .NET SDK | 10.0.403 | Installed user-level at `%LOCALAPPDATA%\Microsoft\dotnet`. `DOTNET_ROOT` and PATH are set in **User** scope; new terminals need a PATH reload. |
| Node.js | ≥ 22 | v24.20.0 verified. |
| Podman | 6.0.2 | WSL-based machine `podman-machine-default`. |
| docker-compose | v2.39.4 | At `%LOCALAPPDATA%\Microsoft\dotnet-tools\docker-compose.exe`, on User PATH. |
| Git | 2.55.0 | Repo initialised, branch `main`. |

---

## First-time setup

```powershell
# 1. Ensure the Podman machine is running and sized.
podman machine start

# 2. Create your local environment file from the template.
Copy-Item .env.example .env
#   Then edit .env and set real (throwaway) passwords. .env is gitignored.

# 3. Bring up the infrastructure (SQL Server, Redis, RabbitMQ, Qdrant, MinIO).
docker-compose up -d

# 4. Set the connection strings the API needs. These override the placeholders in
#    appsettings.json and are never committed.
cd src/TallyBalance.Api
dotnet user-secrets set "ConnectionStrings:TallyBalance" "Server=127.0.0.1,1433;Database=TallyBalance;User Id=sa;Password=<MSSQL_SA_PASSWORD>;TrustServerCertificate=True;Encrypt=True"
dotnet user-secrets set "ConnectionStrings:Redis" "127.0.0.1:6379"
dotnet user-secrets set "ConnectionStrings:RabbitMQ" "amqp://tallybalance:<RABBITMQ_PASSWORD>@127.0.0.1:5672"
cd ..\..

# 5. Restore and build.
dotnet build
```

> **Why user-secrets, not `appsettings.json`?** The committed `appsettings.json`
> holds placeholder passwords. The real container passwords live in `.env` (gitignored)
> and are injected into the running process via user-secrets. This is the discipline
> that makes it impossible to accidentally commit a real credential later.

---

## Running

```powershell
# Infrastructure (if not already up)
docker-compose up -d

# API — http://127.0.0.1:5080
dotnet run --project src/TallyBalance.Api

# Frontend — http://localhost:5173 (proxies /api and /health to the API)
cd client
npm install
npm run dev
```

### Health endpoints

| Endpoint | Meaning |
|---|---|
| `GET /health/live` | Is the process up? Deliberately does **not** depend on external systems. |
| `GET /health/ready` | Can this instance serve traffic? Depends on SQL Server and Redis. |
| `GET /api/v1/system/status` | Phase 1 probe: proves the browser → API → config path and lists configured AI providers. |
| `GET /swagger` | OpenAPI UI (Development only). |

---

## Testing

```powershell
dotnet test
```

Three test projects:

- **`TallyBalance.UnitTests`** — fast, deterministic. Includes a fake `ILLMProvider`
  so no test ever makes a real network call to any AI provider.
- **`TallyBalance.IntegrationTests`** — boots the real API via `WebApplicationFactory`.
- **`TallyBalance.ArchitectureTests`** — enforces Clean Architecture dependency
  direction with NetArchTest. **These must fail CI on a dependency violation.**

---

## Project layout

```
src/
  TallyBalance.Api/            # host: endpoints, middleware, DI wiring, health checks
  TallyBalance.Application/    # use cases, ports, shared contracts (AI abstractions)
  TallyBalance.Domain/         # entities, invariants, state machines (empty in Phase 1)
  TallyBalance.Infrastructure/ # EF Core, adapters, external SDKs (LLM adapter)
  TallyBalance.Worker/         # BackgroundService consumers (outbox processor)
client/                        # React 19 + Vite + TypeScript + Tailwind
tests/
  TallyBalance.UnitTests/
  TallyBalance.IntegrationTests/
  TallyBalance.ArchitectureTests/
docs/                          # architecture, ADRs, ER diagram, learning guides
docker-compose.yaml            # infrastructure on the Podman engine
```

Dependency direction (enforced by the architecture tests):
`Api → Application → Domain`, `Infrastructure → Application`, `Api → Infrastructure`.
Domain and Application never reference Infrastructure or ASP.NET Core.

---

## AI providers

The AI layer talks to **any OpenAI-compatible `/v1/chat/completions` endpoint**. A
provider is defined entirely by base URL, API key, and model name in configuration —
adding a provider never requires provider-specific code.

Configured in `src/TallyBalance.Api/appsettings.json` under `AI:Providers`:

| Key | Endpoint | Example model |
|---|---|---|
| `maq` | `https://llm.maqsoftware.net/v1` | `qwen-3.8-27b` |
| `gemini` | Google Gemini (OpenAI-compatible) | `gemini-2.5-flash` |
| `nim` | NVIDIA NIM | `nvidia/llama-3.1-nemotron-70b-instruct` |
| `groq` | Groq | `llama-3.3-70b-versatile` |

Set an API key per provider via user-secrets (never in a committed file):

```powershell
dotnet user-secrets set "AI:Providers:maq:ApiKey" "sk-..."
```

---

## Troubleshooting

**Host cannot reach a container port (connection refused / timeout).**
This machine uses WSL **mirrored networking mode**. Published ports (DNAT) are not
exposed to the Windows host under mirrored mode, but real listeners are. If a port
refuses, confirm the machine is in mirrored mode and that the container was started
*after* the mode change (port mappings established under NAT do not migrate).

**SQL Server container exits 127.**
The `mssql/server` image is configured entirely through environment variables. Passing
`-e KEY=VALUE` as `command:` arguments makes the entrypoint try to run `-e` as a
program. Do not add a `command:` override to that service.

**`podman machine set --memory` fails.**
Unsupported for WSL machines. Edit `%USERPROFILE%\.wslconfig` instead, then
`podman machine stop; wsl --shutdown; podman machine start`.

**A downloaded script is blocked.**
PowerShell execution policy is `RemoteSigned` (CurrentUser). Run `Unblock-File` on the
script before executing it.

---

## Phase 1 completion gate

- [x] API returns a healthy response.
- [x] Swagger is reachable.
- [x] Frontend builds and the API is reachable through the dev proxy.
- [x] Podman brings up all infrastructure (SQL Server, Redis, RabbitMQ, Qdrant, MinIO).
- [x] Tests run and pass (29 tests: 17 unit, 8 architecture, 4 integration).
- [x] A developer can run everything locally from this README alone.
