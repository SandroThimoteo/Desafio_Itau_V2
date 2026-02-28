using System;
using System.Linq;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CompraProgramada.Tests
{
    public class OrdemCompraPersistenceTests
    {
        private DbContextOptions<ApplicationDbContext> CreateOptions(SqliteConnection connection)
        {
            return new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(connection)
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
            // conexão compartilhada — SQLite em memória só persiste enquanto a conexão está aberta
            using var connection = new SqliteConnection("Filename=:memory:");
            connection.Open();

            var options = CreateOptions(connection);

            using (var context = new ApplicationDbContext(options))
            {
                context.Database.EnsureCreated();

                var cliente = CriarClienteValido();
                context.Clientes.Add(cliente);
                context.SaveChanges();

                var item1 = OrdemCompraItem.Criar("PETR4", 100, 35.80m);
                var item2 = OrdemCompraItem.Criar("VALE3", 50, 68.50m, fracionario: true);

                var ordem = OrdemCompra.Criar(cliente.Id, new[] { item1, item2 });

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
                Assert.Equal((35.80m * 100) + (68.50m * 50), saved.ValorTotal);
                Assert.Equal(StatusOrdem.Pendente, saved.Status);
                Assert.NotEqual(default, saved.DataCriacao);
                Assert.Null(saved.DataConclusao);
            }
        }
    }
}