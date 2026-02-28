using System;
using System.Collections.Generic;

namespace CompraProgramada.Domain.Entities
{
    public class Cliente
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal ValorMensal { get; set; }
        public bool Ativo { get; set; } = true;
        public DateTime DataAdesao { get; set; }
        public DateTime? DataSaida { get; set; }

        public ContaGrafica ContaGrafica { get; set; } = new ContaGrafica();
        public Custodia Custodia { get; set; } = new Custodia();

        // Histórico de alterações de valor mensal
        public List<ValorMensalHistorico> HistoricoValores { get; set; } = new List<ValorMensalHistorico>();
    }

    public class ValorMensalHistorico
    {
        public long Id { get; set; }
        public decimal ValorAnterior { get; set; }
        public decimal ValorNovo { get; set; }
        public DateTime DataAlteracao { get; set; }
    }
}
