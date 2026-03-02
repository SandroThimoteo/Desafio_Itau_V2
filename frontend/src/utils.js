export const fmt = (n) =>
  n?.toLocaleString("pt-BR", { style: "currency", currency: "BRL" }) ?? "—";

export const fmtPct = (n) =>
  n != null ? `${n >= 0 ? "+" : ""}${n.toFixed(2)}%` : "—";

export const fmtNum = (n) =>
  n != null ? n.toLocaleString("pt-BR") : "—";

export const fmtDate = (d) =>
  d ? new Date(d).toLocaleDateString("pt-BR") : "—";

export const COLORS = ["#2563eb", "#f59e0b", "#10b981", "#8b5cf6", "#ef4444", "#06b6d4"];
