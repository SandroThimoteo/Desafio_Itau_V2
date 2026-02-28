using System;
using System.Collections.Generic;

namespace CompraProgramada.Domain.Entities
{
    public class CestaTopFive
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativa { get; set; } = true;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataDesativacao { get; set; }
        public List<CestaItem> Itens { get; set; } = new List<CestaItem>();
    }

    public class CestaItem
    {
        public long Id { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }
}
