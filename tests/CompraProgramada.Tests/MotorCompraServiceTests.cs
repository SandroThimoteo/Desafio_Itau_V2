using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure;
using Xunit;

namespace CompraProgramada.Tests
{
    public class MotorCompraServiceTests
    {
        private string BuildLinhaValida(string ticker, string date = "20260225")
        {
            var chars = new char[245];
            for (int i = 0; i < chars.Length; i++) chars[i] = ' ';
            void Set(int start, string value)
            {
                for (int j = 0; j < value.Length && start + j < chars.Length; j++)
                    chars[start + j] = value[j];
            }
            Set(0, "01");                 // TIPREG
            Set(2, date);                   // DATPRE
            Set(10, "02");                // CODBDI
            Set(12, ticker);                // CODNEG
            Set(24, "010");               // TPMERC
            Set(27, "COMPANY    ");       // NOMRES
            Set(56, "0000000001000");     // PREABE = 10.00
            Set(69, "0000000001200");     // PREMAX = 12.00
            Set(82, "0000000000900");     // PREMIN = 9.00
            Set(95, "0000000001100");     // PREMED = 11.00
            Set(108, "0000000001150");    // PREULT = 11.50
            Set(152, "000000000000001000"); // QUATNEG
            Set(170, "000000000000011500"); // VOLNEG (as cents)
            return new string(chars);
        }

        [Fact]
        public void ExecutarCompra_ComCestaValida_GeraOrdens()
        {
            var cwd = Directory.GetCurrentDirectory();
            var cotacoesDir = Path.Combine(cwd, "cotacoes");
            Directory.CreateDirectory(cotacoesDir);

            // create cotahist for PETR4 and VALE3
            File.WriteAllText(Path.Combine(cotacoesDir, "COTAHIST_D25022026.TXT"), BuildLinhaValida("PETR4    "), System.Text.Encoding.GetEncoding("ISO-8859-1"));
            File.WriteAllText(Path.Combine(cotacoesDir, "COTAHIST_D25022026_VALE.TXT"), BuildLinhaValida("VALE3    "), System.Text.Encoding.GetEncoding("ISO-8859-1"));

            var clientes = new List<Cliente>
            {
                new Cliente { Id = 1L, Nome = "A", ValorMensal = 300m },
                new Cliente { Id = 2L, Nome = "B", ValorMensal = 300m }
            };

            var cesta = new CestaTopFive
            {
                Itens = new List<CestaItem>
                {
                    new CestaItem { Ticker = "PETR4", Percentual = 50m },
                    new CestaItem { Ticker = "VALE3", Percentual = 50m }
                }
            };

            var saldoMaster = new Dictionary<string, decimal>();

            var motor = new MotorCompraService();
            var resultado = motor.ExecutarCompra(DateTime.UtcNow, clientes, cesta, saldoMaster);

            Assert.NotNull(resultado);
            Assert.True(resultado.Ordens.Count >= 1);
            Assert.Contains(resultado.Ordens, o => o.Ticker.StartsWith("PETR4", StringComparison.OrdinalIgnoreCase));

            // cleanup
            try { Directory.Delete(cotacoesDir, true); } catch { }
        }

        [Fact]
        public void ExecutarCompra_CotacaoNaoEncontrada_Throws()
        {
            var clientes = new List<Cliente>
            {
                new Cliente { Id = 1L, Nome = "A", ValorMensal = 300m }
            };

            // ensure cotacoes directory exists (but contains no files for NONEXIST)
            var cotacoesDir = Path.Combine(Directory.GetCurrentDirectory(), "cotacoes");
            Directory.CreateDirectory(cotacoesDir);

            var cesta = new CestaTopFive
            {
                Itens = new List<CestaItem>
                {
                    new CestaItem { Ticker = "NONEXIST", Percentual = 100m }
                }
            };

            var motor = new MotorCompraService();
            var ex = Assert.Throws<Exception>(() => motor.ExecutarCompra(DateTime.UtcNow, clientes, cesta, new Dictionary<string, decimal>()));
            Assert.StartsWith("COTACAO_NAO_ENCONTRADA", ex.Message);
            try { Directory.Delete(cotacoesDir, true); } catch { }
        }
    }
}
