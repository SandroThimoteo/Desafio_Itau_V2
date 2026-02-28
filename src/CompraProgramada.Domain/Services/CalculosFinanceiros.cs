using System;
using System.Collections.Generic;
using System.Linq;

namespace CompraProgramada.Domain.Services
{
    public static class CalculosFinanceiros
    {
        public static decimal CalcularPrecoMedio(decimal qtdAnterior, decimal pmAnterior, decimal qtdNova, decimal precoNova)
        {
            if (qtdAnterior + qtdNova == 0) return 0;
            return (qtdAnterior * pmAnterior + qtdNova * precoNova) / (qtdAnterior + qtdNova);
        }

        public static decimal CalcularIrDedoDuro(decimal valorOperacao)
        {
            return Math.Round(valorOperacao * 0.00005m, 2);
        }

        public static decimal CalcularIrVendaMonthly(decimal totalVendasMes, IEnumerable<decimal> lucrosPorOperacao)
        {
            if (totalVendasMes <= 20000m)
                return 0m;
            var lucroLiquido = lucrosPorOperacao.Sum();
            return lucroLiquido <= 0 ? 0m : Math.Round(lucroLiquido * 0.20m, 2);
        }
    }
}
