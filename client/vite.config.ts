import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // The API is proxied in development so the browser talks to one origin and CORS is
    // mostly a non-issue locally. The API still sends CORS headers (for the day the two
    // are served from different hosts), but routing through the proxy means cookies,
    // auth redirects, and the OpenAPI JSON all behave the same in dev as they will
    // behind a reverse proxy in production — which is the configuration people forget
    // to test until it breaks on deploy.
    proxy: {
      "/api": {
        target: "http://localhost:5080",
        changeOrigin: true,
      },
      "/health": {
        target: "http://localhost:5080",
        changeOrigin: true,
      },
    },
  },
});
