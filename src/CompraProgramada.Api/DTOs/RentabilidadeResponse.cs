using System;
using System.Collections.Generic;

namespace CompraProgramada.Api.DTOs
{
    public class RentabilidadeResponse
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataConsulta { get; set; }
        public RentabilidadeSummaryDTO Rentabilidade { get; set; } = new();
        public List<AporteDTO> HistoricoAportes { get; set; } = new();
        public List<EvolucaoCarteiraDTO> EvolucaoCarteira { get; set; } = new();
    }

    public class RentabilidadeSummaryDTO
    {
        public decimal ValorTotalInvestido { get; set; }
        public decimal ValorAtualCarteira { get; set; }
        public decimal PlTotal { get; set; }
        public decimal RentabilidadePercentual { get; set; }
    }

    public class AporteDTO
    {
        public string Data { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string Parcela { get; set; } = string.Empty;
    }

    public class EvolucaoCarteiraDTO
    {
        public string Data { get; set; } = string.Empty;
        public decimal ValorCarteira { get; set; }
        public decimal ValorInvestido { get; set; }
        public decimal Rentabilidade { get; set; }
    }
}
