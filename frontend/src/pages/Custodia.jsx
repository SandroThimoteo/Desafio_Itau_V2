import { useState, useEffect } from "react";
import { getContaMaster, getCarteira } from "../api/api";
import Alert from "../components/Alert";
import Spinner from "../components/Spinner";
import { fmt, fmtNum, fmtPct } from "../utils";

function CustodiaMaster() {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(true);
  const [msg, setMsg] = useState(null);

  useEffect(() => {
    getContaMaster()
      .then(setData)
      .catch(e => setMsg({ text: e.message, type: "error" }))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="flex items-center gap-8" style={{ color: "var(--muted)", fontSize: 13 }}><Spinner /> Carregando...</div>;
  if (msg) return <Alert msg={msg.text} type={msg.type} />;
  if (!data) return <div className="empty">Sem dados.</div>;

  return (
    <>
      <div className="grid-3" style={{ marginBottom: 20 }}>
        <div className="stat-card">
          <div className="stat-label">Número da Conta</div>
          <div className="stat-value mono" style={{ fontSize: 16 }}>{data.contaMaster?.numeroConta}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Tipo</div>
          <div className="stat-value mono" style={{ fontSize: 16 }}>{data.contaMaster?.tipo}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Valor Total Resíduo</div>
          <div className="stat-value gold">{fmt(data.valorTotalResiduo)}</div>
        </div>
      </div>

      <div className="card">
        <div className="card-title">Posição de Ativos — Custódia Master</div>
        {data.custodia?.length ? (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Ticker</th>
                  <th>Quantidade</th>
                  <th>Preço Médio</th>
                  <th>Valor Atual</th>
                  <th>Origem</th>
                </tr>
              </thead>
              <tbody>
                {data.custodia.map(c => (
                  <tr key={c.ticker}>
                    <td><span className="ticker">{c.ticker}</span></td>
                    <td className="mono">{fmtNum(c.quantidade)}</td>
                    <td className="mono">{fmt(c.precoMedio)}</td>
                    <td className="mono" style={{ color: "var(--gold)" }}>{fmt(c.valorAtual)}</td>
                    <td><span className="badge blue">{c.origem ?? "Resíduo"}</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="empty">
            <div style={{ fontSize: 28, marginBottom: 8 }}>✅</div>
            Nenhum resíduo na custódia master. Toda a posição foi distribuída.
          </div>
        )}
      </div>
    </>
  );
}

function CustodiaFilhote() {
  const [clienteId, setClienteId] = useState("");
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState(null);

  const buscar = async () => {
    if (!clienteId) return;
    setLoading(true); setMsg(null); setData(null);
    try {
      const r = await getCarteira(clienteId);
      setData(r);
    } catch (e) {
      setMsg({ text: e.message, type: "error" });
    }
    setLoading(false);
  };

  const plColor = (v) => (v >= 0 ? "var(--green)" : "var(--red)");

  return (
    <>
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
          <div className="flex items-center gap-12 mb-16">
            <span style={{ fontSize: 17, fontWeight: 700 }}>{data.nome}</span>
            <span className="badge blue">{data.contaGrafica}</span>
            <span style={{ color: "var(--muted)", fontSize: 12 }}>
              Consulta: {new Date(data.dataConsulta).toLocaleString("pt-BR")}
            </span>
          </div>

          <div className="grid-4" style={{ marginBottom: 20 }}>
            <div className="stat-card">
              <div className="stat-label">Total Investido</div>
              <div className="stat-value mono" style={{ fontSize: 18 }}>{fmt(data.resumo?.valorTotalInvestido)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Valor Atual</div>
              <div className="stat-value gold" style={{ fontSize: 18 }}>{fmt(data.resumo?.valorAtualCarteira)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">P/L Total</div>
              <div className="stat-value" style={{ fontSize: 18, color: plColor(data.resumo?.plTotal) }}>
                {fmt(data.resumo?.plTotal)}
              </div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Rentabilidade</div>
              <div className="stat-value" style={{ fontSize: 18, color: plColor(data.resumo?.rentabilidadePercentual) }}>
                {fmtPct(data.resumo?.rentabilidadePercentual)}
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-title">Posição de Ativos — Custódia Filhote</div>
            {data.ativos?.length ? (
              <div className="table-wrap">
                <table>
                  <thead>
                    <tr>
                      <th>Ticker</th>
                      <th>Quantidade</th>
                      <th>Preço Médio</th>
                      <th>Cotação Atual</th>
                      <th>Valor Atual</th>
                      <th>P/L</th>
                      <th>P/L %</th>
                      <th>% Carteira</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.ativos.map(a => (
                      <tr key={a.ticker}>
                        <td><span className="ticker">{a.ticker}</span></td>
                        <td className="mono">{fmtNum(a.quantidade)}</td>
                        <td className="mono">{fmt(a.precoMedio)}</td>
                        <td className="mono">{fmt(a.cotacaoAtual)}</td>
                        <td className="mono" style={{ color: "var(--gold)" }}>{fmt(a.valorAtual)}</td>
                        <td>
                          <span className={`badge ${a.pl >= 0 ? "green" : "red"}`}>
                            {fmt(a.pl)}
                          </span>
                        </td>
                        <td>
                          <span className={`badge ${a.plPercentual >= 0 ? "green" : "red"}`}>
                            {fmtPct(a.plPercentual)}
                          </span>
                        </td>
                        <td>
                          <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
                            <div style={{
                              height: 6, width: `${Math.min(a.composicaoCarteira, 100)}%`,
                              maxWidth: 80, background: "var(--blue)", borderRadius: 3,
                              minWidth: 4
                            }} />
                            <span className="mono" style={{ fontSize: 12 }}>
                              {a.composicaoCarteira?.toFixed(1)}%
                            </span>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="empty">Nenhum ativo na custódia deste cliente.</div>
            )}
          </div>
        </>
      )}
    </>
  );
}

export default function Custodia() {
  const [tab, setTab] = useState("master");

  return (
    <div>
      <div className="page-header">
        <div className="page-title">Custódia</div>
        <div className="page-sub">Posição de ativos na conta master e nas contas filhotes</div>
      </div>

      <div className="tabs">
        <div className={`tab ${tab === "master" ? "active" : ""}`} onClick={() => setTab("master")}>
          🏦 Custódia Master
        </div>
        <div className={`tab ${tab === "filhote" ? "active" : ""}`} onClick={() => setTab("filhote")}>
          👤 Custódia Filhote
        </div>
      </div>

      {tab === "master" && <CustodiaMaster />}
      {tab === "filhote" && <CustodiaFilhote />}
    </div>
  );
}