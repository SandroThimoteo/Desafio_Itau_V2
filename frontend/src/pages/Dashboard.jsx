import { useState, useEffect } from "react";
import { getCestaAtual, getContaMaster } from "../api/api";
import { fmt, fmtDate } from "../utils";
import Spinner from "../components/Spinner";

export default function Dashboard() {
  const [cesta, setCesta]   = useState(null);
  const [master, setMaster] = useState(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    Promise.all([
      getCestaAtual().catch(() => null),
      getContaMaster().catch(() => null),
    ]).then(([c, m]) => {
      setCesta(c);
      setMaster(m);
      setLoading(false);
    });
  }, []);

  if (loading) return (
    <div>
      <div className="page-header">
        <div className="page-title">Dashboard</div>
        <div className="page-sub">Visão geral do sistema</div>
      </div>
      <div className="card" style={{ textAlign: "center", padding: 40 }}><Spinner /></div>
    </div>
  );

  return (
    <div>
      <div className="page-header">
        <div className="page-title">Dashboard</div>
        <div className="page-sub">Visão geral do sistema — Itaú Corretora</div>
      </div>

      {/* Cards de status */}
      <div className="grid-3" style={{ marginBottom: 20 }}>
        <div className="stat-card">
          <div className="stat-label">Cesta Ativa</div>
          <div className="stat-value" style={{ fontSize: 16, marginTop: 4 }}>
            {cesta ? cesta.nome : <span style={{ color: "var(--muted)", fontSize: 14 }}>Nenhuma</span>}
          </div>
          {cesta && <div style={{ fontSize: 11, color: "var(--muted)", marginTop: 6 }}>Criada em {fmtDate(cesta.dataCriacao)}</div>}
        </div>

        <div className="stat-card">
          <div className="stat-label">Ativos na Cesta</div>
          <div className="stat-value blue">{cesta?.itens?.length ?? "—"}</div>
        </div>

        <div className="stat-card">
          <div className="stat-label">Resíduo Total Master</div>
          <div className="stat-value gold">{master ? fmt(master.valorTotalResiduo) : "—"}</div>
        </div>
      </div>

      <div className="grid-2" style={{ alignItems: "start" }}>
        {/* Composição da cesta */}
        <div className="card">
          <div className="card-title">Composição da Cesta Top Five</div>
          {cesta?.itens?.length ? (
            <table>
              <thead>
                <tr><th>Ação</th><th style={{ textAlign: "right" }}>Percentual</th><th style={{ textAlign: "right" }}>Cotação</th></tr>
              </thead>
              <tbody>
                {cesta.itens.map(i => (
                  <tr key={i.ticker}>
                    <td><span className="ticker">{i.ticker}</span></td>
                    <td className="mono" style={{ textAlign: "right" }}>{i.percentual}%</td>
                    <td className="mono" style={{ textAlign: "right" }}>{i.cotacaoAtual ? fmt(i.cotacaoAtual) : "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <div className="empty">Nenhuma cesta ativa. Configure uma no Painel Admin.</div>
          )}
        </div>

        {/* Custódia master */}
        <div className="card">
          <div className="card-title">Custódia Master — Resíduos</div>
          {master?.custodia?.length ? (
            <table>
              <thead>
                <tr><th>Ticker</th><th style={{ textAlign: "right" }}>Qtd</th><th style={{ textAlign: "right" }}>Valor Atual</th></tr>
              </thead>
              <tbody>
                {master.custodia.map(c => (
                  <tr key={c.ticker}>
                    <td><span className="ticker">{c.ticker}</span></td>
                    <td className="mono" style={{ textAlign: "right" }}>{c.quantidade}</td>
                    <td className="mono" style={{ textAlign: "right" }}>{fmt(c.valorAtual)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          ) : (
            <div className="empty">Sem resíduos na custódia master.</div>
          )}
        </div>
      </div>
    </div>
  );
}