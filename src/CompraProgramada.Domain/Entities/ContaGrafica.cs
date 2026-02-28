using System;

namespace CompraProgramada.Domain.Entities
{
    public class ContaGrafica
    {
        public long Id { get; set; }
        public string NumeroConta { get; set; } = string.Empty;
        public ContaTipo Tipo { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    public enum ContaTipo
    {
        Master,
        Filhote
    }
}
