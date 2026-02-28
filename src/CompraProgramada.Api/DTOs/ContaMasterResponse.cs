using System;
using System.Collections.Generic;

namespace CompraProgramada.Api.DTOs
{
    public class ContaMasterResponse
    {
        public ContaMasterDTO ContaMaster { get; set; } = new();
        public List<CustodiaItemDTO> Custodia { get; set; } = new();
        public decimal ValorTotalResiduo { get; set; }
    }

    public class ContaMasterDTO
    {
        public long Id { get; set; }
        public string NumeroConta { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
    }

    public class CustodiaItemDTO
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal ValorAtual { get; set; }
        public string Origem { get; set; } = string.Empty;
    }
}
