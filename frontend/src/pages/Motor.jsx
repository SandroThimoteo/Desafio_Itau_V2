import { useState } from "react";
import { executarCompra, rebalancearDesvio } from "../api/api";
import Alert from "../components/Alert";
import Spinner from "../components/Spinner";
import { fmt, fmtNum, fmtDate } from "../utils";

export default function Motor() {
  const [dataRef, setDataRef]           = useState(new Date().toISOString().split("T")[0]);
  const [tolerancia, setTolerancia]     = useState("2.5");
  const [result, setResult]             = useState(null);
  const [loading, setLoading]           = useState(false);
  const [msg, setMsg]                   = useState(null);

  const handleExecutar = async () => {
    setLoading(true); setMsg(null); setResult(null);
    try {
      const r = await executarCompra(new Date(dataRef).toISOString());
      setResult(r);
      setMsg({ text: "Motor executado com sucesso!", type: "success" });
    } catch (e) {
      setMsg({ text: e.message, type: "error" });
    }
    setLoading(false);
  };

  const handleRebalancear = async () => {
    const tol = parseFloat(tolerancia);
    if (isNaN(tol) || tol < 0 || tol > 100) {
      setMsg({ text: "Tolerância inválida. Informe um valor entre 0 e 100.", type: "error" });
      return;
    }
    setLoading(true); setMsg(null);
    try {
      await rebalancearDesvio(tol);
      setMsg({ text: `Rebalanceamento por desvio executado com tolerância de ${tol}%!`, type: "success" });
    } catch (e) {
      setMsg({ text: e.message, type: "error" });
    }
    setLoading(false);
  };

  return (
    <div>
      <div className="page-header">
        <div className="page-title">Motor de Compra</div>
        <div className="page-sub">Executar compra programada e rebalanceamento de carteiras</div>
      </div>

      {msg && <Alert msg={msg.text} type={msg.type} />}

      <div className="grid-2" style={{ marginBottom: 20 }}>
        <div className="card">
          <div className="card-title">Executar Compra Programada</div>
          <div className="alert info">
            Agrupa aportes de todos os clientes ativos, calcula as ordens de compra consolidadas e distribui os ativos proporcionalmente.
          </div>
          <div className="form-group">
            <label className="form-label">Data de Referência</label>
            <input className="form-input" type="date"
              value={dataRef} onChange={e => setDataRef(e.target.value)} />
          </div>
          <button className="btn btn-primary" onClick={handleExecutar} disabled={loading}>
            {loading ? <Spinner /> : "⚡"} Executar Motor
          </button>
        </div>

        <div className="card">
          <div className="card-title">Rebalanceamento por Desvio</div>
          <div className="alert info">
            Verifica todas as carteiras e rebalanceia aquelas com desvio acima da tolerância configurada em relação à cesta Top Five vigente.
          </div>
          <div className="form-group">
            <label className="form-label">Tolerância de Desvio (%)</label>
            <input className="form-input" type="number" min="0" max="100" step="0.1"
              value={tolerancia} onChange={e => setTolerancia(e.target.value)} />
          </div>
          <button className="btn btn-ghost" onClick={handleRebalancear} disabled={loading}>
            {loading ? <Spinner /> : "⚖️"} Rebalancear por Desvio
          </button>
        </div>
      </div>

      {result && (
        <div className="card">
          <div className="card-title">Resultado — {fmtDate(result.dataExecucao)}</div>

          {/* KPIs reais da API */}
          <div className="grid-4" style={{ marginBottom: 24 }}>
            <div className="stat-card">
              <div className="stat-label">Clientes Processados</div>
              <div className="stat-value">{result.totalClientes}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Total Consolidado</div>
              <div className="stat-value gold">{fmt(result.totalConsolidado)}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Eventos IR Kafka</div>
              <div className="stat-value blue">{result.eventosIRPublicados}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Resíduos na Master</div>
              <div className="stat-value">{result.residuosCustMaster?.length ?? 0}</div>
            </div>
          </div>

          {/* Ordens de compra */}
          {result.ordensCompra?.length > 0 && (
            <>
              <div className="card-title">Ordens de Compra</div>
              <div className="table-wrap" style={{ marginBottom: 24 }}>
                <table>
                  <thead>
                    <tr><th>Ticker</th><th>Qtd Total</th><th>Preço Unit.</th><th>Valor Total</th></tr>
                  </thead>
                  <tbody>
                    {result.ordensCompra.map(o => (
                      <tr key={o.ticker}>
                        <td><span className="ticker">{o.ticker}</span></td>
                        <td className="mono">{fmtNum(o.quantidadeTotal)}</td>
                        <td className="mono">{fmt(o.precoUnitario)}</td>
                        <td className="mono">{fmt(o.valorTotal)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}

          {/* Distribuições */}
          {result.distribuicoes?.length > 0 && (
            <>
              <div className="card-title">Distribuições por Cliente</div>
              <div className="table-wrap" style={{ marginBottom: 24 }}>
                <table>
                  <thead>
                    <tr><th>Cliente</th><th>Valor Aporte</th><th>Ativos Distribuídos</th></tr>
                  </thead>
                  <tbody>
                    {result.distribuicoes.map(d => (
                      <tr key={d.clienteId}>
                        <td><strong>#{d.clienteId}</strong> {d.nome}</td>
                        <td className="mono">{fmt(d.valorAporte)}</td>
                        <td>
                          {d.ativos?.map(a => (
                            <span key={a.ticker} style={{ marginRight: 10 }}>
                              <span className="ticker">{a.ticker}</span>
                              <span style={{ color: "var(--muted)", fontSize: 12, marginLeft: 4 }}>×{a.quantidade}</span>
                            </span>
                          ))}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}

          {/* Resíduos reais */}
          {result.residuosCustMaster?.length > 0 && (
            <>
              <div className="card-title">Resíduos na Custódia Master</div>
              <div className="table-wrap" style={{ marginBottom: 24 }}>
                <table>
                  <thead>
                    <tr><th>Ticker</th><th>Quantidade</th></tr>
                  </thead>
                  <tbody>
                    {result.residuosCustMaster.map(r => (
                      <tr key={r.ticker}>
                        <td><span className="ticker">{r.ticker}</span></td>
                        <td className="mono">{fmtNum(r.quantidade)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}

          {result.mensagem && (
            <div className="alert info" style={{ marginTop: 16 }}>{result.mensagem}</div>
          )}
        </div>
      )}
    </div>
  );
}