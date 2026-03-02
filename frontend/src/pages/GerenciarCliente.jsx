import { useState } from "react";
import { alterarValorMensal, sairProduto, getRentabilidade } from "../api/api";
import Alert from "../components/Alert";
import Spinner from "../components/Spinner";
import { fmt, fmtDate } from "../utils";

function FieldError({ msg }) {
  if (!msg) return null;
  return <div style={{ color: "var(--red)", fontSize: 11, marginTop: 4 }}>⚠️ {msg}</div>;
}

export default function GerenciarCliente() {
  const [tab, setTab] = useState("alterar");

  // Alterar valor
  const [clienteIdValor, setClienteIdValor] = useState("");
  const [novoValor, setNovoValor] = useState("");
  const [errosValor, setErrosValor] = useState({});
  const [loadingValor, setLoadingValor] = useState(false);
  const [msgValor, setMsgValor] = useState(null);

  // Saída
  const [clienteIdSaida, setClienteIdSaida] = useState("");
  const [errosSaida, setErrosSaida] = useState({});
  const [loadingSaida, setLoadingSaida] = useState(false);
  const [msgSaida, setMsgSaida] = useState(null);

  // Histórico
  const [clienteIdHist, setClienteIdHist] = useState("");
  const [historico, setHistorico] = useState(null);
  const [loadingHist, setLoadingHist] = useState(false);
  const [msgHist, setMsgHist] = useState(null);

  // ── Alterar Valor ──────────────────────────────────────
  const validarAlterarValor = () => {
    const e = {};
    if (!clienteIdValor) e.clienteId = "ID do cliente é obrigatório.";
    if (!novoValor) e.novoValor = "Novo valor é obrigatório.";
    else if (parseFloat(novoValor) < 100) e.novoValor = "Valor mínimo é R$ 100,00 (RN-003).";
    setErrosValor(e);
    return Object.keys(e).length === 0;
  };

  const handleAlterarValor = async () => {
    if (!validarAlterarValor()) return;
    setLoadingValor(true); setMsgValor(null);
    try {
      const r = await alterarValorMensal(clienteIdValor, parseFloat(novoValor));
      setMsgValor({ text: `Valor alterado: ${fmt(r.valorMensalAnterior)} → ${fmt(r.valorMensalNovo)}`, type: "success" });
      setNovoValor("");
      setErrosValor({});
    } catch (e) {
      setMsgValor({ text: e.message, type: "error" });
    }
    setLoadingValor(false);
  };

  // ── Saída ──────────────────────────────────────────────
  const validarSaida = () => {
    const e = {};
    if (!clienteIdSaida) e.clienteId = "ID do cliente é obrigatório.";
    setErrosSaida(e);
    return Object.keys(e).length === 0;
  };

  const handleSaida = async () => {
    if (!validarSaida()) return;
    if (!confirm(`Confirmar saída do cliente #${clienteIdSaida} do produto?\n\nA posição existente será mantida.`)) return;
    setLoadingSaida(true); setMsgSaida(null);
    try {
      const r = await sairProduto(clienteIdSaida);
      setMsgSaida({ text: r.mensagem || "Cliente saiu do produto com sucesso.", type: "success" });
      setClienteIdSaida("");
      setErrosSaida({});
    } catch (e) {
      const texto = e.message.includes("INATIVO")
        ? "Este cliente já está inativo no sistema."
        : e.message;
      setMsgSaida({ text: texto, type: "error" });
    }
    setLoadingSaida(false);
  };

  // ── Histórico ──────────────────────────────────────────
  const buscarHistorico = async () => {
    if (!clienteIdHist) return;
    setLoadingHist(true); setMsgHist(null); setHistorico(null);
    try {
      const r = await getRentabilidade(clienteIdHist);
      setHistorico(r);
    } catch (e) {
      setMsgHist({ text: e.message, type: "error" });
    }
    setLoadingHist(false);
  };

  return (
    <div>
      <div className="page-header">
        <div className="page-title">Gerenciar Cliente</div>
        <div className="page-sub">Alterar aporte mensal, solicitar saída ou consultar histórico</div>
      </div>

      <div className="tabs">
        <div className={`tab ${tab === "alterar" ? "active" : ""}`} onClick={() => setTab("alterar")}>
          💱 Alterar Valor Mensal
        </div>
        <div className={`tab ${tab === "saida" ? "active" : ""}`} onClick={() => setTab("saida")}>
          🚪 Saída do Produto
        </div>
        <div className={`tab ${tab === "historico" ? "active" : ""}`} onClick={() => setTab("historico")}>
          📋 Histórico de Aportes
        </div>
      </div>

      {/* ── Alterar Valor Mensal ── */}
      {tab === "alterar" && (
        <div className="card" style={{ maxWidth: 480 }}>
          <div className="card-title">Alterar Valor Mensal</div>
          {msgValor && <Alert msg={msgValor.text} type={msgValor.type} />}

          <div className="form-group">
            <label className="form-label">ID do Cliente *</label>
            <input
              className="form-input"
              placeholder="Ex: 1"
              value={clienteIdValor}
              onChange={e => setClienteIdValor(e.target.value)}
              style={{ borderColor: errosValor.clienteId ? "var(--red)" : undefined }}
            />
            <FieldError msg={errosValor.clienteId} />
          </div>

          <div className="form-group">
            <label className="form-label">Novo Valor Mensal (R$) *</label>
            <input
              className="form-input"
              type="number"
              placeholder="Mínimo R$ 100,00"
              min="100"
              value={novoValor}
              onChange={e => {
                setNovoValor(e.target.value);
                if (errosValor.novoValor) {
                  setErrosValor({
                    ...errosValor,
                    novoValor: parseFloat(e.target.value) < 100 ? "Valor mínimo é R$ 100,00 (RN-003)." : ""
                  });
                }
              }}
              style={{ borderColor: errosValor.novoValor ? "var(--red)" : undefined }}
            />
            <FieldError msg={errosValor.novoValor} />
            <div style={{ fontSize: 11, color: "var(--muted)", marginTop: 4 }}>
              RN-012: o novo valor será usado na próxima data de compra (dia 5, 15 ou 25)
            </div>
          </div>

          <button className="btn btn-primary" onClick={handleAlterarValor} disabled={loadingValor}>
            {loadingValor ? <Spinner /> : "💱"} Alterar Valor
          </button>
        </div>
      )}

      {/* ── Saída do Produto ── */}
      {tab === "saida" && (
        <div className="card" style={{ maxWidth: 480 }}>
          <div className="card-title">Saída do Produto</div>
          {msgSaida && <Alert msg={msgSaida.text} type={msgSaida.type} />}

          <div className="alert info">
            <strong>RN-008:</strong> A posição na custódia filhote é mantida após a saída.<br />
            <strong>RN-009:</strong> O cliente não participará de novas compras programadas.<br />
            <strong>RN-010:</strong> O cliente ainda pode consultar sua carteira normalmente.
          </div>

          <div className="form-group">
            <label className="form-label">ID do Cliente *</label>
            <input
              className="form-input"
              placeholder="Ex: 1"
              value={clienteIdSaida}
              onChange={e => {
                setClienteIdSaida(e.target.value);
                if (errosSaida.clienteId) setErrosSaida({});
              }}
              style={{ borderColor: errosSaida.clienteId ? "var(--red)" : undefined }}
            />
            <FieldError msg={errosSaida.clienteId} />
          </div>

          <button className="btn btn-danger" onClick={handleSaida} disabled={loadingSaida}>
            {loadingSaida ? <Spinner /> : "🚪"} Confirmar Saída do Produto
          </button>
        </div>
      )}

      {/* ── Histórico de Aportes (RN-013) ── */}
      {tab === "historico" && (
        <div>
          <div className="card mb-16">
            <div className="flex items-center gap-12">
              <input
                className="form-input"
                style={{ maxWidth: 200 }}
                placeholder="ID do Cliente"
                value={clienteIdHist}
                onChange={e => setClienteIdHist(e.target.value)}
                onKeyDown={e => e.key === "Enter" && buscarHistorico()}
              />
              <button className="btn btn-primary" onClick={buscarHistorico} disabled={loadingHist}>
                {loadingHist ? <Spinner /> : "🔍"} Buscar
              </button>
            </div>
          </div>

          {msgHist && <Alert msg={msgHist.text} type={msgHist.type} />}

          {historico && (
            <>
              {/* Resumo do cliente */}
              <div className="flex items-center gap-12 mb-16">
                <span style={{ fontSize: 17, fontWeight: 700 }}>{historico.nome}</span>
                <span style={{ color: "var(--muted)", fontSize: 12 }}>
                  Cliente #{historico.clienteId}
                </span>
              </div>

              <div className="grid-2" style={{ alignItems: "start" }}>
                {/* Histórico de Aportes */}
                <div className="card">
                  <div className="card-title">Histórico de Aportes — RN-013</div>
                  {historico.historicoAportes?.length ? (
                    <div className="table-wrap">
                      <table>
                        <thead>
                          <tr>
                            <th>Data</th>
                            <th>Parcela</th>
                            <th>Valor</th>
                          </tr>
                        </thead>
                        <tbody>
                          {historico.historicoAportes.map((a, i) => (
                            <tr key={i}>
                              <td>{a.data}</td>
                              <td>
                                <span className="badge blue">{a.parcela}</span>
                              </td>
                              <td className="mono" style={{ color: "var(--gold)" }}>
                                {fmt(a.valor)}
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <div className="empty">Nenhum aporte registrado.</div>
                  )}
                </div>

                {/* Evolução da Carteira */}
                <div className="card">
                  <div className="card-title">Evolução da Carteira</div>
                  {historico.evolucaoCarteira?.length ? (
                    <div className="table-wrap">
                      <table>
                        <thead>
                          <tr>
                            <th>Data</th>
                            <th>Investido</th>
                            <th>Valor Atual</th>
                            <th>Rentabilidade</th>
                          </tr>
                        </thead>
                        <tbody>
                          {historico.evolucaoCarteira.map((e, i) => (
                            <tr key={i}>
                              <td>{e.data}</td>
                              <td className="mono">{fmt(e.valorInvestido)}</td>
                              <td className="mono" style={{ color: "var(--gold)" }}>
                                {fmt(e.valorCarteira)}
                              </td>
                              <td>
                                <span className={`badge ${e.rentabilidade >= 0 ? "green" : "red"}`}>
                                  {e.rentabilidade >= 0 ? "+" : ""}{e.rentabilidade?.toFixed(2)}%
                                </span>
                              </td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  ) : (
                    <div className="empty">Nenhuma evolução registrada.</div>
                  )}
                </div>
              </div>

              {/* Resumo de Rentabilidade */}
              {historico.rentabilidade && (
                <div className="card mt-16">
                  <div className="card-title">Resumo Atual</div>
                  <div className="grid-4">
                    <div className="stat-card">
                      <div className="stat-label">Total Investido</div>
                      <div className="stat-value mono" style={{ fontSize: 18 }}>
                        {fmt(historico.rentabilidade.valorTotalInvestido)}
                      </div>
                    </div>
                    <div className="stat-card">
                      <div className="stat-label">Valor Atual</div>
                      <div className="stat-value gold" style={{ fontSize: 18 }}>
                        {fmt(historico.rentabilidade.valorAtualCarteira)}
                      </div>
                    </div>
                    <div className="stat-card">
                      <div className="stat-label">P/L Total</div>
                      <div className="stat-value" style={{
                        fontSize: 18,
                        color: historico.rentabilidade.plTotal >= 0 ? "var(--green)" : "var(--red)"
                      }}>
                        {fmt(historico.rentabilidade.plTotal)}
                      </div>
                    </div>
                    <div className="stat-card">
                      <div className="stat-label">Rentabilidade</div>
                      <div className="stat-value" style={{
                        fontSize: 18,
                        color: historico.rentabilidade.rentabilidadePercentual >= 0 ? "var(--green)" : "var(--red)"
                      }}>
                        {historico.rentabilidade.rentabilidadePercentual >= 0 ? "+" : ""}
                        {historico.rentabilidade.rentabilidadePercentual?.toFixed(2)}%
                      </div>
                    </div>
                  </div>
                </div>
              )}
            </>
          )}
        </div>
      )}
    </div>
  );
}