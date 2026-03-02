import { useState, useEffect } from "react";
import { getCestaAtual, getCestaHistorico, criarCesta, getContaMaster } from "../api/api";
import Alert from "../components/Alert";
import Spinner from "../components/Spinner";
import DonutChart from "../components/DonutChart";
import { fmt, fmtNum, fmtDate } from "../utils";

function FieldError({ msg }) {
  if (!msg) return null;
  return <div style={{ color: "var(--red)", fontSize: 11, marginTop: 4 }}>⚠️ {msg}</div>;
}

function ContaMasterCard() {
  const [data, setData] = useState(null);
  useEffect(() => {
    getContaMaster().then(setData).catch(() => {});
  }, []);
  if (!data) return <div className="empty">Carregando...</div>;
  return (
    <>
      <div className="stat-card" style={{ marginBottom: 12 }}>
        <div className="stat-label">Valor Total Resíduo</div>
        <div className="stat-value gold">{fmt(data.valorTotalResiduo)}</div>
      </div>
      <div className="table-wrap">
        <table>
          <thead><tr><th>Ticker</th><th>Qtd</th><th>Preço Médio</th><th>Valor Atual</th></tr></thead>
          <tbody>
            {data.custodia?.length
              ? data.custodia.map(c => (
                <tr key={c.ticker}>
                  <td><span className="ticker">{c.ticker}</span></td>
                  <td className="mono">{fmtNum(c.quantidade)}</td>
                  <td className="mono">{fmt(c.precoMedio)}</td>
                  <td className="mono">{fmt(c.valorAtual)}</td>
                </tr>
              ))
              : <tr><td colSpan={4} className="empty">Sem resíduos na custódia master</td></tr>
            }
          </tbody>
        </table>
      </div>
    </>
  );
}

export default function CestaAdmin() {
  const [tab, setTab] = useState("atual");
  const [atual, setAtual] = useState(null);
  const [historico, setHistorico] = useState([]);
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState(null);

  const [formNome, setFormNome] = useState("");
  const [itens, setItens] = useState(
    Array(5).fill(null).map(() => ({ ticker: "", percentual: "" }))
  );
  const [erros, setErros] = useState({});

  useEffect(() => {
    getCestaAtual().then(setAtual).catch(() => {});
    getCestaHistorico()
      .then(res => {
        // A API pode retornar o array direto ou dentro de uma propriedade
        const lista = Array.isArray(res) ? res : (res?.cestas ?? res?.data ?? res?.historico ?? []);
        setHistorico(lista);
      })
      .catch(() => {});
  }, []);

  const totalPct = itens.reduce((s, i) => s + (parseFloat(i.percentual) || 0), 0);

  // Validação completa do formulário de nova cesta
  const validar = () => {
    const e = {};

    // RN-014: nome obrigatório
    if (!formNome.trim()) e.nome = "Nome da cesta é obrigatório.";

    itens.forEach((item, idx) => {
      // RN-014: todos os 5 tickers obrigatórios
      if (!item.ticker.trim()) {
        e[`ticker_${idx}`] = "Ticker obrigatório.";
      } else if (!/^[A-Za-z]{4}\d{1,2}$/.test(item.ticker.trim())) {
        e[`ticker_${idx}`] = "Formato inválido. Ex: PETR4";
      }

      const pct = parseFloat(item.percentual);
      // RN-016: percentual > 0
      if (!item.percentual) {
        e[`pct_${idx}`] = "Percentual obrigatório.";
      } else if (isNaN(pct) || pct <= 0) {
        e[`pct_${idx}`] = "Deve ser maior que 0% (RN-016).";
      }
    });

    // RN-015: soma = 100%
    if (Object.keys(e).filter(k => k.startsWith("pct_")).length === 0 && totalPct !== 100) {
      e.total = `A soma dos percentuais deve ser exatamente 100%. Atual: ${totalPct}% (RN-015).`;
    }

    // tickers duplicados
    const tickers = itens.map(i => i.ticker.trim().toUpperCase()).filter(Boolean);
    const duplicados = tickers.filter((t, i) => tickers.indexOf(t) !== i);
    if (duplicados.length > 0) {
      e.duplicados = `Tickers duplicados: ${[...new Set(duplicados)].join(", ")}`;
    }

    setErros(e);
    return Object.keys(e).length === 0;
  };

  const handleCriar = async () => {
    if (!validar()) return;
    setLoading(true); setMsg(null);
    try {
      await criarCesta({
        nome: formNome.trim(),
        itens: itens.map((i) => ({
          ticker: i.ticker.trim().toUpperCase(),
          percentual: parseFloat(i.percentual),
        })),
      });
      setMsg({ text: "Cesta criada! O rebalanceamento foi disparado automaticamente.", type: "success" });
      const [a, h] = await Promise.all([getCestaAtual(), getCestaHistorico()]);
      setAtual(a);
      const lista = Array.isArray(h) ? h : (h?.cestas ?? h?.data ?? h?.historico ?? []);
      setHistorico(lista);
      setFormNome("");
      setItens(Array(5).fill(null).map(() => ({ ticker: "", percentual: "" })));
      setErros({});
    } catch (e) {
      setMsg({ text: e.message, type: "error" });
    }
    setLoading(false);
  };

  const updateItem = (idx, field, value) => {
    const n = [...itens];
    n[idx] = { ...n[idx], [field]: field === "ticker" ? value.toUpperCase() : value };
    setItens(n);
    // limpa erro do campo ao editar
    const key = field === "ticker" ? `ticker_${idx}` : `pct_${idx}`;
    if (erros[key]) setErros({ ...erros, [key]: "" });
  };

  const totalColor = totalPct === 100 ? "var(--green)" : totalPct > 100 ? "var(--red)" : "var(--muted)";
  const totalLabel = totalPct === 100
    ? "✅ Total: 100%"
    : totalPct > 100
    ? `⚠️ Total: ${totalPct}% (excede 100%)`
    : `Total: ${totalPct}% (faltam ${(100 - totalPct).toFixed(1)}%)`;

  return (
    <div>
      <div className="page-header">
        <div className="page-title">Cesta Top Five</div>
        <div className="page-sub">Gerenciar carteira recomendada de ações</div>
      </div>

      <div className="tabs">
        <div className={`tab ${tab === "atual" ? "active" : ""}`} onClick={() => setTab("atual")}>Cesta Atual</div>
        <div className={`tab ${tab === "nova" ? "active" : ""}`} onClick={() => setTab("nova")}>Nova Cesta</div>
        <div className={`tab ${tab === "historico" ? "active" : ""}`} onClick={() => setTab("historico")}>Histórico</div>
      </div>

      {tab === "atual" && (
        <div className="grid-2" style={{ alignItems: "start" }}>
          <div className="card">
            <div className="card-title">Composição Atual</div>
            {atual ? (
              <>
                <div className="flex items-center gap-12 mb-16">
                  <span style={{ fontWeight: 600 }}>{atual.nome}</span>
                  <span className="badge green">● Ativa</span>
                  <span style={{ color: "var(--muted)", fontSize: 12 }}>{fmtDate(atual.dataCriacao)}</span>
                </div>
                <DonutChart items={atual.itens} />
                <hr className="divider" />
                <div className="table-wrap">
                  <table>
                    <thead><tr><th>Ação</th><th>Percentual</th><th>Cotação</th></tr></thead>
                    <tbody>
                      {atual.itens?.map(i => (
                        <tr key={i.ticker}>
                          <td><span className="ticker">{i.ticker}</span></td>
                          <td className="mono">{i.percentual}%</td>
                          <td className="mono">{i.cotacaoAtual ? fmt(i.cotacaoAtual) : "—"}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </>
            ) : <div className="empty">Nenhuma cesta ativa.</div>}
          </div>
          <div className="card">
            <div className="card-title">Conta Master — Resíduos</div>
            <ContaMasterCard />
          </div>
        </div>
      )}

      {tab === "nova" && (
        <div className="card" style={{ maxWidth: 560 }}>
          <div className="card-title">Criar Nova Cesta</div>
          {msg && <Alert msg={msg.text} type={msg.type} />}
          <div className="alert info">
            Ao criar uma nova cesta, o rebalanceamento será disparado automaticamente (RN-019).
            Apenas uma cesta pode estar ativa por vez (RN-018).
          </div>

          {/* Nome */}
          <div className="form-group">
            <label className="form-label">Nome da Cesta *</label>
            <input
              className="form-input"
              placeholder="Ex: Top Five Q2 2026"
              value={formNome}
              onChange={e => {
                setFormNome(e.target.value);
                if (erros.nome) setErros({ ...erros, nome: "" });
              }}
              style={{ borderColor: erros.nome ? "var(--red)" : undefined }}
            />
            <FieldError msg={erros.nome} />
          </div>

          {/* Itens — RN-014, RN-015, RN-016 */}
          <div style={{ marginBottom: 8, fontSize: 12, color: "var(--muted)" }}>
            Exatamente 5 ações com percentuais maiores que 0% (RN-014, RN-016)
          </div>

          {itens.map((item, idx) => (
            <div key={idx} style={{ marginBottom: 12 }}>
              <div className="flex gap-8 items-center">
                <span style={{ color: "var(--muted)", fontSize: 13, width: 18, textAlign: "right", flexShrink: 0 }}>
                  {idx + 1}.
                </span>
                <div style={{ flex: 1 }}>
                  <input
                    className="form-input"
                    placeholder="PETR4"
                    value={item.ticker}
                    onChange={e => updateItem(idx, "ticker", e.target.value)}
                    style={{ borderColor: erros[`ticker_${idx}`] ? "var(--red)" : undefined }}
                  />
                  <FieldError msg={erros[`ticker_${idx}`]} />
                </div>
                <div style={{ width: 90, flexShrink: 0 }}>
                  <input
                    className="form-input"
                    type="number"
                    placeholder="%"
                    min="0.01"
                    max="99.99"
                    value={item.percentual}
                    onChange={e => updateItem(idx, "percentual", e.target.value)}
                    style={{ borderColor: erros[`pct_${idx}`] ? "var(--red)" : undefined }}
                  />
                  <FieldError msg={erros[`pct_${idx}`]} />
                </div>
              </div>
            </div>
          ))}

          {/* Total */}
          <div style={{ fontSize: 13, marginBottom: 4, color: totalColor, fontWeight: 600 }}>
            {totalLabel}
          </div>
          {erros.total && <FieldError msg={erros.total} />}
          {erros.duplicados && <FieldError msg={erros.duplicados} />}

          <button
            className="btn btn-gold mt-16"
            onClick={handleCriar}
            disabled={loading}
            style={{ width: "100%" }}
          >
            {loading ? <Spinner /> : "🗃️"} Criar Cesta
          </button>
        </div>
      )}

      {tab === "historico" && (
        <div className="card">
          <div className="card-title">Histórico de Cestas</div>
          <div className="table-wrap">
            <table>
              <thead>
                <tr><th>ID</th><th>Nome</th><th>Status</th><th>Criação</th><th>Desativação</th><th>Ações</th></tr>
              </thead>
              <tbody>
                {historico?.map(c => (
                  <tr key={c.cestaId}>
                    <td className="mono">#{c.cestaId}</td>
                    <td>{c.nome}</td>
                    <td><span className={`badge ${c.ativa ? "green" : "red"}`}>{c.ativa ? "Ativa" : "Inativa"}</span></td>
                    <td>{fmtDate(c.dataCriacao)}</td>
                    <td>{c.dataDesativacao ? fmtDate(c.dataDesativacao) : "—"}</td>
                    <td>
                      {c.itens?.map(i => (
                        <span className="ticker" style={{ marginRight: 4 }} key={i.ticker}>{i.ticker}</span>
                      ))}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}