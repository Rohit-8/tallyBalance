/** @type {import('tailwindcss').Config} */
export default {
  content: ["./index.html", "./src/**/*.{ts,tsx}"],
  theme: {
    extend: {
      colors: {
        // Semantic tokens rather than raw palette values. shadcn/ui in Phase 3 expects
        // this shape (hsl CSS variables), and defining the tokens now means the UI work
        // later is a drop-in rather than a refactor of every className.
        //
        // Financial UI has one non-negotiable requirement most dashboards ignore:
        // a number's meaning must never depend on colour alone. Accounting staff work
        // long sessions, often on projectors, and colour-blindness is common enough that
        // "red = overdue" is not an accessible design. Negative amounts therefore also
        // carry a sign and parentheses, and these tokens are chosen to stay
        // distinguishable in greyscale.
        surface: "hsl(var(--surface))",
        foreground: "hsl(var(--foreground))",
        muted: "hsl(var(--muted))",
        border: "hsl(var(--border))",
        positive: "hsl(var(--positive))",
        negative: "hsl(var(--negative))",
        pending: "hsl(var(--pending))",
        brand: "hsl(var(--brand))",
      },
      fontFamily: {
        // Tabular figures for anything monetary. Without font-variant-numeric:
        // tabular-nums, digits have proportional widths and a column of rupee amounts
        // does not line up on the decimal — the single most common flaw in home-grown
        // financial tables, and one users notice immediately even when they cannot name it.
        mono: ["ui-monospace", "SFMono-Regular", "Menlo", "monospace"],
      },
    },
  },
  plugins: [],
};
