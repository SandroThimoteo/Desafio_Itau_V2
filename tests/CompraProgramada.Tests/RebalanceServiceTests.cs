using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CompraProgramada.Tests
{
    [Collection("Cotacoes")]
    public class RebalanceServiceTests
    {
        private class FakeIrPublisher : IrPublisher
        {
            public List<IrVendaEvent> Vendas { get; } = new List<IrVendaEvent>();

            public FakeIrPublisher() : base(new KafkaProducer("localhost:9092", "ir-test"))
            {
            }

            public override Task PublishVenda(IrVendaEvent evt)
            {
                Vendas.Add(evt);
                return Task.CompletedTask;
            }
        }

        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        private string BuildLinhaValida(string ticker, string date = "20260225")
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
            Set(108, "0000000001150");
            Set(152, "000000000000001000");
            Set(170, "000000000000011500");
            return new string(chars);
        }

        [Fact]
        public async Task RebalancearPorMudancaDeCestaAsync_CestaInvalida_RetornaMensagem()
        {
            using var ctx = CreateContext();
            var service = new RebalanceService(ctx, new FakeIrPublisher(), LoggerTestHelper.CreateMockLogger<RebalanceService>());

            var result = await service.RebalancearPorMudancaDeCestaAsync(9999, DateTime.UtcNow);

            Assert.NotNull(result);
            Assert.Contains("nao encontrada", string.Join(" ", result.Mensagens), StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task RebalancearPorMudancaDeCestaAsync_ComAtivoForaDaCesta_VendeECompraNovoAtivo()
        {
            var cwd = Directory.GetCurrentDirectory();
            var cotacoesDir = Path.Combine(cwd, "cotacoes");
            Directory.CreateDirectory(cotacoesDir);
            File.WriteAllText(Path.Combine(cotacoesDir, "COTAHIST_D25022026.TXT"), BuildLinhaValida("PETR4    "), System.Text.Encoding.GetEncoding("ISO-8859-1"));

            using var ctx = CreateContext();

            var cesta = new CestaTopFive
            {
                Nome = "Top Five",
                Ativa = true,
                DataCriacao = DateTime.UtcNow,
                Itens = new List<CestaItem>
                {
                    new CestaItem { Ticker = "PETR4", Percentual = 100m },
                    new CestaItem { Ticker = "VALE3", Percentual = 0m },
                    new CestaItem { Ticker = "ITUB4", Percentual = 0m },
                    new CestaItem { Ticker = "BBDC4", Percentual = 0m },
                    new CestaItem { Ticker = "WEGE3", Percentual = 0m }
                }
            };

            var cliente = new Cliente
            {
                Nome = "Cliente A",
                CPF = "12345678900",
                Email = "a@a.com",
                ValorMensal = 1000m,
                Ativo = true,
                DataAdesao = DateTime.UtcNow,
                Custodia = new Custodia
                {
                    Itens = new List<CustodiaItem>
                    {
                        new CustodiaItem { Ticker = "ABEV3", Quantidade = 100m, PrecoMedio = 10m }
                    }
                }
            };

            ctx.Cestas.Add(cesta);
            ctx.Clientes.Add(cliente);
            ctx.SaveChanges();

            var service = new RebalanceService(ctx, new FakeIrPublisher(), LoggerTestHelper.CreateMockLogger<RebalanceService>());

            var result = await service.RebalancearPorMudancaDeCestaAsync(cesta.Id, DateTime.UtcNow);

            var clienteAtualizado = ctx.Clientes
                .Include(c => c.Custodia)
                .ThenInclude(c => c.Itens)
                .First(c => c.Id == cliente.Id);

            Assert.True(result.OrdensVendaGeradas > 0);
            Assert.True(result.OrdensCompraGeradas > 0);
            Assert.Contains(clienteAtualizado.Custodia.Itens, i => i.Ticker.StartsWith("PETR4", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(clienteAtualizado.Custodia.Itens, i => i.Ticker.Equals("ABEV3", StringComparison.OrdinalIgnoreCase) && i.Quantidade > 0);

            try { Directory.Delete(cotacoesDir, true); } catch { }
        }

        [Fact]
        public async Task RebalancearPorMudancaDeCestaAsync_AcimaDe20k_GeraRegistroIrVenda()
        {
            var cwd = Directory.GetCurrentDirectory();
            var cotacoesDir = Path.Combine(cwd, "cotacoes");
            Directory.CreateDirectory(cotacoesDir);
            File.WriteAllText(Path.Combine(cotacoesDir, "COTAHIST_D25022026.TXT"), BuildLinhaValida("PETR4    "), System.Text.Encoding.GetEncoding("ISO-8859-1"));

            using var ctx = CreateContext();
            var fakeIr = new FakeIrPublisher();

            var cesta = new CestaTopFive
            {
                Nome = "Top Five",
                Ativa = true,
                DataCriacao = DateTime.UtcNow,
                Itens = new List<CestaItem>
                {
                    new CestaItem { Ticker = "PETR4", Percentual = 100m },
                    new CestaItem { Ticker = "VALE3", Percentual = 0m },
                    new CestaItem { Ticker = "ITUB4", Percentual = 0m },
                    new CestaItem { Ticker = "BBDC4", Percentual = 0m },
                    new CestaItem { Ticker = "WEGE3", Percentual = 0m }
                }
            };

            var cliente = new Cliente
            {
                Nome = "Cliente B",
                CPF = "99999999999",
                Email = "b@b.com",
                ValorMensal = 1000m,
                Ativo = true,
                DataAdesao = DateTime.UtcNow,
                Custodia = new Custodia
                {
                    Itens = new List<CustodiaItem>
                    {
                        new CustodiaItem { Ticker = "ABEV3", Quantidade = 3000m, PrecoMedio = 10m }
                    }
                }
            };

            ctx.Cestas.Add(cesta);
            ctx.Clientes.Add(cliente);
            ctx.SaveChanges();

            var service = new RebalanceService(ctx, fakeIr, LoggerTestHelper.CreateMockLogger<RebalanceService>());
            var result = await service.RebalancearPorMudancaDeCestaAsync(cesta.Id, DateTime.UtcNow);

            Assert.True(result.EventosIrPublicados >= 1);
            Assert.True(fakeIr.Vendas.Count >= 1);
            Assert.Contains(ctx.IrRegistros.ToList(), i => i.Tipo == "VENDA" && i.ClienteId == cliente.Id);

            try { Directory.Delete(cotacoesDir, true); } catch { }
        }
    }
}
