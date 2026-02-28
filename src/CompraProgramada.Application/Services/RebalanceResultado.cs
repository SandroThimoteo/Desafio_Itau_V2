using System.Collections.Generic;

namespace CompraProgramada.Application.Services
{
    public class RebalanceResultado
    {
        public int ClientesProcessados { get; set; }
        public int OrdensVendaGeradas { get; set; }
        public int OrdensCompraGeradas { get; set; }
        public decimal ValorTotalVendido { get; set; }
        public decimal ValorTotalComprado { get; set; }
        public int EventosIrPublicados { get; set; }
        public List<string> Mensagens { get; set; } = new List<string>();
    }
}
