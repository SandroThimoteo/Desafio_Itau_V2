using System;
using System.Collections.Generic;
using System.Linq;
using CompraProgramada.Domain.Entities;
using Xunit;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Tests
{
    public class OrdemCompraTests
    {
        private static OrdemCompraItem CriarItem(string ticker, decimal quantidade, decimal precoUnitario, bool fracionario = false)
        {
            return OrdemCompraItem.Criar(ticker, quantidade, precoUnitario, fracionario);
        }

        private static OrdemCompra CriarOrdem(List<OrdemCompraItem> itens)
        {
            return OrdemCompra.Criar(1, itens);
        }

        [Fact]
        public void OrdemCompra_StatusInicial_DeveSerPendente()
        {
            var ordem = CriarOrdem(new List<OrdemCompraItem> { CriarItem("PETR4", 1, 1) });

            Assert.Equal(StatusOrdem.Pendente, ordem.Status);
        }

        [Fact]
        public void OrdemCompra_DataCriacao_DeveSerPreenchida()
        {
            var antes = DateTime.UtcNow;
            var ordem = CriarOrdem(new List<OrdemCompraItem> { CriarItem("PETR4", 1, 1) });

            Assert.True(ordem.DataCriacao >= antes);
            Assert.Null(ordem.DataConclusao);
        }

        [Fact]
        public void OrdemCompra_ValorItem_DeveSerSomaDosItens()
        {
            var itens = new List<OrdemCompraItem>
            {
                CriarItem("PETR4", 100, 35.80m),
                CriarItem("VALE3", 200, 68.50m)
            };

            var ordem = CriarOrdem(itens);

            Assert.Equal(17280.00m, ordem.ValorTotal);
        }

        [Fact]
        public void OrdemCompraItem_ValorItem_DeveSerQuantidadeVezesPreco()
        {
            var item = CriarItem("PETR4", 100, 35.80m);

            Assert.Equal(3580.00m, item.ValorItem);
        }

        [Fact]
        public void OrdemCompra_Itens_DevemTerTickerQuantidadeEPrecoCorretos()
        {
            var itens = new List<OrdemCompraItem>
            {
                CriarItem("PETR4", 100, 35.80m),
                CriarItem("PETR4F", 50, 35.80m, fracionario: true)
            };

            var ordem = CriarOrdem(itens);

            var itemPadrao    = ordem.Itens.First(i => !i.Fracionario);
            var itemFracionario = ordem.Itens.First(i => i.Fracionario);

            Assert.Equal("PETR4",  itemPadrao.Ticker);
            Assert.Equal(100,      itemPadrao.Quantidade);
            Assert.Equal(35.80m,   itemPadrao.PrecoUnitario);

            Assert.Equal("PETR4F", itemFracionario.Ticker);
            Assert.Equal(50,       itemFracionario.Quantidade);
            Assert.True(itemFracionario.Fracionario);
        }

        [Fact]
        public void OrdemCompra_AposExecucao_DeveAtualizarStatusEDataExecucao()
        {
            var ordem = CriarOrdem(new List<OrdemCompraItem> { CriarItem("PETR4", 1, 1) });

            ordem.MarcarExecutada();

            Assert.Equal(StatusOrdem.Executada, ordem.Status);
            Assert.NotNull(ordem.DataConclusao);
        }

        [Fact]
        public void CriarOrdem_SemItens_LancaExcecao()
        {
            Assert.Throws<ArgumentException>(() => OrdemCompra.Criar(1, new List<OrdemCompraItem>()));
        }

        [Fact]
        public void CriarItem_Invalido_LancaExcecao()
        {
            Assert.Throws<ArgumentException>(() => OrdemCompraItem.Criar("", 0, 0));
        }

        [Fact]
        public void OrdemCompra_SalvarERecuperarDoContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                            .UseSqlite("Filename=:memory:")
                            .Options;
            using var context = new ApplicationDbContext(options);
            context.Database.OpenConnection();
            context.Database.EnsureCreated();

            // ensure a cliente exists so FK constraint passes
            context.Clientes.Add(new Cliente { Id = 1, Nome = "A", CPF = "00000000000", ValorMensal = 100m, DataAdesao = DateTime.UtcNow });
            var item = OrdemCompraItem.Criar("PETR4", 10, 10);
            var ordem = OrdemCompra.Criar(1, new[] { item });
            context.OrdensCompra.Add(ordem);
            context.SaveChanges();

            var carregada = context.OrdensCompra.Include(o => o.Itens).First();
            Assert.Equal(ordem.ValorTotal, carregada.ValorTotal);
            Assert.Single(carregada.Itens);
            Assert.Equal("PETR4", carregada.Itens[0].Ticker);
        }
    }
}