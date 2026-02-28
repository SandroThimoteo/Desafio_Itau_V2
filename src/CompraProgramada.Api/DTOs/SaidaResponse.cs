using System;

namespace CompraProgramada.Api.DTOs
{
    public class SaidaResponse
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public DateTime DataSaida { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }
}
