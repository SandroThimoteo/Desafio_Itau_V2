const BASE_URL = "http://localhost:5000";

async function api(path, method = "GET", body) {
  const res = await fetch(`${BASE_URL}${path}`, {
    method,
    headers: { "Content-Type": "application/json" },
    body: body ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) throw new Error(`Erro ${res.status}: ${res.statusText}`);
  const text = await res.text();
  return text ? JSON.parse(text) : null;
}

// Clientes
export const adesao = (dados) => api("/api/Clientes/adesao", "POST", dados);
export const sairProduto = (clienteId) => api(`/api/Clientes/${clienteId}/saida`, "POST");
export const alterarValorMensal = (clienteId, novoValorMensal) =>
  api(`/api/Clientes/${clienteId}/valor-mensal`, "PUT", { novoValorMensal });
export const getCarteira = (clienteId) => api(`/api/Clientes/${clienteId}/carteira`);
export const getRentabilidade = (clienteId) => api(`/api/Clientes/${clienteId}/rentabilidade`);

// Cesta
export const getCestaAtual = () => api("/api/admin/Cesta/atual");
export const getCestaHistorico = () => api("/api/admin/Cesta/historico");
export const criarCesta = (dados) => api("/api/admin/Cesta", "POST", dados);

// Conta Master
export const getContaMaster = () => api("/api/admin/conta-master/custodia");

// Motor
export const executarCompra = (dataReferencia) =>
  api("/api/motor/executar-compra", "POST", { dataReferencia });
export const getAgendamentoStatus = () => api("/api/motor/agendamento/status");

// Rebalanceamento
export const rebalancearDesvio = (toleranciaPercentual) =>
  api(`/api/rebalance/desvio?toleranciaPercentual=${toleranciaPercentual}`, "POST");
export const rebalancearMudancaCesta = (cestaId, dataReferencia) =>
  api(`/api/rebalance/mudanca-cesta/${cestaId}?dataReferencia=${dataReferencia}`, "POST");

// Operações
export const getOrdens = (params) => api(`/api/operacoes/ordens?${new URLSearchParams(params)}`);
export const getDistribuicoes = (params) => api(`/api/operacoes/distribuicoes?${new URLSearchParams(params)}`);
export const getIR = (params) => api(`/api/operacoes/ir?${new URLSearchParams(params)}`);
