using System;
using System.Collections.Generic;
using System.Linq;

namespace CompraProgramada.Application.Services
{
    public class CicloCompraProgramada
    {
        public string Chave { get; set; } = string.Empty;
        public int DiaBase { get; set; }
        public DateTime DataReferenciaUtc { get; set; }
    }

    public static class CalendarioCompraProgramada
    {
        private static readonly int[] DiasBase = new[] { 5, 15, 25 };

        public static DateTime CalcularDataUtilOuSubsequente(int ano, int mes, int diaBase)
        {
            var data = new DateTime(ano, mes, diaBase, 0, 0, 0, DateTimeKind.Utc);

            while (data.DayOfWeek == DayOfWeek.Saturday || data.DayOfWeek == DayOfWeek.Sunday)
            {
                data = data.AddDays(1);
            }

            return data;
        }

        public static List<CicloCompraProgramada> ObterCiclosPendentes(DateTime dataAtualUtc, ISet<string> ciclosExecutados)
        {
            var hoje = dataAtualUtc.Date;
            var resultado = new List<CicloCompraProgramada>();

            foreach (var diaBase in DiasBase)
            {
                var dataExecucao = CalcularDataUtilOuSubsequente(hoje.Year, hoje.Month, diaBase);
                var chave = $"{hoje:yyyyMM}-{diaBase}";

                if (hoje >= dataExecucao.Date && !ciclosExecutados.Contains(chave))
                {
                    resultado.Add(new CicloCompraProgramada
                    {
                        Chave = chave,
                        DiaBase = diaBase,
                        DataReferenciaUtc = dataExecucao
                    });
                }
            }

            return resultado.OrderBy(c => c.DiaBase).ToList();
        }
    }
}
