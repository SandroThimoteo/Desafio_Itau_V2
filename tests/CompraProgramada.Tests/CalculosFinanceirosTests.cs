using System;
using System.Collections.Generic;
using CompraProgramada.Domain.Services;
using Xunit;

namespace CompraProgramada.Tests
{
    public class CalculosFinanceirosTests
    {
        [Fact]
        public void CalcularPrecoMedio_FormulaCorreta()
        {
            var pm = CalculosFinanceiros.CalcularPrecoMedio(10, 5m, 5, 7m);
            Assert.Equal((10*5m + 5*7m)/15m, pm);
        }

        [Fact]
        public void CalcularIrDedoDuro_0_005percent()
        {
            var ir = CalculosFinanceiros.CalcularIrDedoDuro(1000m);
            Assert.Equal(Math.Round(1000m * 0.00005m, 2), ir);
        }

        [Fact]
        public void CalcularIrVendaMonthly_IsentoAcima20k()
        {
            var ir = CalculosFinanceiros.CalcularIrVendaMonthly(19000m, new decimal[] {100m, 200m});
            Assert.Equal(0m, ir);
        }
    }
}
