import { useState } from "react";
import { getCarteira, getRentabilidade } from "../api/api";
import Alert from "../components/Alert";
import Spinner from "../components/Spinner";
import { fmt, fmtPct, fmtNum, fmtDate } from "../utils";

export default function Carteira() {
  const [clienteId, setClienteId] = useState("");
  const [tab, setTab] = useState("carteira");
  const [carteira, setCarteira] = useState(null);
  const [rent, setRent] = useState(null);
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState(null);
  const [ordemAsc, setOrdemAsc] = useState(false); // false = mais recente primeiro

  const buscar = async () => {
    if (!clienteId) return;
    setLoading(true); setMsg(null);
    try {
      const [c, r] = await Promise.all([
        getCarteira(clienteId),
        getRentabilidade(clienteId),
      ]);
      setCarteira(c); setRent(r);
    } catch (e) {
      setMsg({ text: e.message, type: "error" });
      setCarteira(null); setRent(null);
    }
    setLoading(false);
  };

  const plColor = (v) => (v >= 0 ? "green" : "red");

  return (
    <div>
      <div className="page-header">
        <div className="page-title">Carteira do Cliente</div>
        <div className="page-sub">Posição, P/L e rentabilidade</div>
      </div>

      <div className="card mb-16">
        <div className="flex items-center gap-12">
          <input className="form-input" style={{ maxWidth: 200 }} placeholder="ID do Cliente"
            value={clienteId} onChange={e => setClienteId(e.target.value)}
            onKeyDown={e => e.key === "Enter" && buscar()} />
          <button className="btn btn-primary" onClick={buscar} disabled={loading}>
            {loading ? <Spinner /> : "🔍"} Buscar
          </button>
        </div>
      </div>

      {msg && <Alert msg={msg.text} type={msg.type} />}

      {carteira && (
        <>
          <div className="flex items-center gap-12 mb-16">
            <span style={{ fontSize: 18, fontWeight: 700 }}>{carteira.nome}</span>
            <span className="badge blue">{carteira.contaGrafica}</span>
          </div>

          <div className="grid-4" style={{ marginBottom: 20 }}>
            {[
              { label: "Valor Investido", val: fmt(carteira.resumo?.valorTotalInvestido) },
              { label: "Valor Atual", val: fmt(carteira.resumo?.valorAtualCarteira), cls: "gold" },
              { label: "P/L Total", val: fmt(carteira.resumo?.plTotal), cls: plColor(carteira.resumo?.plTotal) },
              { label: "Rentabilidade", val: fmtPct(carteira.resumo?.rentabilidadePercentual), cls: plColor(carteira.resumo?.rentabilidadePercentual) },
            ].map(s => (
              <div className="stat-card" key={s.label}>
                <div className="stat-label">{s.label}</div>
                <div className={`stat-value ${s.cls || ""}`}>{s.val}</div>
              </div>
            ))}
          </div>

          <div className="tabs">
            <div className={`tab ${tab === "carteira" ? "active" : ""}`} onClick={() => setTab("carteira")}>Posição</div>
            <div className={`tab ${tab === "rent" ? "active" : ""}`} onClick={() => setTab("rent")}>Rentabilidade</div>
            <div className={`tab ${tab === "evolucao" ? "active" : ""}`} onClick={() => setTab("evolucao")}>Evolução</div>
          </div>

          {tab === "carteira" && (
            <div className="card">
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Ticker</th><th>Quantidade</th><th>Preço Médio</th>
                      <th>Cotação Atual</th><th>Valor Atual</th><th>P/L</th><th>P/L %</th><th>% Carteira</th>
                    </tr>
                  </thead>
                  <tbody>
                    {carteira.ativos?.map(a => (
                      <tr key={a.ticker}>
                        <td><span className="ticker">{a.ticker}</span></td>
                        <td className="mono">{fmtNum(a.quantidade)}</td>
                        <td className="mono">{fmt(a.precoMedio)}</td>
                        <td className="mono">{fmt(a.cotacaoAtual)}</td>
                        <td className="mono">{fmt(a.valorAtual)}</td>
                        <td><span className={`badge ${a.pl >= 0 ? "green" : "red"}`}>{fmt(a.pl)}</span></td>
                        <td><span className={`badge ${a.plPercentual >= 0 ? "green" : "red"}`}>{fmtPct(a.plPercentual)}</span></td>
                        <td className="mono">{a.composicaoCarteira?.toFixed(1)}%</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {tab === "rent" && rent && (
            <div className="grid-2">
              <div className="card">
                <div className="card-title">Resumo de Rentabilidade</div>
                <table><tbody>
                  <tr><td style={{ color: "var(--muted)" }}>Total Investido</td>
                    <td className="mono">{fmt(rent.rentabilidade?.valorTotalInvestido)}</td></tr>
                  <tr><td style={{ color: "var(--muted)" }}>Valor Atual</td>
                    <td className="mono" style={{ color: "var(--gold)" }}>{fmt(rent.rentabilidade?.valorAtualCarteira)}</td></tr>
                  <tr><td style={{ color: "var(--muted)" }}>P/L Total</td>
                    <td className="mono" style={{ color: rent.rentabilidade?.plTotal >= 0 ? "var(--green)" : "var(--red)" }}>
                      {fmt(rent.rentabilidade?.plTotal)}</td></tr>
                  <tr><td style={{ color: "var(--muted)" }}>Rentabilidade</td>
                    <td className="mono" style={{ color: rent.rentabilidade?.rentabilidadePercentual >= 0 ? "var(--green)" : "var(--red)" }}>
                      {fmtPct(rent.rentabilidade?.rentabilidadePercentual)}</td></tr>
                </tbody></table>
              </div>
              <div className="card">
                <div className="flex items-center justify-between mb-12">
                  <div className="card-title" style={{ marginBottom: 0 }}>Histórico de Aportes</div>
                  <button
                    className="btn"
                    style={{ fontSize: 12, padding: "4px 12px" }}
                    onClick={() => setOrdemAsc(o => !o)}
                  >
                    {ordemAsc ? "⬆️ Mais antigo primeiro" : "⬇️ Mais recente primeiro"}
                  </button>
                </div>
                <div className="table-wrap">
                  <table>
                    <thead><tr><th>Data e Hora</th><th>Parcela</th><th style={{ textAlign: "right" }}>Valor</th></tr></thead>
                    <tbody>
                      {(ordemAsc ? [...(rent.historicoAportes ?? [])] : [...(rent.historicoAportes ?? [])].reverse()).map((a, i) => (
                        <tr key={i}>
                          <td className="mono" style={{ whiteSpace: "nowrap" }}>
                            {a.dataHora ? new Date(a.dataHora).toLocaleString("pt-BR") : a.data}
                          </td>
                          <td><span className="badge blue">{a.parcela}</span></td>
                          <td className="mono" style={{ textAlign: "right" }}>{fmt(a.valor)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            </div>
          )}

          {tab === "evolucao" && rent?.evolucaoCarteira && (() => {
            const lista = [...(rent.evolucaoCarteira ?? [])];
            if (!ordemAsc) lista.reverse();
            return (
              <div className="card">
                <div className="flex items-center justify-between mb-12">
                  <div className="card-title" style={{ marginBottom: 0 }}>Evolução da Carteira</div>
                  <button
                    className="btn"
                    style={{ fontSize: 12, padding: "4px 12px" }}
                    onClick={() => setOrdemAsc(o => !o)}
                  >
                    {ordemAsc ? "⬆️ Mais antigo primeiro" : "⬇️ Mais recente primeiro"}
                  </button>
                </div>
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr>
                        <th>Data e Hora</th>
                        <th style={{ textAlign: "right" }}>Valor Investido</th>
                        <th style={{ textAlign: "right" }}>Valor Carteira</th>
                        <th style={{ textAlign: "right" }}>P/L</th>
                        <th style={{ textAlign: "right" }}>Rentabilidade</th>
                      </tr>
                    </thead>
                    <tbody>
                      {lista.map((e, i) => {
                        const pl = (e.valorCarteira ?? 0) - (e.valorInvestido ?? 0);
                        return (
                          <tr key={i}>
                            <td className="mono" style={{ whiteSpace: "nowrap" }}>
                              {e.dataHora
                                ? new Date(e.dataHora).toLocaleString("pt-BR")
                                : e.data}
                            </td>
                            <td className="mono" style={{ textAlign: "right" }}>{fmt(e.valorInvestido)}</td>
                            <td className="mono" style={{ textAlign: "right", color: "var(--gold)" }}>{fmt(e.valorCarteira)}</td>
                            <td style={{ textAlign: "right" }}>
                              <span className={`badge ${pl >= 0 ? "green" : "red"}`}>{fmt(pl)}</span>
                            </td>
                            <td style={{ textAlign: "right" }}>
                              <span className={`badge ${e.rentabilidade >= 0 ? "green" : "red"}`}>{fmtPct(e.rentabilidade)}</span>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              </div>
            );
          })()}
        </>
      )}
    </div>
  );
}