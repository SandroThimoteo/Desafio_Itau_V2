using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CompraProgramada.Tests
{
    [Collection("Cotacoes")]
    public class RentabilidadeServiceTests
    {
        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private string BuildLinhaValida(string ticker, string date = "20260225", string fechamento = "0000000001200")
        {
            var chars = new char[245];
            for (int i = 0; i < chars.Length; i++) chars[i] = ' ';
            void Set(int start, string value)
            {
                for (int j = 0; j < value.Length && start + j < chars.Length; j++)
                    chars[start + j] = value[j];
            }
            Set(0, "01");
            Set(2, date);
            Set(10, "02");
            Set(12, ticker);
            Set(24, "010");
            Set(27, "COMPANY    ");
            Set(56, "0000000001000");
            Set(69, "0000000001200");
            Set(82, "0000000000900");
            Set(95, "0000000001100");
            Set(108, fechamento);
            Set(152, "000000000000001000");
            Set(170, "000000000000011500");
            return new string(chars);
        }

        [Fact]
        public void Calcular_ClienteInexistente_RetornaNull()
        {
            using var ctx = CreateContext();
            var service = new RentabilidadeService(ctx);

            var resultado = service.Calcular(999);

            Assert.Null(resultado);
        }

        [Fact]
        public void Calcular_DeveRetornarPlTotalEhHistorico()
        {
            var cwd = Directory.GetCurrentDirectory();
            var cotacoesDir = Path.Combine(cwd, "cotacoes");
            Directory.CreateDirectory(cotacoesDir);
            File.WriteAllText(Path.Combine(cotacoesDir, "COTAHIST_D25022026.TXT"), BuildLinhaValida("PETR4    ", fechamento: "0000000001200"), System.Text.Encoding.GetEncoding("ISO-8859-1"));

            using var ctx = CreateContext();

            var cliente = new Cliente
            {
                Nome = "Cliente Rent",
                CPF = "11111111111",
                Email = "rent@x.com",
                ValorMensal = 1000m,
                Ativo = true,
                DataAdesao = DateTime.UtcNow,
                Custodia = new Custodia
                {
                    Itens = new List<CustodiaItem>
                    {
                        new CustodiaItem { Ticker = "PETR4", Quantidade = 10m, PrecoMedio = 10m }
                    }
                }
            };

            ctx.Clientes.Add(cliente);
            ctx.SaveChanges();

            var ordem = OrdemCompra.Criar(cliente.Id, new[] { OrdemCompraItem.Criar("PETR4", 10m, 10m) });
            ordem.MarcarExecutada();
            ctx.OrdensCompra.Add(ordem);
            ctx.SaveChanges();

            var service = new RentabilidadeService(ctx);
            var resultado = service.Calcular(cliente.Id);

            Assert.NotNull(resultado);
            Assert.Equal(cliente.Id, resultado!.ClienteId);
            Assert.True(resultado.Itens.Count == 1);
            Assert.Equal(20m, resultado.PlTotal); // 10*(12-10)
            Assert.True(resultado.RentabilidadePercentual > 0);
            Assert.True(resultado.HistoricoEvolucao.Count >= 1);

            try { Directory.Delete(cotacoesDir, true); } catch { }
        }
    }
}
