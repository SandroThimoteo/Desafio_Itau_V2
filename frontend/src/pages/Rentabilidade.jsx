import { useState } from "react";
import { getRentabilidade } from "../api/api";
import Alert from "../components/Alert";
import Spinner from "../components/Spinner";
import { fmt, fmtPct, fmtDate } from "../utils";

export default function Rentabilidade() {
  const [clienteId, setClienteId] = useState("");
  const [data, setData]           = useState(null);
  const [loading, setLoading]     = useState(false);
  const [msg, setMsg]             = useState(null);
  const [tab, setTab]             = useState("resumo");
  const [ordemAsc, setOrdemAsc]   = useState(false); // false = mais recente primeiro

  const buscar = async () => {
    if (!clienteId) return;
    setLoading(true); setMsg(null); setData(null);
    try {
      const r = await getRentabilidade(clienteId);
      setData(r);
    } catch (e) {
      setMsg({ text: e.message, type: "error" });
    }
    setLoading(false);
  };

  const plColor = (v) => (v >= 0 ? "green" : "red");

  const rent = data?.rentabilidade;

  return (
    <div>
      <div className="page-header">
        <div className="page-title">Rentabilidade</div>
        <div className="page-sub">Acompanhamento detalhado de performance da carteira do cliente</div>
      </div>

      {/* Busca */}
      <div className="card mb-16">
        <div className="flex items-center gap-12">
          <input
            className="form-input"
            style={{ maxWidth: 200 }}
            placeholder="ID do Cliente"
            value={clienteId}
            onChange={e => setClienteId(e.target.value)}
            onKeyDown={e => e.key === "Enter" && buscar()}
          />
          <button className="btn btn-primary" onClick={buscar} disabled={loading}>
            {loading ? <Spinner /> : "🔍"} Buscar
          </button>
        </div>
      </div>

      {msg && <Alert msg={msg.text} type={msg.type} />}

      {data && (
        <>
          {/* Header do cliente */}
          <div className="flex items-center gap-12 mb-16">
            <span style={{ fontSize: 18, fontWeight: 700 }}>{data.nome}</span>
            <span className="badge blue">ID #{data.clienteId}</span>
            <span style={{ fontSize: 12, color: "var(--muted)", marginLeft: "auto" }}>
              Consulta em {fmtDate(data.dataConsulta)}
            </span>
          </div>

          {/* Cards de resumo */}
          <div className="grid-4" style={{ marginBottom: 20 }}>
            <div className="stat-card">
              <div className="stat-label">Total Investido</div>
              <div className="stat-value">{fmt(rent?.valorTotalInvestido)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Valor Atual</div>
              <div className="stat-value gold">{fmt(rent?.valorAtualCarteira)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">P/L Total</div>
              <div className={`stat-value ${plColor(rent?.plTotal)}`}>{fmt(rent?.plTotal)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Rentabilidade</div>
              <div className={`stat-value ${plColor(rent?.rentabilidadePercentual)}`}>
                {fmtPct(rent?.rentabilidadePercentual)}
              </div>
            </div>
          </div>

          {/* Tabs */}
          <div className="tabs">
            <div className={`tab ${tab === "resumo"   ? "active" : ""}`} onClick={() => setTab("resumo")}>Resumo</div>
            <div className={`tab ${tab === "aportes"  ? "active" : ""}`} onClick={() => setTab("aportes")}>Histórico de Aportes</div>
            <div className={`tab ${tab === "evolucao" ? "active" : ""}`} onClick={() => setTab("evolucao")}>Evolução da Carteira</div>
          </div>

          {/* Tab: Resumo */}
          {tab === "resumo" && (
            <div className="grid-2">
              <div className="card">
                <div className="card-title">Indicadores de Performance</div>
                <table>
                  <tbody>
                    {[
                      { label: "Total Investido",    val: fmt(rent?.valorTotalInvestido),           color: "var(--text)" },
                      { label: "Valor Atual",        val: fmt(rent?.valorAtualCarteira),             color: "var(--gold)" },
                      { label: "Lucro / Prejuízo",   val: fmt(rent?.plTotal),                       color: rent?.plTotal >= 0 ? "var(--green)" : "var(--red)" },
                      { label: "Rentabilidade (%)",  val: fmtPct(rent?.rentabilidadePercentual),    color: rent?.rentabilidadePercentual >= 0 ? "var(--green)" : "var(--red)" },
                    ].map(row => (
                      <tr key={row.label}>
                        <td style={{ color: "var(--muted)", paddingLeft: 0 }}>{row.label}</td>
                        <td className="mono" style={{ color: row.color, textAlign: "right" }}>{row.val}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>

              <div className="card">
                <div className="card-title">Resumo dos Aportes</div>
                {data.historicoAportes?.length > 0 ? (
                  <>
                    <div className="flex items-center justify-between mb-16" style={{ fontSize: 13 }}>
                      <span style={{ color: "var(--muted)" }}>Total de aportes</span>
                      <span className="mono" style={{ fontWeight: 600 }}>{data.historicoAportes.length}</span>
                    </div>
                    <div className="flex items-center justify-between mb-16" style={{ fontSize: 13 }}>
                      <span style={{ color: "var(--muted)" }}>Primeiro aporte</span>
                      <span className="mono">{data.historicoAportes[0]?.data}</span>
                    </div>
                    <div className="flex items-center justify-between mb-16" style={{ fontSize: 13 }}>
                      <span style={{ color: "var(--muted)" }}>Último aporte</span>
                      <span className="mono">{data.historicoAportes[data.historicoAportes.length - 1]?.data}</span>
                    </div>
                    <div className="flex items-center justify-between" style={{ fontSize: 13 }}>
                      <span style={{ color: "var(--muted)" }}>Total aportado</span>
                      <span className="mono" style={{ color: "var(--gold)", fontWeight: 600 }}>
                        {fmt(data.historicoAportes.reduce((acc, a) => acc + (a.valor ?? 0), 0))}
                      </span>
                    </div>
                  </>
                ) : (
                  <div className="empty">Nenhum aporte registrado</div>
                )}
              </div>
            </div>
          )}

          {/* Tab: Histórico de Aportes */}
          {tab === "aportes" && (() => {
            const lista = [...(data.historicoAportes ?? [])];
            if (!ordemAsc) lista.reverse();
            // Calcular P/L e rentabilidade individual por aporte (acumulado até aquele ponto)
            const evolucao = data.evolucaoCarteira ?? [];
            return (
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
                {lista.length > 0 ? (
                  <div className="table-wrap">
                    <table>
                      <thead>
                        <tr>
                          <th>Data e Hora</th>
                          <th>Parcela</th>
                          <th style={{ textAlign: "right" }}>Valor Aportado</th>
                          <th style={{ textAlign: "right" }}>Valor Carteira</th>
                          <th style={{ textAlign: "right" }}>Rentabilidade</th>
                        </tr>
                      </thead>
                      <tbody>
                        {lista.map((a, i) => {
                          // encontrar o ponto de evolução correspondente pela parcela original
                          const parcelaNum = parseInt(a.parcela?.split("/")[0]) - 1;
                          const ev = evolucao[parcelaNum];
                          return (
                            <tr key={i}>
                              <td className="mono" style={{ whiteSpace: "nowrap" }}>
                                {a.dataHora
                                  ? new Date(a.dataHora).toLocaleString("pt-BR")
                                  : a.data}
                              </td>
                              <td><span className="badge blue">{a.parcela}</span></td>
                              <td className="mono" style={{ textAlign: "right" }}>{fmt(a.valor)}</td>
                              <td className="mono" style={{ textAlign: "right", color: "var(--gold)" }}>
                                {ev ? fmt(ev.valorCarteira) : "—"}
                              </td>
                              <td style={{ textAlign: "right" }}>
                                {ev
                                  ? <span className={`badge ${ev.rentabilidade >= 0 ? "green" : "red"}`}>
                                      {fmtPct(ev.rentabilidade)}
                                    </span>
                                  : "—"}
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="empty">Nenhum aporte registrado</div>
                )}
              </div>
            );
          })()}

          {/* Tab: Evolução */}
          {tab === "evolucao" && (() => {
            const lista = [...(data.evolucaoCarteira ?? [])];
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
                {lista.length > 0 ? (
                  <div className="table-wrap">
                    <table>
                      <thead>
                        <tr>
                          <th>Data e Hora</th>
                          <th style={{ textAlign: "right" }}>Valor Investido</th>
                          <th style={{ textAlign: "right" }}>Valor da Carteira</th>
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
                                <span className={`badge ${e.rentabilidade >= 0 ? "green" : "red"}`}>
                                  {fmtPct(e.rentabilidade)}
                                </span>
                              </td>
                            </tr>
                          );
                        })}
                      </tbody>
                    </table>
                  </div>
                ) : (
                  <div className="empty">Nenhum dado de evolução disponível</div>
                )}
              </div>
            );
          })()}
        </>
      )}
    </div>
  );
}