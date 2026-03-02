const PAGES = {
  dashboard:  { label: "Dashboard",         icon: "📊", section: "geral" },
  adesao:     { label: "Nova Adesão",        icon: "✅", section: "clientes" },
  carteira:   { label: "Carteira",           icon: "📈", section: "clientes" },
  gerenciar:  { label: "Gerenciar Cliente",  icon: "👤", section: "clientes" },
  custodia:   { label: "Custódia",            icon: "🏦", section: "clientes" },
  cesta:      { label: "Cesta Top Five",     icon: "🗃️", section: "admin" },
  motor:      { label: "Motor de Compra",    icon: "⚡", section: "admin" },
  operacoes:  { label: "Operações",          icon: "📋", section: "admin" },
};

const SECTIONS = {
  geral:    "Geral",
  clientes: "Portal do Cliente",
  admin:    "Painel Admin",
};

const css = `
  .sidebar {
    width: 240px; min-height: 100vh; background: var(--bg2);
    border-right: 1px solid var(--border); display: flex; flex-direction: column;
    position: fixed; top: 0; left: 0; z-index: 100;
  }
  .sidebar-logo {
    padding: 24px 20px; border-bottom: 1px solid rgba(255,255,255,0.2);
    display: flex; align-items: center; gap: 10px;
    background: var(--gold);
  }
  .logo-icon {
    width: 36px; height: 36px; background: white; border-radius: 8px;
    display: flex; align-items: center; justify-content: center;
    font-size: 18px; font-weight: 700; color: var(--gold);
  }
  .logo-text { font-size: 13px; font-weight: 700; color: white; line-height: 1.3; }
  .logo-sub { font-size: 10px; color: rgba(255,255,255,0.75); font-weight: 400; }
  .nav { padding: 16px 12px; flex: 1; }
  .nav-section { margin-bottom: 24px; }
  .nav-label {
    font-size: 10px; font-weight: 600; color: var(--muted);
    text-transform: uppercase; letter-spacing: 1px; padding: 0 8px; margin-bottom: 6px;
  }
  .nav-item {
    display: flex; align-items: center; gap: 10px; padding: 9px 10px;
    border-radius: 8px; cursor: pointer; font-size: 13px; font-weight: 500;
    color: var(--muted); transition: all 0.15s; margin-bottom: 2px;
  }
  .nav-item:hover { background: var(--card); color: var(--text); }
  .nav-item.active { background: rgba(0,48,135,0.08); color: var(--blue); box-shadow: inset 2px 0 0 var(--blue); font-weight: 600; }
  .nav-icon { font-size: 16px; width: 20px; text-align: center; }
  .sidebar-footer {
    padding: 16px 20px; border-top: 1px solid rgba(255,255,255,0.2);
    background: var(--gold);
  }
  .sidebar-footer-label { font-size: 11px; color: rgba(255,255,255,0.75); }
  .sidebar-footer-url { font-size: 11px; font-family: var(--mono); color: white; font-weight: 600; }
`;

export default function Sidebar({ page, setPage }) {
  return (
    <>
      <style>{css}</style>
      <aside className="sidebar">
        <div className="sidebar-logo">
          <div className="logo-icon">I</div>
          <div>
            <div className="logo-text">Itaú Corretora</div>
            <div className="logo-sub">Compra Programada</div>
          </div>
        </div>

        <nav className="nav">
          {Object.entries(SECTIONS).map(([sKey, sLabel]) => (
            <div className="nav-section" key={sKey}>
              <div className="nav-label">{sLabel}</div>
              {Object.entries(PAGES)
                .filter(([, v]) => v.section === sKey)
                .map(([key, val]) => (
                  <div
                    key={key}
                    className={`nav-item ${page === key ? "active" : ""}`}
                    onClick={() => setPage(key)}
                  >
                    <span className="nav-icon">{val.icon}</span>
                    {val.label}
                  </div>
                ))}
            </div>
          ))}
        </nav>

        <div className="sidebar-footer">
          <div className="sidebar-footer-label">API</div>
          <div className="sidebar-footer-url">localhost:5000</div>
        </div>
      </aside>
    </>
  );
}