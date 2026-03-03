using CompraProgramada.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Cliente> Clientes { get; set; } = null!;
        public DbSet<CestaTopFive> Cestas { get; set; } = null!;
        public DbSet<OrdemCompra> OrdensCompra { get; set; } = null!;
        public DbSet<Distribuicao> Distribuicoes { get; set; } = null!;
        public DbSet<Custodia> Custodias { get; set; } = null!;
        public DbSet<ContaGrafica> ContasGraficas { get; set; } = null!;
        public DbSet<IrRegistro> IrRegistros { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Cliente
            modelBuilder.Entity<Cliente>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).ValueGeneratedOnAdd();
                e.Property(c => c.Nome).IsRequired().HasMaxLength(200);
                e.Property(c => c.CPF).IsRequired().HasMaxLength(14);
                e.Property(c => c.Email).HasMaxLength(200);
                e.Property(c => c.ValorMensal).HasPrecision(18, 2);
                e.HasOne(c => c.ContaGrafica).WithOne().HasForeignKey<ContaGrafica>("ClienteId");
                e.HasOne(c => c.Custodia).WithOne().HasForeignKey<Custodia>("ClienteId");
                e.HasMany(c => c.HistoricoValores).WithOne().HasForeignKey("ClienteId");
            });

            // CestaTopFive
            modelBuilder.Entity<CestaTopFive>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).ValueGeneratedOnAdd();
                e.HasMany(c => c.Itens).WithOne().HasForeignKey("CestaId").OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CestaItem>(e =>
            {
                e.HasKey(i => i.Id);
                e.Property(i => i.Id).ValueGeneratedOnAdd();
                e.Property(i => i.Ticker).IsRequired().HasMaxLength(20);
                e.Property(i => i.Percentual).HasPrecision(5, 2);
            });

            // OrdemCompra
            modelBuilder.Entity<OrdemCompra>(e =>
            {
                e.HasKey(o => o.Id);
                e.Property(o => o.Id).ValueGeneratedOnAdd();
                e.Property(o => o.ValorTotal).HasPrecision(18, 2);
                e.Property(o => o.ValorCarteiraNoMomento).HasPrecision(18, 2);
                e.HasMany(o => o.Itens)
                    .WithOne(i => i.OrdemCompra)
                    .HasForeignKey(i => i.OrdemCompraId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrdemCompraItem>(e =>
            {
                e.HasKey(i => i.Id);
                e.Property(i => i.Id).ValueGeneratedOnAdd();
                e.Property(i => i.Quantidade).HasPrecision(18, 2);
                e.Property(i => i.PrecoUnitario).HasPrecision(18, 2);
                e.Property(i => i.ValorItem).HasPrecision(18, 2);
            });

            // Custodia
            modelBuilder.Entity<Custodia>(e =>
            {
                e.HasKey(c => c.Id);
                e.Property(c => c.Id).ValueGeneratedOnAdd();
                e.HasMany(c => c.Itens).WithOne().HasForeignKey("CustodiaId").OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CustodiaItem>(e =>
            {
                e.HasKey(i => i.Id);
                e.Property(i => i.Id).ValueGeneratedOnAdd();
                e.Property(i => i.Quantidade).HasPrecision(18, 2);
                e.Property(i => i.PrecoMedio).HasPrecision(18, 2);
            });

            // Distribuicao
            modelBuilder.Entity<Distribuicao>(e =>
            {
                e.HasKey(d => d.Id);
                e.Property(d => d.Id).ValueGeneratedOnAdd();
                e.HasMany(d => d.Itens).WithOne().HasForeignKey("DistribuicaoId").OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<IrRegistro>(e =>
            {
                e.HasKey(i => i.Id);
                e.Property(i => i.Id).ValueGeneratedOnAdd();
                e.Property(i => i.Tipo).IsRequired().HasMaxLength(20);
                e.Property(i => i.Ticker).HasMaxLength(20);
                e.Property(i => i.MesReferencia).HasMaxLength(7);
                e.Property(i => i.ValorOperacao).HasPrecision(18, 2);
                e.Property(i => i.LucroLiquido).HasPrecision(18, 2);
                e.Property(i => i.Aliquota).HasPrecision(10, 6);
                e.Property(i => i.ValorIR).HasPrecision(18, 2);
            });
        }
    }
}