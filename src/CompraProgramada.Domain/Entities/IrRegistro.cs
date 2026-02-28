using System;

namespace CompraProgramada.Domain.Entities
{
    public class IrRegistro
    {
        public long Id { get; set; }
        public long ClienteId { get; set; }
        public string Tipo { get; set; } = string.Empty; // DEDO_DURO | VENDA
        public string? Ticker { get; set; }
        public string? MesReferencia { get; set; }
        public decimal ValorOperacao { get; set; }
        public decimal LucroLiquido { get; set; }
        public decimal Aliquota { get; set; }
        public decimal ValorIR { get; set; }
        public DateTime DataEvento { get; set; }
    }
}
