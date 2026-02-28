using System;
using System.Collections.Generic;

namespace CompraProgramada.Domain.Entities
{
    public class IrDedoDuroEvent
    {
        public long ClienteId { get; set; }
        public string CPF { get; set; } = string.Empty;
        public string Ticker { get; set; } = string.Empty;
        public string TipoOperacao { get; set; } = "COMPRA";
        public decimal Quantidade { get; set; }
        public decimal PrecoUnitario { get; set; }
        public decimal ValorOperacao { get; set; }
        public decimal Aliquota { get; set; } = 0.00005m;
        public decimal ValorIR { get; set; }
        public DateTime DataOperacao { get; set; }
    }

    public class IrVendaEvent
    {
        public long ClienteId { get; set; }
        public string CPF { get; set; } = string.Empty;
        public string MesReferencia { get; set; } = string.Empty; // YYYY-MM
        public decimal TotalVendasMes { get; set; }
        public decimal LucroLiquido { get; set; }
        public decimal Aliquota { get; set; } = 0.20m;
        public decimal ValorIR { get; set; }
        public List<IrVendaDetalhe> Detalhes { get; set; } = new List<IrVendaDetalhe>();
        public DateTime DataCalculo { get; set; }
    }

    public class IrVendaDetalhe
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal PrecoVenda { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal Lucro { get; set; }
    }
}
