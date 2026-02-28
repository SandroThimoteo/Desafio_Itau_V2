using System;
using System.Collections.Generic;

namespace CompraProgramada.Api.DTOs
{
    public class MotorExecutionResponse
    {
        public DateTime DataExecucao { get; set; }
        public int TotalClientes { get; set; }
        public decimal TotalConsolidado { get; set; }
        public List<OrdemCompraDTO> OrdensCompra { get; set; } = new();
        public List<DistribuicaoDTO> Distribuicoes { get; set; } = new();
        public List<ResiduoDTO> ResiduosCustMaster { get; set; } = new();
        public int EventosIRPublicados { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }

    public class OrdemCompraDTO
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal QuantidadeTotal { get; set; }
        public List<DetalheOrdemDTO> Detalhes { get; set; } = new();
        public decimal PrecoUnitario { get; set; }
        public decimal ValorTotal { get; set; }
    }

    public class DetalheOrdemDTO
    {
        public string Tipo { get; set; } = string.Empty;
        public string Ticker { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
    }

    public class DistribuicaoDTO
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public decimal ValorAporte { get; set; }
        public List<AtivoDistribuicaoDTO> Ativos { get; set; } = new();
    }

    public class AtivoDistribuicaoDTO
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
    }

    public class ResiduoDTO
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
    }
}
