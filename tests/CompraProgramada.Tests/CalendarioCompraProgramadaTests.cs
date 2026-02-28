using System;
using System.Collections.Generic;
using CompraProgramada.Application.Services;
using Xunit;

namespace CompraProgramada.Tests
{
    public class CalendarioCompraProgramadaTests
    {
        [Fact]
        public void CalcularDataUtilOuSubsequente_QuandoDiaUtil_MantemData()
        {
            var data = CalendarioCompraProgramada.CalcularDataUtilOuSubsequente(2026, 3, 5);
            Assert.Equal(new DateTime(2026, 3, 5, 0, 0, 0, DateTimeKind.Utc), data);
        }

        [Fact]
        public void CalcularDataUtilOuSubsequente_QuandoFinalSemana_EmpurraParaSegunda()
        {
            var data = CalendarioCompraProgramada.CalcularDataUtilOuSubsequente(2026, 4, 5);
            Assert.Equal(new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc), data);
        }

        [Fact]
        public void ObterCiclosPendentes_DeveRetornarSomenteNaoExecutadosEAtingidos()
        {
            var hoje = new DateTime(2026, 4, 16, 10, 0, 0, DateTimeKind.Utc);
            var executados = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "202604-5"
            };

            var pendentes = CalendarioCompraProgramada.ObterCiclosPendentes(hoje, executados);

            Assert.Single(pendentes);
            Assert.Equal("202604-15", pendentes[0].Chave);
            Assert.Equal(15, pendentes[0].DiaBase);
        }
    }
}
