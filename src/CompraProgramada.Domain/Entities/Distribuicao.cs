using System;
using System.Collections.Generic;

namespace CompraProgramada.Domain.Entities
{
    public class Distribuicao
    {
        public long Id { get; set; }
        public long ClienteId { get; set; }
        public DateTime Data { get; set; }
        public List<DistribuicaoItem> Itens { get; set; } = new List<DistribuicaoItem>();
    }

    public class DistribuicaoItem
    {
        public long Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
    }
}
