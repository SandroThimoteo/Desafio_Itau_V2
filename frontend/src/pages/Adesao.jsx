import { useState } from "react";
import { adesao } from "../api/api";
import Alert from "../components/Alert";
import Spinner from "../components/Spinner";
import { fmt, fmtDate } from "../utils";

// RN-002: valida formato de CPF (apenas dígitos, 11 caracteres)
function validarCPF(cpf) {
  const digits = cpf.replace(/\D/g, "");
  if (digits.length !== 11) return false;
  if (/^(\d)\1+$/.test(digits)) return false; // ex: 111.111.111-11
  let sum = 0;
  for (let i = 0; i < 9; i++) sum += parseInt(digits[i]) * (10 - i);
  let rest = (sum * 10) % 11;
  if (rest === 10 || rest === 11) rest = 0;
  if (rest !== parseInt(digits[9])) return false;
  sum = 0;
  for (let i = 0; i < 10; i++) sum += parseInt(digits[i]) * (11 - i);
  rest = (sum * 10) % 11;
  if (rest === 10 || rest === 11) rest = 0;
  return rest === parseInt(digits[10]);
}

// Formata CPF enquanto digita: 000.000.000-00
function formatarCPF(valor) {
  return valor
    .replace(/\D/g, "")
    .slice(0, 11)
    .replace(/(\d{3})(\d)/, "$1.$2")
    .replace(/(\d{3})(\d)/, "$1.$2")
    .replace(/(\d{3})(\d{1,2})$/, "$1-$2");
}

function FieldError({ msg }) {
  if (!msg) return null;
  return <div style={{ color: "var(--red)", fontSize: 11, marginTop: 4 }}>⚠️ {msg}</div>;
}

export default function Adesao() {
  const [form, setForm] = useState({ nome: "", cpf: "", email: "", valorMensal: "" });
  const [errors, setErrors] = useState({});
  const [loading, setLoading] = useState(false);
  const [msg, setMsg] = useState(null);
  const [result, setResult] = useState(null);

  // Validação de cada campo individualmente
  const validarCampo = (campo, valor) => {
    switch (campo) {
      case "nome":
        if (!valor.trim()) return "Nome é obrigatório.";
        if (valor.trim().length < 3) return "Nome deve ter pelo menos 3 caracteres.";
        return "";
      case "cpf":
        if (!valor) return "CPF é obrigatório.";
        if (!validarCPF(valor)) return "CPF inválido.";
        return "";
      case "email":
        if (!valor) return "E-mail é obrigatório.";
        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(valor)) return "E-mail inválido.";
        return "";
      case "valorMensal":
        if (!valor) return "Valor mensal é obrigatório.";
        if (parseFloat(valor) < 100) return "Valor mínimo é R$ 100,00 (RN-003).";
        return "";
      default:
        return "";
    }
  };

  const handleChange = (campo, valor) => {
    const novoValor = campo === "cpf" ? formatarCPF(valor) : valor;
    setForm({ ...form, [campo]: novoValor });
    // valida em tempo real após o campo ter sido tocado
    if (errors[campo] !== undefined) {
      setErrors({ ...errors, [campo]: validarCampo(campo, novoValor) });
    }
  };

  const handleBlur = (campo) => {
    setErrors({ ...errors, [campo]: validarCampo(campo, form[campo]) });
  };

  const validarTudo = () => {
    const novosErros = {};
    Object.keys(form).forEach(campo => {
      novosErros[campo] = validarCampo(campo, form[campo]);
    });
    setErrors(novosErros);
    return Object.values(novosErros).every(e => e === "");
  };

  const handle = async () => {
    if (!validarTudo()) {
      setMsg({ text: "Corrija os erros antes de continuar.", type: "error" });
      return;
    }
    setLoading(true); setMsg(null);
    try {
      const data = await adesao({
        nome: form.nome.trim(),
        cpf: form.cpf.replace(/\D/g, ""), // envia só dígitos para a API
        email: form.email.trim(),
        valorMensal: parseFloat(form.valorMensal),
      });
      setResult(data);
      setMsg({ text: "Adesão realizada com sucesso!", type: "success" });
      setForm({ nome: "", cpf: "", email: "", valorMensal: "" });
      setErrors({});
    } catch (e) {
      // trata erros vindos do backend
      const texto = e.message.includes("CPF")
        ? "CPF já cadastrado no sistema (RN-002)."
        : e.message;
      setMsg({ text: texto, type: "error" });
    }
    setLoading(false);
  };

  const formValido = Object.values(errors).every(e => e === "") &&
    Object.values(form).every(v => v !== "");

  return (
    <div>
      <div className="page-header">
        <div className="page-title">Nova Adesão</div>
        <div className="page-sub">Cadastrar cliente no plano de Compra Programada</div>
      </div>

      <div className="grid-2" style={{ alignItems: "start" }}>
        <div className="card">
          <div className="card-title">Dados do Cliente</div>
          {msg && <Alert msg={msg.text} type={msg.type} />}

          {/* Nome — RN-001 */}
          <div className="form-group">
            <label className="form-label">Nome Completo *</label>
            <input
              className="form-input"
              placeholder="Ex: João da Silva"
              value={form.nome}
              onChange={e => handleChange("nome", e.target.value)}
              onBlur={() => handleBlur("nome")}
              style={{ borderColor: errors.nome ? "var(--red)" : undefined }}
            />
            <FieldError msg={errors.nome} />
          </div>

          {/* CPF — RN-001 / RN-002 */}
          <div className="form-group">
            <label className="form-label">CPF *</label>
            <input
              className="form-input"
              placeholder="000.000.000-00"
              value={form.cpf}
              onChange={e => handleChange("cpf", e.target.value)}
              onBlur={() => handleBlur("cpf")}
              style={{ borderColor: errors.cpf ? "var(--red)" : undefined }}
            />
            <FieldError msg={errors.cpf} />
          </div>

          {/* Email — RN-001 */}
          <div className="form-group">
            <label className="form-label">E-mail *</label>
            <input
              className="form-input"
              type="email"
              placeholder="joao@email.com"
              value={form.email}
              onChange={e => handleChange("email", e.target.value)}
              onBlur={() => handleBlur("email")}
              style={{ borderColor: errors.email ? "var(--red)" : undefined }}
            />
            <FieldError msg={errors.email} />
          </div>

          {/* Valor Mensal — RN-003 */}
          <div className="form-group">
            <label className="form-label">Valor Mensal de Aporte (R$) *</label>
            <input
              className="form-input"
              type="number"
              placeholder="Mínimo R$ 100,00"
              min="100"
              value={form.valorMensal}
              onChange={e => handleChange("valorMensal", e.target.value)}
              onBlur={() => handleBlur("valorMensal")}
              style={{ borderColor: errors.valorMensal ? "var(--red)" : undefined }}
            />
            <FieldError msg={errors.valorMensal} />
            <div style={{ fontSize: 11, color: "var(--muted)", marginTop: 4 }}>
              Parcela mensal dividida em 3x (dias 5, 15 e 25)
            </div>
          </div>

          <button
            className="btn btn-primary"
            onClick={handle}
            disabled={loading}
            style={{ width: "100%" }}
          >
            {loading ? <Spinner /> : "✅"} Confirmar Adesão
          </button>
        </div>

        {result && (
          <div className="card">
            <div className="card-title">Conta Criada com Sucesso</div>
            <div className="stat-card" style={{ marginBottom: 16 }}>
              <div className="stat-label">Cliente ID</div>
              <div className="stat-value mono"># {result.clienteId}</div>
            </div>
            <table><tbody>
              <tr><td style={{ color: "var(--muted)" }}>Nome</td><td>{result.nome}</td></tr>
              <tr><td style={{ color: "var(--muted)" }}>CPF</td><td className="mono">{result.cpf}</td></tr>
              <tr><td style={{ color: "var(--muted)" }}>E-mail</td><td>{result.email}</td></tr>
              <tr><td style={{ color: "var(--muted)" }}>Aporte Mensal</td><td className="mono">{fmt(result.valorMensal)}</td></tr>
              <tr><td style={{ color: "var(--muted)" }}>Parcela (1/3)</td><td className="mono">{fmt(result.valorMensal / 3)}</td></tr>
              <tr><td style={{ color: "var(--muted)" }}>Conta Gráfica</td><td className="mono">{result.contaGrafica?.numeroConta}</td></tr>
              <tr><td style={{ color: "var(--muted)" }}>Data Adesão</td><td>{fmtDate(result.dataAdesao)}</td></tr>
              <tr><td style={{ color: "var(--muted)" }}>Status</td>
                <td><span className="badge green">● Ativo</span></td></tr>
            </tbody></table>
          </div>
        )}
      </div>
    </div>
  );
}