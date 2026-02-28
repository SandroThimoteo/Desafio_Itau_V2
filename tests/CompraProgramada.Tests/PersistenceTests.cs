using System;
using System.Linq;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CompraProgramada.Tests
{
    public class PersistenceTests
    {
        private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new ApplicationDbContext(options);
        }

        [Fact]
        public void SavingOrdemCompra_PersistsItemsAndCalculatesTotals()
        {
            using var ctx = CreateContext();

            var item1 = OrdemCompraItem.Criar("PETR4", 100, 10m);
            var item2 = OrdemCompraItem.Criar("VALE3", 50, 20m, fracionario: true);

            var ordem = OrdemCompra.Criar(42, new[] { item1, item2 });

            ctx.OrdensCompra.Add(ordem);
            ctx.SaveChanges();

            var saved = ctx.OrdensCompra
                           .Include(o => o.Itens)
                           .FirstOrDefault(o => o.Id == ordem.Id);

            Assert.NotNull(saved);
            Assert.Equal(2, saved!.Itens.Count);
            Assert.Equal(2000m, saved.ValorTotal); // 100*10 + 50*20
            Assert.Equal(2000m, saved.Itens.Sum(i => i.ValorItem));
        }
    }
}