/**
 * Formatting for money and dates.
 *
 * These helpers exist as a single module because getting them wrong is a correctness
 * bug, not a cosmetic one, and the mistakes are easy to make and hard to see.
 *
 * INR formatting: Indian numbering groups the last three digits then pairs of two
 * (1,23,45,678 rather than 12,34,56,789 — lakh/crore grouping). Doing this with a
 * generic thousands formatter produces a number that looks plausible to a non-Indian
 * reviewer and is simply wrong to every user of this product. Intl handles it correctly
 * when given the "en-IN" locale, which is why the locale is pinned here rather than
 * inherited from the browser: a developer or tester on an en-US machine would otherwise
 * silently see Western grouping and never notice.
 */

const inrFormatter = new Intl.NumberFormat("en-IN", {
  style: "currency",
  currency: "INR",
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

/**
 * Formats a decimal amount as rupees.
 *
 * Takes string | number deliberately. Amounts arrive from the API as JSON numbers today,
 * but the domain uses decimal(19,4) and any client-side arithmetic on them is a
 * floating-point bug waiting to happen. Accepting a string keeps the door open to
 * sending decimals as strings later (the safer wire format) without touching every call
 * site — and this function never performs arithmetic, only formatting.
 */
export function formatINR(amount: string | number | null | undefined): string {
  if (amount === null || amount === undefined) return "—";
  const value = typeof amount === "string" ? Number(amount) : amount;
  if (!Number.isFinite(value)) return "—";
  return inrFormatter.format(value);
}

/**
 * Formats a negative amount the way accounting expects: parentheses, not a leading
 * minus. "(₹1,200.00)" reads as a credit in a ledger; "-₹1,200.00" reads as a mistake.
 * Colour is applied by the caller, and the parentheses mean the meaning survives in
 * greyscale, on a projector, or for a colour-blind reviewer.
 */
export function formatINRAccounting(amount: string | number | null | undefined): string {
  if (amount === null || amount === undefined) return "—";
  const value = typeof amount === "string" ? Number(amount) : amount;
  if (!Number.isFinite(value)) return "—";
  if (value < 0) return `(${inrFormatter.format(Math.abs(value))})`;
  return inrFormatter.format(value);
}

/**
 * Renders an instant in the organization's operating timezone.
 *
 * The API stores and returns UTC. Displaying raw UTC would put every Indian user's
 * "invoice dated today" a day in the past or future depending on the hour, because IST
 * is UTC+5:30 — a half-hour offset that breaks the naive "add the offset hours" trick
 * people reach for. Using the runtime's own Intl support with an explicit IANA zone is
 * the only version that handles the boundary correctly.
 */
const istDateFormatter = new Intl.DateTimeFormat("en-IN", {
  timeZone: "Asia/Kolkata",
  year: "numeric",
  month: "short",
  day: "2-digit",
  hour: "2-digit",
  minute: "2-digit",
  hour12: false,
});

export function formatIst(iso: string | null | undefined): string {
  if (!iso) return "—";
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return "—";
  return `${istDateFormatter.format(date)} IST`;
}
