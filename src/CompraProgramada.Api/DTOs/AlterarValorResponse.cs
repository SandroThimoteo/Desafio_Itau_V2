using System;

namespace CompraProgramada.Api.DTOs
{
    public class AlterarValorResponse
    {
        public long ClienteId { get; set; }
        public decimal ValorMensalAnterior { get; set; }
        public decimal ValorMensalNovo { get; set; }
        public DateTime DataAlteracao { get; set; }
        public string Mensagem { get; set; } = string.Empty;
    }
}
