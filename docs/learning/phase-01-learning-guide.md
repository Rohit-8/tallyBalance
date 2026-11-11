# Phase 1 Learning Guide — Development Environment and Solution Setup

**Phase 1 objective:** create a working development environment. No business features.
The deliverable is a machine on which the API, frontend, and infrastructure all start
together, tests pass, and a developer can run everything from the README alone.

This guide covers the teaching topics for the phase and, more importantly, the
lessons that only surface when you actually try to run the thing — because those are
the ones that cost an afternoon if you learn them from memory instead of from the
error.

---

## 1. Solution and project references

A solution is a list of projects; a project is a buildable unit. The reference
direction between them is the first architectural decision you make, and it is the
one that is hardest to undo, because every later decision assumes it.

The layout:

```
src/
  TallyBalance.Api/            # host: endpoints, middleware, DI wiring, health checks
  TallyBalance.Application/    # use cases, ports, shared contracts
  TallyBalance.Domain/         # entities, invariants, state machines
  TallyBalance.Infrastructure/ # EF Core, adapters, external SDKs
  TallyBalance.Worker/         # BackgroundService consumers
```

The reference direction:

```
Api ──→ Application ──→ Domain
Api ──→ Infrastructure ──→ Application
Worker ──→ Application
Worker ──→ Infrastructure
```

**The rule that matters:** Domain and Application never reference Infrastructure or
ASP.NET Core. Infrastructure references Application (to implement its ports), and the
hosts reference everything. This is the dependency direction that makes the modular
monolith modular — the modules can be extracted later because nothing in the core
depends on the outside.

**Why the direction is enforced by tests, not by convention.** A dependency rule that
lives only in a comment is a dependency rule that will be violated the first time a
developer is in a hurry. `TallyBalance.ArchitectureTests` uses NetArchTest to assert
the direction, and those tests run in CI. A rule that fails CI is a rule that holds.

**A concrete failure this prevents:** a use case in Application that calls
`HttpContext` directly. It compiles, it works, and it is invisible until someone tries
to run the same use case from a background job — where there is no `HttpContext` — and
it throws. The architecture test catches it at the point of introduction, not at the
point of pain.

---

## 2. Clean Architecture dependency direction

Clean Architecture is not a set of folders. It is a single rule: **dependencies point
inward, toward the thing that is least likely to change.**

The thing least likely to change is the domain — the business rules. The thing most
likely to change is the outside — the database, the HTTP framework, the AI provider.
So the domain depends on nothing, and everything else depends on the domain.

The mechanism that makes this work is **dependency inversion**: the Application layer
declares a port (an interface, e.g. `ILLMProvider`), and the Infrastructure layer
provides an implementation. The Application layer depends on the port, not the
implementation. The implementation can change — swap the AI provider, swap the
database — without the Application layer knowing.

**The composition root is the one place the rule is broken, on purpose.** The hosts
(`Api`, `Worker`) are allowed to reference Infrastructure, because they are the
place where ports are bound to implementations. That is the single seam where the
outside is allowed in. If you find yourself wanting to break the rule somewhere else,
that is a signal that a port is missing, not that the rule is wrong.

---

## 3. Dependency injection lifetimes

Three lifetimes, and the mistake is always the same: choosing the wrong one for the
thing's actual nature.

| Lifetime | Meaning | Use for |
|---|---|---|
| **Transient** | A new instance per resolution | Stateless services, use cases, most things |
| **Scoped** | One instance per HTTP request (or per unit of work) | `DbContext`, anything that should be shared within a request but not across requests |
| **Singleton** | One instance for the process's life | Infrastructure that must be shared, e.g. a connection factory |

**The failure mode is a captive dependency.** A singleton that depends on a scoped
service captures the first scoped instance and reuses it forever — so every request
shares one `DbContext`, which is a data-corruption bug that looks like a concurrency
problem. The fix is not to change the lifetime; it is to recognise that a singleton
should not depend on a scoped service at all.

**The AI provider is transient, deliberately.** Each call resolves a fresh
`ILLMProvider` bound to a specific provider name. That is what lets a use case ask
for two different providers on the same request — which evaluation in Phase 10 needs —
without any configuration change.

---

## 4. Configuration and the options pattern

The options pattern is the discipline of binding a configuration section to a typed
object, validating it, and injecting that object — rather than reading
`configuration["Some:Key"]` scattered through the code.

Why it matters here: the AI provider is defined entirely by `BaseUrl`, `ApiKey`, and
`ModelId`. Binding `AI:Providers` to `AiProviderOptions` means **adding a provider is a
configuration change, not a code change.** That is the whole point of the abstraction,
and it only works if the configuration is typed and validated.

**Validate on start.** `ValidateOnStart()` means a typo in the configuration fails the
application at startup with a clear message, rather than on the first AI call in
production. A configuration error that surfaces in production is a configuration error
that costs an incident.

**Secrets are not configuration.** The API key is never in `appsettings.json`. It comes
from user-secrets in development and a secret store in production. The committed file
holds a placeholder; the real value is injected at runtime. This is the discipline that
makes it impossible to accidentally commit a real credential — because there is no real
credential in the file to commit.

---

## 5. Middleware pipeline

The pipeline is the order in which requests are processed, and the order is the
decision. Each piece of middleware wraps the next, so the first one added is the last
one to see the response.

The Phase 1 pipeline:

```
Serilog request logging
  → exception handler
    → status code pages
      → CORS
        → health checks / endpoints
```

**The mistake that costs the most:** putting the exception handler after something that
throws. A 500 on an unmatched route usually means the exception handler is sitting
after the routing middleware, so the routing error escapes it. The integration test
`Unknown_api_route_returns_404_not_500` exists specifically to catch this — it is cheap
to assert, and it catches a class of wiring bug nothing else covers.

---

## 6. Health checks: liveness vs readiness

This is the part people get wrong, and it is the part that turns one outage into two.

| Endpoint | Question | Depends on external systems? |
|---|---|---|
| `/health/live` | Is this process up? | **No.** |
| `/health/ready` | Can this instance serve traffic? | **Yes** — SQL Server, Redis. |

**Liveness must not depend on external systems.** An orchestrator that restarts the API
because SQL Server is slow turns one outage into two: the database is down, and now the
API is being killed and restarted in a loop, which makes the database *more* unavailable.
Liveness answers "should this process be alive?" and the answer is "yes, it is running."

**Readiness answers "should traffic be routed here?"** and the answer is "only if the
dependencies it needs are up." This is the endpoint a load balancer or compose should
gate on.

**A readiness probe should depend only on what the running code calls.** In Phase 1,
nothing publishes to RabbitMQ, stores an object in MinIO, or searches Qdrant. Making
readiness depend on those services turns an unrelated container outage into an API
outage. They are added in the phases that first use them. This is stated in
`HealthCheckRegistration.cs` rather than hidden, because the reason is the kind of thing
that gets "fixed" by someone who sees a missing check and does not know why it is
missing.

---

## 7. Environment-based configuration and secrets

The same code runs in development, staging, and production. The only thing that should
differ is configuration — connection strings, endpoints, secrets. The mechanism is
environment-based configuration: `appsettings.{Environment}.json`, user-secrets, and
environment variables, in that order of precedence.

**The discipline that matters:** the committed configuration holds placeholders, and the
real values are injected at runtime and never committed. `.env` is gitignored. This is
not paranoia — it is the only way to make it *impossible* to leak a real credential,
because there is no real credential in the repository to leak.

---

## 8. Lessons from actually running it

These are the lessons that do not appear in the teaching topics, because they only
surface when you try to run the thing on this machine. They are recorded here so they
are not re-learned from scratch next time.

### 8.1 The container runtime is Podman, not Docker

There is no Docker daemon, no Docker CLI, and no Docker Desktop. The `docker-compose`
binary drives the Podman engine over the Docker-compatible npipe. This is recorded in
`docs/adr/ADR-008-container-runtime-podman.md`. The lesson: **do not assume the runtime
from the tool name.** `docker-compose` is a client; the engine it talks to is a
separate decision, and on this machine that decision is Podman.

### 8.2 The WSL machine is sized via `.wslconfig`, not `podman machine set`

`podman machine set --memory` is unsupported for WSL machines. The only way to size a
WSL-based Podman machine is `%USERPROFILE%\.wslconfig`. The machine was raised from 2
GiB to 8 GiB because SQL Server for Linux alone reserves ~2 GiB — at 2 GiB it starts
and then dies during recovery, which looks like a compose bug and is not.

### 8.3 Mirrored networking mode is required for the host to reach containers

This was the hardest problem in the phase, and it is worth understanding rather than
just memorising.

The symptom: the API's health checks time out, but a raw TCP test to the same port
succeeds. The cause: under WSL **mirrored networking mode**, published ports (which use
DNAT) are not exposed to the Windows host, but real listeners are. And `localhost`
resolves to IPv6 `::1` first, which has no listener, while the containers publish only
on IPv4 `127.0.0.1`.

The fix: use `127.0.0.1` explicitly in connection strings, not `localhost`. The lesson
is general: **when a connection "times out" but a raw TCP test succeeds, the problem is
usually not the target — it is the address resolution or the network mode between the
two.** The fix was to make the connection string say exactly what it meant.

### 8.4 A health check that always passes is worse than no health check

The SQL Server image ships neither `sqlcmd` nor `curl`, so a query-based or HTTP probe
is not available without building a derived image. The healthcheck uses bash's
`/dev/tcp` to make a real TCP connection to the listener. It is weaker than running a
query — it proves the endpoint accepts sockets, not that a database can be opened — and
that gap is stated rather than hidden. The lesson: **a check that always returns 0
looks identical to a healthy system, and it is the most dangerous kind of check.** A
weaker-but-honest check is better than a stronger-but-fake one.

### 8.5 The boot test earned its keep

The integration test that boots the real API via `WebApplicationFactory` caught a real
DI bug that unit tests could not see: a keyed service registration that bound to the
wrong overload, registering the service as its own implementation type. It would have
failed at API startup in production. The lesson: **the test that builds the whole
composition graph is the one that catches the wiring bugs, and those bugs are invisible
to unit tests because unit tests never build a service provider.**

---

## 9. Common mistakes in this phase

| Mistake | Why it fails | Fix |
|---|---|---|
| Assuming Docker exists | No Docker daemon on this machine | Use `docker-compose` over the Podman npipe |
| `podman machine set --memory` | Unsupported for WSL machines | Edit `%USERPROFILE%\.wslconfig` |
| `localhost` in connection strings | Resolves to `::1` first under mirrored mode | Use `127.0.0.1` explicitly |
| `command:` override on `mssql/server` | Image is configured by env vars, not CLI args | Remove the `command:` override |
| Committing real credentials | A real credential in the repo is a leaked credential | Placeholders in `appsettings.json`, real values in user-secrets |
| Liveness depending on external systems | Turns one outage into two | Liveness depends on nothing; readiness depends on what the code calls |
| A health check that always passes | Looks identical to healthy | Make the check able to fail |

---

## 10. Interview questions

**Q: What is the difference between liveness and readiness, and why does it matter?**
A: Liveness answers "is this process up?" and must not depend on external systems,
because an orchestrator that restarts the API because SQL Server is slow turns one
outage into two. Readiness answers "can this instance serve traffic?" and depends on
the dependencies the running code actually calls. A load balancer gates on readiness;
an orchestrator gates on liveness.

**Q: Why is the AI provider a configuration change, not a code change?**
A: Because a provider is defined entirely by base URL, API key, and model name, bound to
a typed options object and validated on start. Adding a provider is editing the
configuration section, not writing a new adapter. The abstraction earns its keep only if
this is true.

**Q: Why does the architecture test exist, and what would happen without it?**
A: It enforces the Clean Architecture dependency direction — Domain and Application never
reference Infrastructure or ASP.NET Core. Without it, the rule lives only in a comment
and is violated the first time a developer is in a hurry. A rule that fails CI is a rule
that holds.

**Q: A health check always returns healthy. Is that a problem?**
A: Yes — a check that always passes looks identical to a healthy system, and it is the
most dangerous kind of check. A weaker-but-honest check (a TCP probe that can fail) is
better than a stronger-but-fake one (a check that always returns 0).

**Q: Why does the boot test matter when unit tests already pass?**
A: Because it builds the whole composition graph, which unit tests never do. DI cycles,
a missing registration, a malformed options section, and a middleware ordering mistake
are all startup failures that unit tests cannot see. The boot test caught a real keyed
registration bug that would have failed at API startup in production.

**Q: Why are secrets not in `appsettings.json`?**
A: Because a real credential in a committed file is a leaked credential. The committed
file holds placeholders; the real values are injected at runtime via user-secrets in
development and a secret store in production. This makes it impossible to accidentally
commit a real credential, because there is no real credential in the file to commit.

---

## 11. Phase 1 completion checklist

- [x] API returns a healthy response (`/health/ready` → Healthy, SQL Server + Redis).
- [x] Swagger is reachable (`/swagger` → 200, OpenAPI document served).
- [x] Frontend builds and the API is reachable through the dev proxy.
- [x] Podman brings up all infrastructure (SQL Server, Redis, RabbitMQ, Qdrant, MinIO).
- [x] Tests run and pass (29 tests: 17 unit, 8 architecture, 4 integration).
- [x] A developer can run everything locally from the README alone.
- [x] No business features implemented — this is the Phase 1 gate.

**Wait for approval before starting Phase 2 (Identity and Authentication).**
