using System;
using System.Linq;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CompraProgramada.Tests
{
    public class OrdemCompraPersistenceTests
    {
        private DbContextOptions<ApplicationDbContext> CreateOptions()
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite("Filename=:memory:")
                .Options;
        }

        private Cliente CriarClienteValido() => new Cliente
        {
            Nome = "João Silva",
            CPF = "123.456.789-00",
            Email = "joao@email.com",
            ValorMensal = 1000m,
            Ativo = true,
            DataAdesao = DateTime.UtcNow
        };

        [Fact]
        public void CanPersistAndRetrieve_OrdemCompraWithItems()
        {
            var options = CreateOptions();

            using (var context = new ApplicationDbContext(options))
            {
                context.Database.OpenConnection();
                context.Database.EnsureCreated();

                // cliente precisa existir antes da ordem por causa da FK
                var cliente = CriarClienteValido();
                context.Clientes.Add(cliente);
                context.SaveChanges();

                var item1 = OrdemCompraItem.Criar("PETR4", 100, 35.80m);
                var item2 = OrdemCompraItem.Criar("VALE3", 50, 68.50m, fracionario: true);

                var ordem = new OrdemCompra
                {
                    ClienteId = cliente.Id,
                    DataCriacao = DateTime.UtcNow,
                    Status = StatusOrdem.Pendente,
                    ValorTotal = item1.ValorItem + item2.ValorItem,
                    Itens = new System.Collections.Generic.List<OrdemCompraItem> { item1, item2 }
                };

                context.OrdensCompra.Add(ordem);
                context.SaveChanges();
            }

            using (var context = new ApplicationDbContext(options))
            {
                var saved = context.OrdensCompra
                    .Include(o => o.Itens)
                    .FirstOrDefault();

                Assert.NotNull(saved);
                Assert.Equal(2, saved!.Itens.Count);
                Assert.Equal(35.80m * 100, saved.Itens[0].ValorItem);
                Assert.Equal(68.50m * 50, saved.Itens[1].ValorItem);
            }
        }
    }
}