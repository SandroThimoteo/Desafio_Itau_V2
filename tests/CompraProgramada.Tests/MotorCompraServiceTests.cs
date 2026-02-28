using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CompraProgramada.Tests
{
    [Collection("Cotacoes")]
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

        private class FakeIrPublisher : IrPublisher
        {
            public List<IrDedoDuroEvent> Events = new List<IrDedoDuroEvent>();
            public FakeIrPublisher() : base(new KafkaProducer("localhost:9092", "ir-test")) { }
            public override global::System.Threading.Tasks.Task PublishDedoDuro(IrDedoDuroEvent evt)
            {
                Events.Add(evt);
                return global::System.Threading.Tasks.Task.CompletedTask;
            }
            public override global::System.Threading.Tasks.Task PublishVenda(IrVendaEvent evt) => global::System.Threading.Tasks.Task.CompletedTask;
        }

        private ApplicationDbContext CreateContext()
        {
            var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void ExecutarCompra_ComCestaValida_GeraOrdensYDistribuicao()
        {
            var cwd = Directory.GetCurrentDirectory();
            var cotacoesDir = Path.Combine(cwd, "cotacoes");
            Directory.CreateDirectory(cotacoesDir);

            File.WriteAllText(Path.Combine(cotacoesDir, "COTAHIST_D25022026.TXT"), BuildLinhaValida("PETR4    "), System.Text.Encoding.GetEncoding("ISO-8859-1"));
            File.WriteAllText(Path.Combine(cotacoesDir, "COTAHIST_D25022026_VALE.TXT"), BuildLinhaValida("VALE3    "), System.Text.Encoding.GetEncoding("ISO-8859-1"));

            var clientes = new List<Cliente>
            {
                new Cliente { Id = 1L, Nome = "A", ValorMensal = 300m, CPF = "123" },
                new Cliente { Id = 2L, Nome = "B", ValorMensal = 300m, CPF = "456" }
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

            using var ctx = CreateContext();
            var fakeIr = new FakeIrPublisher();
            var motor = new MotorCompraService(ctx, fakeIr, LoggerTestHelper.CreateMockLogger<MotorCompraService>());

            var resultado = motor.ExecutarCompra(DateTime.UtcNow, clientes, cesta, saldoMaster);

            Assert.NotNull(resultado);
            Assert.Equal(2, resultado.ClientesDistribuidos.Count);
            Assert.True(resultado.Ordens.Any());
            Assert.Contains(resultado.Ordens, o => o.Ticker == "PETR4" || o.Ticker == "VALE3");

            // check that ordens were persisted
            var saved = ctx.OrdensCompra.Include(o => o.Itens).ToList();
            Assert.True(saved.Count >= 1);
            Assert.All(saved, o => Assert.Equal(StatusOrdem.Executada, o.Status));

            var distribuicoes = ctx.Distribuicoes.Include(d => d.Itens).ToList();
            Assert.True(distribuicoes.Count >= 1);

            var irRegistros = ctx.IrRegistros.ToList();
            Assert.True(irRegistros.Count >= 1);
            Assert.Contains(irRegistros, i => i.Tipo == "DEDO_DURO");

            // custódias devem ter sido preenchidas
            foreach (var cliente in clientes)
            {
                Assert.NotNull(cliente.Custodia);
                Assert.True(cliente.Custodia.Itens.Count > 0);
            }

            // IR events should have been published
            Assert.True(fakeIr.Events.Count > 0);

            try { Directory.Delete(cotacoesDir, true); } catch { }
        }

        [Fact]
        public void ExecutarCompra_ConsomeSaldoMasterEPersisteNovoSaldo()
        {
            var cwd = Directory.GetCurrentDirectory();
            var cotacoesDir = Path.Combine(cwd, "cotacoes");
            Directory.CreateDirectory(cotacoesDir);
            File.WriteAllText(Path.Combine(cotacoesDir, "COTAHIST_D25022026.TXT"), BuildLinhaValida("PETR4    "), System.Text.Encoding.GetEncoding("ISO-8859-1"));

            var clientes = new List<Cliente>
            {
                new Cliente { Id = 10L, Nome = "Cliente Master", ValorMensal = 300m, CPF = "111" }
            };

            var cesta = new CestaTopFive
            {
                Itens = new List<CestaItem>
                {
                    new CestaItem { Ticker = "PETR4", Percentual = 100m }
                }
            };

            using var ctx = CreateContext();
            var contaMaster = new ContaGrafica
            {
                NumeroConta = "MASTER-001",
                Tipo = ContaTipo.Master,
                DataCriacao = DateTime.UtcNow
            };
            ctx.ContasGraficas.Add(contaMaster);
            ctx.SaveChanges();

            var custodiaMaster = new Custodia
            {
                ContaGraficaId = contaMaster.Id,
                Itens = new List<CustodiaItem>
                {
                    new CustodiaItem { Ticker = "PETR4", Quantidade = 10m, PrecoMedio = 11.5m }
                }
            };
            ctx.Custodias.Add(custodiaMaster);
            ctx.SaveChanges();

            var fakeIr = new FakeIrPublisher();
            var motor = new MotorCompraService(ctx, fakeIr, LoggerTestHelper.CreateMockLogger<MotorCompraService>());

            var saldoMaster = new Dictionary<string, decimal>();
            var resultado = motor.ExecutarCompra(DateTime.UtcNow, clientes, cesta, saldoMaster);

            Assert.NotNull(resultado);
            Assert.Empty(resultado.Ordens); // saldo master cobriu a necessidade, sem compra adicional

            var masterAtualizada = ctx.Custodias.Include(c => c.Itens).First(c => c.ContaGraficaId == contaMaster.Id);
            var itemMaster = masterAtualizada.Itens.First(i => i.Ticker == "PETR4");
            Assert.Equal(2m, itemMaster.Quantidade);
            Assert.Equal(2m, saldoMaster["PETR4"]);

            try { Directory.Delete(cotacoesDir, true); } catch { }
        }

        [Fact]
        public void ExecutarCompra_CotacaoNaoEncontrada_Throws()
        {
            var clientes = new List<Cliente>
            {
                new Cliente { Id = 1L, Nome = "A", ValorMensal = 300m }
            };

            var cotacoesDir = Path.Combine(Directory.GetCurrentDirectory(), "cotacoes");
            Directory.CreateDirectory(cotacoesDir);

            var cesta = new CestaTopFive
            {
                Itens = new List<CestaItem>
                {
                    new CestaItem { Ticker = "NONEXIST", Percentual = 100m }
                }
            };

            using var ctx = CreateContext();
            var fakeIr = new FakeIrPublisher();
            var motor = new MotorCompraService(ctx, fakeIr, LoggerTestHelper.CreateMockLogger<MotorCompraService>());

            var ex = Assert.Throws<Exception>(() => motor.ExecutarCompra(DateTime.UtcNow, clientes, cesta, new Dictionary<string, decimal>()));
            Assert.StartsWith("COTACAO_NAO_ENCONTRADA", ex.Message);
            try { Directory.Delete(cotacoesDir, true); } catch { }
        }
    }
}
