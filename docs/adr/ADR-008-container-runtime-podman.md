# ADR-008 — Container Runtime: Podman, driven by docker-compose

- **Status:** Accepted (pending developer review)
- **Date:** 2026-11-10
- **Phase:** 0
- **Supersedes:** none (ratifies the global rule in `project-overview.md` §3.8)

---

## Context

The project rule is that **Podman is the container engine and Docker is not used** — no Docker
Desktop, no Docker daemon. All local infrastructure (SQL Server, Redis, RabbitMQ, Qdrant, optional
MinIO) must run as Podman pods.

We still need a way to declare and start that multi-service topology declaratively. Three options
exist on this machine.

Environment facts verified on 2026-11-10:

| Fact | Value |
|---|---|
| Podman | 6.0.2, WSL-based machine `podman-machine-default` |
| Docker daemon / Docker Desktop | **Not installed** |
| `podman-compose` | **Not installed** |
| `podman compose` | Exists, but is only a thin wrapper requiring an external compose provider |
| `podman machine set --memory/--cpus` | **Unsupported for WSL machines** |
| Admin privileges | **Not available** — user-scope installs only |
| Podman Docker-compatible socket | `npipe:////./pipe/docker_engine` (exposed by default) |

---

## Decision

1. **Engine:** Podman, rootless-where-possible, WSL machine.
2. **Compose tooling:** the standard **`docker-compose` binary (v2.39.4)** pointed at Podman's
   Docker-compatible API socket via `DOCKER_HOST=npipe:////./pipe/docker_engine`.
   Compose files are written in ordinary Compose specification format.
3. **Installation scope:** user-level only.
   `docker-compose.exe` lives in `%LOCALAPPDATA%\Microsoft\dotnet-tools`, added to the **User** PATH.
4. **Resource sizing:** controlled by `%USERPROFILE%\.wslconfig`, **not** `podman machine set`.

   ```ini
   [wsl2]
   memory=8GB
   processors=4
   swap=4GB
   localhostForwarding=true
   ```

   Applied with `podman machine stop; wsl --shutdown; podman machine start`.
5. **Images:** OCI-compatible, pulled from public registries. Any `Dockerfile` must build with
   `podman build`.
6. **Fallback:** if the docker-compose-over-Podman path breaks in a way that blocks work, switch to
   `podman-compose` (or Quadlet) and record it as a superseding ADR.

---

## Verification performed before adopting

A throwaway compose file was run against the Podman socket:

```yaml
services:
  probe:
    image: docker.io/library/alpine:3.20
    command: echo podman-compose-ok
```

`docker-compose up` pulled the image through Podman, created the network and container, printed
`podman-compose-ok`, and exited 0. The approach was adopted only after this passed.

---

## Alternatives considered

### Option A — `podman-compose` (Python)
- **Pros:** Podman-native, no Docker-ecosystem binary, actively maintained.
- **Cons:** Not installed here. Its Compose-spec coverage has historically lagged the reference
  implementation, and some features behave differently — which produces confusing failures that are
  really compose-dialect problems, not infrastructure problems. Error messages are also less familiar,
  which slows debugging for a portfolio project where the goal is learning the *application*.
- **Not chosen as primary.** Retained as the documented fallback.

### Option B — Quadlet / systemd units
- **Pros:** The most Podman-native production pattern; systemd-managed, restart policies, clean
  integration with Linux service management.
- **Cons:** Requires systemd inside the Podman machine and is awkward on a Windows/WSL developer
  workstation. High ceremony for "start four containers for local development", and it teaches
  systemd rather than the application.
- **Rejected for local dev.** Worth revisiting for a Linux-hosted demo environment.

### Option C — Plain `podman pod create` + `podman run` PowerShell scripts
- **Pros:** Zero extra tooling; explicit and easy to read.
- **Cons:** No declarative dependency ordering, no secret/config templating, no reusable profiles,
  and every developer edit becomes imperative script logic. Drifts quickly.
- **Rejected** as the primary mechanism, though a thin PowerShell wrapper *around* compose for
  one-command startup is acceptable.

### Option D — Install Docker Desktop
- **Rejected outright:** violates the project rule, requires administrator privileges which are not
  available, and adds a licensing and resource burden we do not want.

---

## Consequences

### Positive
- Full, conventional Compose-spec support while the actual engine remains Podman.
- Compose files stay portable and conventional — transferable knowledge, and CI-friendly.
- No Docker daemon, no Docker Desktop, no daemon-root dependency, no admin rights.
- Familiar error messages and documentation for the developer.
- Podman's daemonless, rootless model is a genuine security advantage and is worth demonstrating.

### Negative (accepted)
- Using a Docker-branded binary may look contradictory next to the "no Docker" rule. Mitigation:
  this ADR, plus `project-overview.md` §3.8, state clearly that it is a **client** talking to the
  **Podman** engine over a compatible API. Nothing Docker runs.
- The socket path must be set (`DOCKER_HOST`) in new shells. Mitigation: documented in the README
  and in a startup script; Podman's default socket already matches, so it usually works unset.
- Occasional Compose-spec features may behave differently under Podman (e.g. some `network_mode`
  and `secrets` semantics). Mitigation: keep compose files simple; verify each service as it is
  added in Phase 1.
- WSL resource limits are configured in a file outside the repo (`.wslconfig`), so a new machine
  needs it set up manually. Mitigation: documented in the learning guide and README, with the exact
  contents.

### Why the memory change mattered
SQL Server for Linux requires roughly 2 GiB for its own buffer pool. The machine shipped at 2 GiB
total, which would have produced OOM kills that present as unexplained container restarts. Raising
the limit to 8 GiB (verified 7.8 GiB inside the VM) is a prerequisite for Phase 1, not an optimisation.

---

## Related

- `project-overview.md` §3.8 — global container rule
- `docs/architecture/system-overview.md` §2 — verified environment
- `docs/learning/phase-00-learning-guide.md` §10 — environment engineering lessons
