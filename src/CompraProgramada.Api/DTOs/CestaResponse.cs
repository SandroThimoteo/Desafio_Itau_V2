using System;
using System.Collections.Generic;

namespace CompraProgramada.Api.DTOs
{
    public class CestaCreateResponse
    {
        public long CestaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativa { get; set; }
        public DateTime DataCriacao { get; set; }
        public List<CestaItemDTO> Itens { get; set; } = new();
        public bool RebalanceamentoDisparado { get; set; }
        public CestaAnteriorDTO? CestaAnteriorDesativada { get; set; }
        public List<string>? AtivosRemovidos { get; set; }
        public List<string>? AtivosAdicionados { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }

    public class CestaAnteriorDTO
    {
        public long CestaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataDesativacao { get; set; }
    }

    public class CestaItemDTO
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
        public decimal? CotacaoAtual { get; set; }
    }

    public class CestaAtualResponse
    {
        public long CestaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativa { get; set; }
        public DateTime DataCriacao { get; set; }
        public List<CestaItemDTO> Itens { get; set; } = new();
    }

    public class CestaHistoricoResponse
    {
        public long CestaId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativa { get; set; }
        public DateTime DataCriacao { get; set; }
        public DateTime? DataDesativacao { get; set; }
        public List<CestaItemDTO> Itens { get; set; } = new();
    }
}
