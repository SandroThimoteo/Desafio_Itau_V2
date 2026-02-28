using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace CompraProgramada.Api.Services
{
    public class MotorAgendamentoStatusSnapshot
    {
        public DateTime DataConsultaUtc { get; set; }
        public DateTime? UltimaExecucaoEmUtc { get; set; }
        public DateTime? UltimaDataReferenciaUtc { get; set; }
        public string UltimoResultado { get; set; } = "NUNCA_EXECUTADO";
        public string? UltimoErro { get; set; }
        public int TotalExecucoesSucesso { get; set; }
        public int TotalExecucoesFalha { get; set; }
        public List<string> CiclosExecutados { get; set; } = new List<string>();
    }

    public class MotorAgendamentoStatusStore
    {
        private readonly object _sync = new object();
        private readonly HashSet<string> _ciclosExecutados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly string _filePath;

        private DateTime? _ultimaExecucaoEmUtc;
        private DateTime? _ultimaDataReferenciaUtc;
        private string _ultimoResultado = "NUNCA_EXECUTADO";
        private string? _ultimoErro;
        private int _totalExecucoesSucesso;
        private int _totalExecucoesFalha;

        public MotorAgendamentoStatusStore()
            : this(Path.Combine(AppContext.BaseDirectory, "motor-agendamento-state.json"))
        {
        }

        public MotorAgendamentoStatusStore(string filePath)
        {
            _filePath = filePath;
            CarregarEstado();
        }

        public ISet<string> ObterCiclosExecutados()
        {
            lock (_sync)
            {
                return new HashSet<string>(_ciclosExecutados, StringComparer.OrdinalIgnoreCase);
            }
        }

        public void RegistrarSucesso(string chaveCiclo, DateTime dataReferenciaUtc)
        {
            lock (_sync)
            {
                _ciclosExecutados.Add(chaveCiclo);
                _ultimaExecucaoEmUtc = DateTime.UtcNow;
                _ultimaDataReferenciaUtc = dataReferenciaUtc;
                _ultimoResultado = "SUCESSO";
                _ultimoErro = null;
                _totalExecucoesSucesso++;
                SalvarEstado();
            }
        }

        public void RegistrarFalha(DateTime dataReferenciaUtc, string erro)
        {
            lock (_sync)
            {
                _ultimaExecucaoEmUtc = DateTime.UtcNow;
                _ultimaDataReferenciaUtc = dataReferenciaUtc;
                _ultimoResultado = "FALHA";
                _ultimoErro = erro;
                _totalExecucoesFalha++;
                SalvarEstado();
            }
        }

        public MotorAgendamentoStatusSnapshot ObterSnapshot()
        {
            lock (_sync)
            {
                return new MotorAgendamentoStatusSnapshot
                {
                    DataConsultaUtc = DateTime.UtcNow,
                    UltimaExecucaoEmUtc = _ultimaExecucaoEmUtc,
                    UltimaDataReferenciaUtc = _ultimaDataReferenciaUtc,
                    UltimoResultado = _ultimoResultado,
                    UltimoErro = _ultimoErro,
                    TotalExecucoesSucesso = _totalExecucoesSucesso,
                    TotalExecucoesFalha = _totalExecucoesFalha,
                    CiclosExecutados = _ciclosExecutados.OrderBy(c => c).ToList()
                };
            }
        }

        private void CarregarEstado()
        {
            lock (_sync)
            {
                if (!File.Exists(_filePath))
                    return;

                try
                {
                    var json = File.ReadAllText(_filePath);
                    var estado = JsonSerializer.Deserialize<MotorAgendamentoStatusSnapshot>(json);
                    if (estado == null)
                        return;

                    _ultimaExecucaoEmUtc = estado.UltimaExecucaoEmUtc;
                    _ultimaDataReferenciaUtc = estado.UltimaDataReferenciaUtc;
                    _ultimoResultado = string.IsNullOrWhiteSpace(estado.UltimoResultado) ? "NUNCA_EXECUTADO" : estado.UltimoResultado;
                    _ultimoErro = estado.UltimoErro;
                    _totalExecucoesSucesso = estado.TotalExecucoesSucesso;
                    _totalExecucoesFalha = estado.TotalExecucoesFalha;

                    _ciclosExecutados.Clear();
                    foreach (var ciclo in estado.CiclosExecutados ?? new List<string>())
                    {
                        _ciclosExecutados.Add(ciclo);
                    }
                }
                catch
                {
                    _ciclosExecutados.Clear();
                    _ultimaExecucaoEmUtc = null;
                    _ultimaDataReferenciaUtc = null;
                    _ultimoResultado = "NUNCA_EXECUTADO";
                    _ultimoErro = null;
                    _totalExecucoesSucesso = 0;
                    _totalExecucoesFalha = 0;
                }
            }
        }

        private void SalvarEstado()
        {
            var snapshot = new MotorAgendamentoStatusSnapshot
            {
                DataConsultaUtc = DateTime.UtcNow,
                UltimaExecucaoEmUtc = _ultimaExecucaoEmUtc,
                UltimaDataReferenciaUtc = _ultimaDataReferenciaUtc,
                UltimoResultado = _ultimoResultado,
                UltimoErro = _ultimoErro,
                TotalExecucoesSucesso = _totalExecucoesSucesso,
                TotalExecucoesFalha = _totalExecucoesFalha,
                CiclosExecutados = _ciclosExecutados.OrderBy(c => c).ToList()
            };

            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = true });

            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json);

            if (File.Exists(_filePath))
                File.Replace(tempPath, _filePath, null);
            else
                File.Move(tempPath, _filePath);
        }
    }
}
