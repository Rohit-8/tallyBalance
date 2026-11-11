# ADR-001 — Modular Monolith Architecture

- **Status:** Accepted (pending developer review)
- **Date:** 2026-11-10
- **Phase:** 0
- **Deciders:** AI development agent (proposing), developer (approving)

---

## Context

TallyBalance AI is an enterprise SaaS invoice-processing platform with ten business capabilities:
Identity, Organizations, Vendors, Invoices, Adjustments, Approvals, Documents, Notifications, AI,
and Analytics.

We must choose a deployment and decomposition architecture. The choice determines how transactions
work, how tenant isolation is enforced, how the financial adjustment engine guarantees correctness,
and how much operational overhead the project carries.

Key constraints from the specification:

1. The **financial adjustment engine** requires strong consistency: applying an adjustment must
   atomically write the adjustment, its history, the derived invoice state, and an outbox event.
2. **Tenant isolation** is a security boundary requiring enforcement in depth, including in
   background processing.
3. The project is built and demonstrated by **one developer**, and is a portfolio artifact — so
   operational complexity has a real cost and must be justified.
4. Some work is genuinely **slow and should be asynchronous**: document extraction, embeddings,
   AI summarisation, notification delivery.
5. The developer is strengthening skills in system design, distributed systems, and concurrency —
   so the architecture should teach those concepts, but not manufacture problems to solve.

---

## Decision

Adopt a **modular monolith**:

- **Two deployable processes:**
  1. `TallyBalance.Api` — the ASP.NET Core Web API hosting all modules.
  2. `TallyBalance.Worker` — background consumers for slow and asynchronous work.
- **Ten internal modules**, each owning its own entities, use cases, endpoints, tables, and domain
  events.
- **Four horizontal layers** (`Api`, `Application`, `Domain`, `Infrastructure`) crossed by the
  vertical module slices.
- **Enforced dependency rules** via automated architecture tests, not convention.
- **Shared database** (SQL Server) with one owning module per table.
- **RabbitMQ + transactional outbox** for asynchronous, reliable cross-module effects.
- **Redis** for justified caching, **Qdrant** for vector retrieval.

Modules are **not** services. They are compile-time and test-time boundaries inside one process.

---

## Alternatives considered

### Option A — Layered monolith (no module boundaries)
Classic `Controllers / Services / Repositories / Data` folders with no module separation.

- **Pros:** Simplest possible start. Least ceremony. Fastest first feature.
- **Cons:** Boundaries erode immediately. With ten capabilities and a financial core, anything can
  call anything. After several months, extracting the AI module or reasoning about the adjustment
  engine's dependencies becomes genuinely hard. Nothing mechanically prevents an invoice controller
  from writing to the approvals table.
- **Rejected because:** the cost is paid later, invisibly, and is very hard to retrofit. The
  discipline costs little now.

### Option B — Microservices from the start
One service per capability, own database, network communication.

- **Pros:** Independent deployment and scaling. Strong isolation. Matches the module list
  superficially.
- **Cons:** Fatal for the adjustment engine. Applying an adjustment spans invoice state, adjustment
  ledger, history, and outbox — across service boundaries that becomes a **distributed transaction**
  or a **saga with compensation logic**. Both are dramatically harder to get right and to test than
  one database transaction. Also: ten services means ten deployments, ten pipelines, service
  discovery, network failure modes, and clock-skew reasoning — for a single-developer portfolio
  project. It would also make tenant isolation harder, since membership checks cross service lines.
- **Rejected because:** it introduces the hardest problems in distributed systems without solving a
  problem we actually have. There is no scaling, team, or availability requirement that justifies it.

### Option C — Serverless / function-per-endpoint
- **Pros:** Minimal infrastructure management, good for bursty AI workloads.
- **Cons:** Long-running AI and document processing fit poorly against execution timeouts. Local
  development and debugging become awkward. Cold starts distort latency measurements. Harder to
  demonstrate transactional behaviour clearly.
- **Rejected because:** it obscures exactly the engineering concepts this project exists to
  demonstrate.

### Option D — Single monolith with a background worker, no modules
- **Pros:** Simple, and gets async processing right.
- **Cons:** Same boundary-erosion problem as Option A.
- **Rejected because:** the worker is genuinely needed, but the module discipline is what makes the
  monolith maintainable.

---

## Consequences

### Positive
- **Real ACID transactions** across the financial core. The adjustment engine can be correct in the
  straightforward way: one transaction, one `RowVersion` check, one commit.
- **Tenant isolation is enforceable in one place** — the request pipeline and query boundaries —
  rather than being reconstructed across network hops.
- **Fast local development.** One process to run, one debugger to attach, one log stream.
- **Low operational cost.** Two containers plus infrastructure, deployable to App Service.
- **Testability.** Domain and application layers are pure C#; the adjustment engine and state
  machine are unit-testable with no database and no network.
- **A real extraction path.** Because modules already own their tables and talk outward via events,
  extracting a module later is mostly a messaging change rather than a rewrite.

### Negative (accepted)
- **Boundaries can still erode** if not enforced. Mitigation: architecture tests fail the build on
  violations (Rule D1–D7 in `dependency-rules.md`).
- **Single deployment unit** means one bad module can take down the process. Mitigated by moving
  slow/risky work (AI, document processing) into the worker, and by timeouts and circuit handling on
  all external calls.
- **Scaling is process-wide.** Mitigated by scaling API and worker independently — which covers the
  realistic load profile here, since AI/document work is the heavy part.
- **In-process event dispatch can be forgotten** and replaced by a direct call. Mitigated by Rule M3
  and code review.
- **More upfront structure** than a layered monolith. Accepted deliberately.

### Neutral / to watch
- Modules begin as **folders and namespaces**, not separate assemblies. Assembly separation is
  deferred until a module is stable, because premature assembly splitting multiplies build time and
  ceremony without adding safety beyond what architecture tests already provide.
- The outbox pattern adds a table and a publisher loop. This is the honest cost of reliable
  publishing without distributed transactions, and it is a valuable concept to demonstrate.

---

## Design rules that make this work

1. **Domain depends on nothing.** No EF Core, no ASP.NET.
2. **Application defines ports; Infrastructure implements adapters.** Dependency inversion at every
   external boundary.
3. **No cross-module entity access.** Cross-module reads go through an application contract;
   cross-module writes go through domain events.
4. **No cycles.** Enforced mechanically.
5. **Events are written to the outbox in the same transaction** as the state change. Never publish
   to RabbitMQ inside a database transaction.
6. **Background consumers must re-establish tenant context** from the message envelope.
7. **AI is advisory only** and has no write path to financial state.

---

## Revisit when

Consider extracting a module into a separate service only if **all** of the following become true:

- It has a genuinely independent scaling profile that process-level scaling cannot address.
- It has an independent availability requirement.
- Its interface with the rest of the system is already event-driven and narrow (which the module
  rules are designed to produce).
- The team is large enough that independent deployment reduces coordination cost rather than adding it.

Absent those, the modular monolith is the correct and simpler answer.

---

## Related

- `docs/architecture/system-overview.md`
- `docs/architecture/module-boundaries.md`
- `docs/architecture/dependency-rules.md`
- `docs/database/initial-er-diagram.md`
- `docs/security/security-overview.md`

## Follow-up ADRs planned

| ADR | Topic | Phase |
|---|---|---|
| ADR-002 | Authentication: JWT + rotating refresh tokens | 2 |
| ADR-003 | Tenant isolation strategy | 3 |
| ADR-004 | Invoice state machine | 5 |
| ADR-005 | Append-only adjustment ledger and undo/redo semantics | 7 |
| ADR-006 | Transactional outbox and messaging topology | 9 |
| ADR-007 | Provider-independent AI layer and RAG pipeline | 10–11 |
| ADR-008 | Container runtime: Podman with docker-compose driver | 0 (ratified here) |
