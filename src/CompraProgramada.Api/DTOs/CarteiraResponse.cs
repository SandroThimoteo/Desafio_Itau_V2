using System;
using System.Collections.Generic;

namespace CompraProgramada.Api.DTOs
{
    public class CarteiraResponse
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string ContaGrafica { get; set; } = string.Empty;
        public DateTime DataConsulta { get; set; }
        public ResumoCarteiraDTO Resumo { get; set; } = new();
        public List<AtivoCarteiraDTO> Ativos { get; set; } = new();
    }

    public class ResumoCarteiraDTO
    {
        public decimal ValorTotalInvestido { get; set; }
        public decimal ValorAtualCarteira { get; set; }
        public decimal PlTotal { get; set; }
        public decimal RentabilidadePercentual { get; set; }
    }

    public class AtivoCarteiraDTO
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal CotacaoAtual { get; set; }
        public decimal ValorAtual { get; set; }
        public decimal Pl { get; set; }
        public decimal PlPercentual { get; set; }
        public decimal ComposicaoCarteira { get; set; }
    }
}
