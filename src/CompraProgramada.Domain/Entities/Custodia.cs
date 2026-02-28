using System.Collections.Generic;

namespace CompraProgramada.Domain.Entities
{
    public class Custodia
    {
        public long Id { get; set; }
        public long ContaGraficaId { get; set; }
        public List<CustodiaItem> Itens { get; set; } = new List<CustodiaItem>();
    }

    public class CustodiaItem
    {
        public long Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
    }
}
