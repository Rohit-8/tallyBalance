import { useQuery } from "@tanstack/react-query";
import { api, ApiError } from "./lib/api";
import { formatIst } from "./lib/format";

/**
 * Phase 1 screen. Its purpose is to prove the full path works end to end:
 * browser → Vite proxy → ASP.NET Core → configuration binding → JSON back into React.
 *
 * It is not a dashboard and deliberately does not pretend to be one. Every element here
 * corresponds to something the Phase 1 completion gate actually requires: the API is
 * reachable, the AI provider configuration bound correctly, and the readiness probe
 * reports the state of the infrastructure containers.
 */
export default function App() {
  const status = useQuery({ queryKey: ["system-status"], queryFn: api.systemStatus });
  const health = useQuery({ queryKey: ["health"], queryFn: api.readiness });

  return (
    <main className="mx-auto max-w-3xl px-6 py-12">
      <header className="mb-10">
        <h1 className="text-3xl font-semibold tracking-tight">TallyBalance AI</h1>
        <p className="mt-2 text-sm text-slate-500">
          Accounts payable automation for Indian vendors — GST, TDS, approvals, and AI
          document intelligence. Phase 1: environment and solution skeleton.
        </p>
      </header>

      <section className="mb-8">
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
          API
        </h2>
        {status.isPending && <p className="text-sm text-slate-400">Checking…</p>}
        {status.isError && (
          <p className="text-sm text-negative">
            {status.error instanceof ApiError
              ? status.error.message
              : "API unreachable. Is the backend running?"}
          </p>
        )}
        {status.data && (
          <dl className="grid grid-cols-2 gap-x-6 gap-y-2 rounded-lg border border-border p-4 text-sm">
            <dt className="text-slate-500">Status</dt>
            <dd className="font-medium text-positive">{status.data.status}</dd>
            <dt className="text-slate-500">Application</dt>
            <dd>{status.data.application}</dd>
            <dt className="text-slate-500">Environment</dt>
            <dd>{status.data.environment}</dd>
            <dt className="text-slate-500">Server time (UTC)</dt>
            <dd className="tabular">{formatIst(status.data.utcNow)}</dd>
          </dl>
        )}
      </section>

      <section className="mb-8">
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
          AI providers
        </h2>
        <p className="mb-3 text-sm text-slate-500">
          All providers are reached through one OpenAI-compatible client. A provider is
          base URL + key + model in configuration; adding one is never a code change.
        </p>
        {status.data && (
          <ul className="flex flex-wrap gap-2">
            {status.data.configuredAiProviders.map((provider) => (
              <li
                key={provider}
                className={
                  provider === status.data.defaultAiProvider
                    ? "rounded-full border border-brand px-3 py-1 text-xs font-medium text-brand"
                    : "rounded-full border border-border px-3 py-1 text-xs text-slate-600"
                }
              >
                {provider}
                {provider === status.data.defaultAiProvider ? " · default" : ""}
              </li>
            ))}
          </ul>
        )}
      </section>

      <section>
        <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">
          Infrastructure readiness
        </h2>
        <p className="mb-3 text-sm text-slate-500">
          Readiness includes SQL Server and Redis. Start the containers with{" "}
          <code className="rounded bg-muted px-1 py-0.5 text-xs">
            docker-compose up -d
          </code>{" "}
          (running on the Podman engine).
        </p>
        {health.isPending && <p className="text-sm text-slate-400">Checking…</p>}
        {health.isError && (
          <p className="text-sm text-negative">Readiness endpoint unreachable.</p>
        )}
        {health.data?.entries && health.data.entries.length > 0 && (
          <ul className="space-y-1 text-sm">
            {health.data.entries.map((entry) => (
              <li key={entry.name} className="flex items-center gap-2">
                <StatusDot status={entry.status} />
                <span className="font-medium">{entry.name}</span>
                <span className="text-slate-400">{entry.status}</span>
              </li>
            ))}
          </ul>
        )}
        {health.data?.entries?.length === 0 && (
          <p className="text-sm text-slate-400">
            No external dependencies registered yet — expected before containers are up.
          </p>
        )}
      </section>
    </main>
  );
}

/**
 * Status indicator that does not rely on colour alone. The word is always present; the
 * dot is reinforcement. A coloured circle on its own would be invisible to a red-green
 * colour-blind user, which in a financial tool is not a theoretical concern.
 */
function StatusDot({ status }: { status: string }) {
  const tone =
    status === "Healthy"
      ? "bg-positive"
      : status === "Degraded"
        ? "bg-pending"
        : "bg-negative";
  return <span className={`inline-block h-2 w-2 rounded-full ${tone}`} aria-hidden />;
}
