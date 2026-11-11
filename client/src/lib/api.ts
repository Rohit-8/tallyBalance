/**
 * Typed API client.
 *
 * Deliberately a thin hand-written wrapper over fetch rather than a generated client or
 * axios instance, for Phase 1. The reasons are practical:
 *
 * - One place owns error normalisation, so components never branch on the difference
 *   between a network failure, a 401, and a 422 with validation problems.
 * - The API returns RFC 9457 Problem Details on failure. Unpacking that into something
 *   a form can display is exactly the kind of logic that gets duplicated per call site
 *   when there is no client.
 *
 * When real endpoints arrive in Phase 2, this file grows a generated layer from the
 * OpenAPI document rather than being rewritten — which is why response types live in
 * `types/` and not inline here.
 */

export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  // Declared as `| undefined` rather than merely optional. With
  // exactOptionalPropertyTypes enabled, an optional property `detail?: string` means
  // "may be absent" but NOT "may be present and undefined" — so assigning a possibly
  // undefined value to it is a type error. That strictness is the point: it forces the
  // distinction between "the server sent no detail" and "the server sent detail: null",
  // which matters when deciding whether to show a fallback message.
  readonly detail: string | undefined;
  readonly errors: Record<string, string[]> | undefined;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Request failed with status ${status}`);
    this.name = "ApiError";
    this.status = status;
    this.title = problem.title ?? "Request failed";
    this.detail = problem.detail;
    this.errors = problem.errors;
  }
}

interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    ...init,
    headers: {
      Accept: "application/json",
      ...(init?.body ? { "Content-Type": "application/json" } : {}),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    // Best-effort parse. A gateway or crashed container can return HTML with a 502, and
    // throwing on the JSON parse there would replace the real failure (bad gateway) with
    // an irrelevant one (unexpected token '<').
    let problem: ProblemDetails;
    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      problem = { title: `Request failed with status ${response.status}` };
    }
    throw new ApiError(response.status, problem);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export interface SystemStatus {
  status: string;
  application: string;
  environment: string;
  utcNow: string;
  defaultAiProvider: string | null;
  configuredAiProviders: string[];
}

export interface HealthReport {
  status: string;
  entries?: Array<{ name: string; status: string; description?: string }>;
}

export const api = {
  systemStatus: () => request<SystemStatus>("/api/v1/system/status"),
  readiness: () => request<HealthReport>("/health/ready"),
};
