import { useState } from "react";
import { getOrdens, getDistribuicoes, getIR } from "../api/api";
import Alert from "../components/Alert";
import Spinner from "../components/Spinner";
import { fmt, fmtNum, fmtDate } from "../utils";

export default function Operacoes() {
  const [tab, setTab] = useState("ordens");
  const [clienteId, setClienteId] = useState("");
  const [inicio, setInicio] = useState("");
  const [fim, setFim] = useState("");
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState(null);

  const buscar = async () => {
    setLoading(true); setMsg(null); setData(null);
    try {
      const params = {};
      if (clienteId) params.clienteId = clienteId;
      if (inicio) params.inicio = new Date(inicio).toISOString();
      if (fim) params.fim = new Date(fim).toISOString();

      let r;
      if (tab === "ordens") r = await getOrdens(params);
      else if (tab === "distribuicoes") r = await getDistribuicoes(params);
      else {
        // Backend de IR aceita: clienteId, tipo, mesReferencia (formato "yyyy-MM")
        const irParams = {};
        if (clienteId) irParams.clienteId = clienteId;
        if (inicio) irParams.mesReferencia = inicio.slice(0, 7); // "2026-02"
        r = await getIR(irParams);
      }
      setData(r);
    } catch (e) {
      setMsg({ text: e.message, type: "error" });
    }
    setLoading(false);
  };

  const changeTab = (t) => { setTab(t); setData(null); setMsg(null); };

  return (
    <div>
      <div className="page-header">
        <div className="page-title">Operações</div>
        <div className="page-sub">Consultar ordens de compra, distribuições e eventos de IR</div>
      </div>

      <div className="tabs">
        <div className={`tab ${tab === "ordens" ? "active" : ""}`} onClick={() => changeTab("ordens")}>Ordens</div>
        <div className={`tab ${tab === "distribuicoes" ? "active" : ""}`} onClick={() => changeTab("distribuicoes")}>Distribuições</div>
        <div className={`tab ${tab === "ir" ? "active" : ""}`} onClick={() => changeTab("ir")}>IR / Kafka</div>
      </div>

      {/* Filtros */}
      <div className="card mb-16">
        <div className="flex gap-12 items-center" style={{ flexWrap: "wrap" }}>
          <div>
            <div className="form-label">ID Cliente (opcional)</div>
            <input className="form-input" style={{ maxWidth: 160 }} placeholder="Todos"
              value={clienteId} onChange={e => setClienteId(e.target.value)} />
          </div>
          <div>
            <div className="form-label">Data Início</div>
            <input className="form-input" type="date" style={{ maxWidth: 170 }}
              value={inicio} onChange={e => setInicio(e.target.value)} />
          </div>
          <div>
            <div className="form-label">Data Fim</div>
            <input className="form-input" type="date" style={{ maxWidth: 170 }}
              value={fim} onChange={e => setFim(e.target.value)} />
          </div>
          <div style={{ marginTop: 18 }}>
            <button className="btn btn-primary" onClick={buscar} disabled={loading}>
              {loading ? <Spinner /> : "🔍"} Filtrar
            </button>
          </div>
        </div>
      </div>

      {msg && <Alert msg={msg.text} type={msg.type} />}

      {data && (
        <div className="card">
          {/* Ordens */}
          {tab === "ordens" && (
            <>
              <div className="card-title">Ordens de Compra</div>
              {Array.isArray(data) && data.length > 0 ? (
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr><th>ID</th><th>Cliente</th><th>Data</th><th>Status</th><th>Itens</th></tr>
                    </thead>
                    <tbody>
                      {data.map((o, i) => (
                        <tr key={i}>
                          <td className="mono">#{o.id ?? i + 1}</td>
                          <td className="mono">#{o.clienteId}</td>
                          <td>{fmtDate(o.dataCriacao ?? o.dataExecucao ?? o.data)}</td>
                          <td>
                            <span className={`badge ${o.status === "Concluida" || o.status === "Executada" ? "green" : "gold"}`}>
                              {o.status ?? (o.executada ? "Executada" : "Pendente")}
                            </span>
                          </td>
                          <td>
                            {o.itens?.map(it => (
                              <span key={it.ticker} style={{ marginRight: 8 }}>
                                <span className="ticker">{it.ticker}</span>
                                <span style={{ color: "var(--muted)", fontSize: 12, marginLeft: 4 }}>×{fmtNum(it.quantidade)}</span>
                              </span>
                            )) ?? <span style={{ color: "var(--muted)" }}>—</span>}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : <div className="empty">Nenhuma ordem encontrada.</div>}
            </>
          )}

          {/* Distribuições */}
          {tab === "distribuicoes" && (
            <>
              <div className="card-title">Distribuições</div>
              {Array.isArray(data) && data.length > 0 ? (
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr><th>Cliente</th><th>Data</th><th>Valor Aporte</th><th>Ativos</th></tr>
                    </thead>
                    <tbody>
                      {data.map((d, i) => (
                        <tr key={i}>
                          <td><strong>#{d.clienteId}</strong> {d.nome}</td>
                          <td>{fmtDate(d.data ?? d.dataDistribuicao)}</td>
                          <td className="mono">
                            {d.valorAporte != null
                              ? fmt(d.valorAporte)
                              : d.valorTotal != null
                                ? fmt(d.valorTotal)
                                : <span style={{ color: "var(--muted)" }}>—</span>}
                          </td>
                          <td>
                            {(d.itens ?? d.ativos)?.map(a => (
                              <span key={a.ticker} style={{ marginRight: 8 }}>
                                <span className="ticker">{a.ticker}</span>
                                <span style={{ color: "var(--muted)", fontSize: 12, marginLeft: 4 }}>×{a.quantidade}</span>
                              </span>
                            )) ?? <span style={{ color: "var(--muted)" }}>—</span>}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : <div className="empty">Nenhuma distribuição encontrada.</div>}
            </>
          )}

          {/* IR */}
          {tab === "ir" && (
            <>
              <div className="card-title">Eventos de IR / Kafka</div>
              {Array.isArray(data) && data.length > 0 ? (
                <div className="table-wrap">
                  <table>
                    <thead>
                      <tr><th>Cliente</th><th>Tipo</th><th>Ticker</th><th>Valor Operação</th><th>Valor IR</th><th>Data</th></tr>
                    </thead>
                    <tbody>
                      {data.map((ir, i) => (
                        <tr key={i}>
                          <td className="mono">#{ir.clienteId}</td>
                          <td><span className="badge blue">{ir.tipo ?? "DEDO_DURO"}</span></td>
                          <td><span className="ticker">{ir.ticker}</span></td>
                          <td className="mono">{fmt(ir.valorOperacao)}</td>
                          <td className="mono" style={{ color: "var(--red)" }}>{fmt(ir.valorIR)}</td>
                          <td>{fmtDate(ir.dataEvento ?? ir.data)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              ) : <div className="empty">Nenhum evento de IR encontrado.</div>}
            </>
          )}
        </div>
      )}
    </div>
  );
}