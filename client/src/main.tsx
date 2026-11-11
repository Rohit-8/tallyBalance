import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import App from "./App";
import "./index.css";

// Query client configuration carries real decisions, not just boilerplate:
//
// - staleTime 30s: invoice and vendor data changes on human timescales. Refetching on
//   every mount would hammer the API and make navigation feel slow, while 0 would mean
//   a reviewer editing an invoice sees stale totals after a save.
//
// - retry 1, not the default 3: a 4xx is a client or validation error and will fail
//   identically every time. Retrying a 403 three times is wasted load and, for
//   permission errors, produces a confusing delay before the user learns they lack
//   access. Only 5xx/network failures deserve a retry, and one is enough here because
//   the API's own AI and DB layers already retry internally.
//
// - refetchOnWindowFocus false by default: this is a data-entry application. Refiring a
//   query when a user tabs back from a spreadsheet can silently replace values they are
//   mid-edit on. Individual screens that genuinely want live refresh opt in explicitly.
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 30_000,
      retry: (failureCount, error) => {
        const status = (error as { status?: number })?.status;
        if (status !== undefined && status >= 400 && status < 500) return false;
        return failureCount < 1;
      },
      refetchOnWindowFocus: false,
    },
    mutations: {
      // Mutations are never retried automatically. A retried POST /invoices that
      // succeeded but whose response was lost would create a second invoice. The
      // idempotency key (Phase 5) is the correct protection; a blind retry is not.
      retry: 0,
    },
  },
});

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
    </QueryClientProvider>
  </StrictMode>,
);
