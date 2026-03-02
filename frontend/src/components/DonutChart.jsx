import { COLORS } from "../utils";

export default function DonutChart({ items }) {
  if (!items?.length) return null;
  const total = items.reduce((s, i) => s + i.percentual, 0);
  let cumulative = 0;
  const r = 60, cx = 70, cy = 70, stroke = 18;
  const circumference = 2 * Math.PI * r;

  return (
    <div className="donut-wrap">
      <div className="pie-container">
        <svg width={140} height={140}>
          {items.map((item, idx) => {
            const pct = item.percentual / total;
            const dash = pct * circumference;
            const offset = circumference - cumulative * circumference;
            cumulative += pct;
            return (
              <circle
                key={item.ticker}
                cx={cx} cy={cy} r={r}
                fill="none"
                stroke={COLORS[idx % COLORS.length]}
                strokeWidth={stroke}
                strokeDasharray={`${dash} ${circumference - dash}`}
                strokeDashoffset={offset}
                style={{ transform: "rotate(-90deg)", transformOrigin: "center" }}
              />
            );
          })}
        </svg>
        <div className="pie-center">
          <div className="pie-center-value">Top 5</div>
          <div className="pie-center-label">Cesta</div>
        </div>
      </div>
      <div className="donut-legend">
        {items.map((item, idx) => (
          <div className="legend-item" key={item.ticker}>
            <div className="legend-dot" style={{ background: COLORS[idx % COLORS.length] }} />
            <span className="legend-name"><span className="ticker">{item.ticker}</span></span>
            <span className="legend-pct">{item.percentual}%</span>
          </div>
        ))}
      </div>
    </div>
  );
}
