using System;

namespace CompraProgramada.Api.DTOs
{
    public class AdesaoResponse
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal ValorMensal { get; set; }
        public bool Ativo { get; set; }
        public DateTime DataAdesao { get; set; }
        public ContaGraficaDTO ContaGrafica { get; set; } = new();
    }

    public class ContaGraficaDTO
    {
        public long Id { get; set; }
        public string NumeroConta { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
    }
}
