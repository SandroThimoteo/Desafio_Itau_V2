using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;

public class InfrastructureDataAccessTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    public InfrastructureDataAccessTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    #region Clientes CRUD

    [Fact]
    public void InsertCliente_DeveArmazenarCorretamente()
    {
        // Arrange
        var cliente = new Cliente
        {
            Nome = "João Silva",
            CPF = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 1000m,
            Ativo = true,
            DataAdesao = DateTime.UtcNow,
            ContaGrafica = new ContaGrafica
            {
                Tipo = ContaTipo.Filhote,
                NumeroConta = "FLH-123456",
                DataCriacao = DateTime.UtcNow
            }
        };

        // Act
        _db.Clientes.Add(cliente);
        _db.SaveChanges();

        // Assert
        var clienteBD = _db.Clientes.FirstOrDefault(c => c.CPF == "12345678901");
        Assert.NotNull(clienteBD);
        Assert.Equal("João Silva", clienteBD.Nome);
        Assert.Equal(1000m, clienteBD.ValorMensal);
    }

    [Fact]
    public void UpdateCliente_DeveAtualizarCorretamente()
    {
        // Arrange
        var cliente = new Cliente
        {
            Nome = "João Silva",
            CPF = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 1000m,
            Ativo = true,
            DataAdesao = DateTime.UtcNow
        };
        _db.Clientes.Add(cliente);
        _db.SaveChanges();

        // Act
        cliente.ValorMensal = 2000m;
        _db.Clientes.Update(cliente);
        _db.SaveChanges();

        // Assert
        var clienteBD = _db.Clientes.FirstOrDefault(c => c.Id == cliente.Id);
        Assert.Equal(2000m, clienteBD.ValorMensal);
    }

    [Fact]
    public void DeleteCliente_DeveRemoverCorretamente()
    {
        // Arrange
        var cliente = new Cliente
        {
            Nome = "João Silva",
            CPF = "12345678901",
            Email = "joao@email.com",
            ValorMensal = 1000m,
            Ativo = true
        };
        _db.Clientes.Add(cliente);
        _db.SaveChanges();

        var clienteId = cliente.Id;

        // Act
        var clienteRecuperado = _db.Clientes.Find(clienteId);
        _db.Clientes.Remove(clienteRecuperado);
        _db.SaveChanges();

        // Assert
        var clienteBD = _db.Clientes.FirstOrDefault(c => c.Id == clienteId);
        Assert.Null(clienteBD);
    }

    #endregion

    #region Cestas CRUD

    [Fact]
    public void InsertCesta_DeveArmazenarComItens()
    {
        // Arrange
        var cesta = new CestaTopFive
        {
            Nome = "Top Five",
            Ativa = true,
            DataCriacao = DateTime.UtcNow,
            Itens = new List<CestaItem>
            {
                new() { Ticker = "PETR4", Percentual = 30 },
                new() { Ticker = "VALE3", Percentual = 25 },
                new() { Ticker = "ITUB4", Percentual = 20 },
                new() { Ticker = "BBDC4", Percentual = 15 },
                new() { Ticker = "WEGE3", Percentual = 10 }
            }
        };

        // Act
        _db.Cestas.Add(cesta);
        _db.SaveChanges();

        // Assert
        var cestaBD = _db.Cestas.Include(c => c.Itens).FirstOrDefault(c => c.Nome == "Top Five");
        Assert.NotNull(cestaBD);
        Assert.Equal(5, cestaBD.Itens.Count);
    }

    [Fact]
    public void QueryCestaAtiva_DeveRetornarApenasAtivaS()
    {
        // Arrange
        var cesta1 = new CestaTopFive
        {
            Nome = "V1",
            Ativa = false,
            DataCriacao = DateTime.UtcNow.AddDays(-10),
            DataDesativacao = DateTime.UtcNow.AddDays(-5)
        };
        var cesta2 = new CestaTopFive
        {
            Nome = "V2",
            Ativa = true,
            DataCriacao = DateTime.UtcNow
        };

        _db.Cestas.Add(cesta1);
        _db.Cestas.Add(cesta2);
        _db.SaveChanges();

        // Act
        var cestaAtiva = _db.Cestas.FirstOrDefault(c => c.Ativa);

        // Assert
        Assert.NotNull(cestaAtiva);
        Assert.Equal("V2", cestaAtiva.Nome);
    }

    #endregion

    #region Custodia CRUD

    [Fact]
    public void InsertCustodia_ComItens_DeveArmazenarCorretamente()
    {
        // Arrange
        var custodia = new Custodia
        {
            Itens = new List<CustodiaItem>
            {
                new() { Ticker = "PETR4", Quantidade = 100, PrecoMedio = 30.50m },
                new() { Ticker = "VALE3", Quantidade = 50, PrecoMedio = 60.00m }
            }
        };

        // Act
        _db.Custodias.Add(custodia);
        _db.SaveChanges();

        // Assert
        var custodiaBD = _db.Custodias.Include(c => c.Itens).FirstOrDefault();
        Assert.NotNull(custodiaBD);
        Assert.Equal(2, custodiaBD.Itens.Count);
    }

    [Fact]
    public void UpdateCustodiaItem_DeveAtualizarQuantidadeEPreco()
    {
        // Arrange
        var custodia = new Custodia();
        _db.Custodias.Add(custodia);
        _db.SaveChanges();

        var item = new CustodiaItem
        {
            Ticker = "PETR4",
            Quantidade = 100,
            PrecoMedio = 30.50m
        };
        custodia.Itens.Add(item);
        _db.SaveChanges();

        // Act
        item.Quantidade = 150;
        item.PrecoMedio = 31.00m;
        _db.SaveChanges();

        // Assert
        var itemBD = _db.Custodias.Include(c => c.Itens)
            .SelectMany(c => c.Itens)
            .FirstOrDefault(i => i.Ticker == "PETR4");
        Assert.Equal(150, itemBD.Quantidade);
        Assert.Equal(31.00m, itemBD.PrecoMedio);
    }

    #endregion

    #region OrdemCompra CRUD

    [Fact]
    public void InsertOrdenCompra_DeveArmazenarComItens()
    {
        // Arrange
        var cliente = new Cliente { Nome = "João", CPF = "123", ValorMensal = 1000m };
        _db.Clientes.Add(cliente);
        _db.SaveChanges();

        var itens = new List<OrdemCompraItem>
        {
            OrdemCompraItem.Criar("PETR4", 100, 30.50m)
        };
        var ordem = OrdemCompra.Criar(cliente.Id, itens);

        // Act
        _db.OrdensCompra.Add(ordem);
        _db.SaveChanges();

        // Assert
        var ordemBD = _db.OrdensCompra.Include(o => o.Itens).FirstOrDefault();
        Assert.NotNull(ordemBD);
        Assert.Single(ordemBD.Itens);
    }

    [Fact]
    public void QueryOrdensCompraByClienteId_DeveRetornarApenasDoCliente()
    {
        // Arrange
        var cliente1 = new Cliente { Nome = "João", CPF = "123", ValorMensal = 1000m };
        var cliente2 = new Cliente { Nome = "Maria", CPF = "456", ValorMensal = 2000m };
        _db.Clientes.Add(cliente1);
        _db.Clientes.Add(cliente2);
        _db.SaveChanges();

        var itens1 = new List<OrdemCompraItem>
        {
            OrdemCompraItem.Criar("PETR4", 100, 30.50m)
        };
        var itens2 = new List<OrdemCompraItem>
        {
            OrdemCompraItem.Criar("VALE3", 50, 60.00m)
        };

        var ordem1 = OrdemCompra.Criar(cliente1.Id, itens1);
        var ordem2 = OrdemCompra.Criar(cliente2.Id, itens2);

        _db.OrdensCompra.Add(ordem1);
        _db.OrdensCompra.Add(ordem2);
        _db.SaveChanges();

        // Act
        var ordensCliente1 = _db.OrdensCompra.Where(o => o.ClienteId == cliente1.Id).ToList();

        // Assert
        Assert.Single(ordensCliente1);
        Assert.Equal(cliente1.Id, ordensCliente1[0].ClienteId);
    }

    #endregion

    #region Distribuicao CRUD

    [Fact]
    public void InsertDistribuicao_DeveArmazenarComItens()
    {
        // Arrange
        var cliente = new Cliente { Nome = "João", CPF = "123", ValorMensal = 1000m };
        _db.Clientes.Add(cliente);
        _db.SaveChanges();

        var distribuicao = new Distribuicao
        {
            ClienteId = cliente.Id,
            Data = DateTime.UtcNow,
            Itens = new List<DistribuicaoItem>
            {
                new() { Ticker = "PETR4", Quantidade = 50 },
                new() { Ticker = "VALE3", Quantidade = 25 }
            }
        };

        // Act
        _db.Distribuicoes.Add(distribuicao);
        _db.SaveChanges();

        // Assert
        var distribuicaoBD = _db.Distribuicoes
            .Include(d => d.Itens)
            .FirstOrDefault(d => d.ClienteId == cliente.Id);
        Assert.NotNull(distribuicaoBD);
        Assert.Equal(2, distribuicaoBD.Itens.Count);
    }

    #endregion

    #region IrRegistro CRUD

    [Fact]
    public void InsertIrRegistro_DeveArmazenarEvento()
    {
        // Arrange
        var cliente = new Cliente { Nome = "João", CPF = "123", ValorMensal = 1000m };
        _db.Clientes.Add(cliente);
        _db.SaveChanges();

        var irRegistro = new IrRegistro
        {
            ClienteId = cliente.Id,
            Tipo = "DEDO_DURO",
            Ticker = "PETR4",
            MesReferencia = "202602",
            ValorOperacao = 3050m,
            LucroLiquido = 100m,
            Aliquota = 0.005m,
            ValorIR = 15.25m,
            DataEvento = DateTime.UtcNow
        };

        // Act
        _db.IrRegistros.Add(irRegistro);
        _db.SaveChanges();

        // Assert
        var irBD = _db.IrRegistros.FirstOrDefault(i => i.ClienteId == cliente.Id);
        Assert.NotNull(irBD);
        Assert.Equal("DEDO_DURO", irBD.Tipo);
        Assert.Equal(15.25m, irBD.ValorIR);
    }

    [Fact]
    public void QueryIrByClienteAndMes_DeveRetornarApenasDoMes()
    {
        // Arrange
        var cliente = new Cliente { Nome = "João", CPF = "123", ValorMensal = 1000m };
        _db.Clientes.Add(cliente);
        _db.SaveChanges();

        var ir1 = new IrRegistro
        {
            ClienteId = cliente.Id,
            Tipo = "DEDO_DURO",
            Ticker = "PETR4",
            MesReferencia = "202602",
            ValorOperacao = 3050m,
            Aliquota = 0.005m,
            ValorIR = 15.25m,
            DataEvento = DateTime.UtcNow
        };
        var ir2 = new IrRegistro
        {
            ClienteId = cliente.Id,
            Tipo = "VENDA",
            Ticker = "VALE3",
            MesReferencia = "202601",
            ValorOperacao = 2000m,
            Aliquota = 0.2m,
            ValorIR = 400m,
            DataEvento = DateTime.UtcNow.AddMonths(-1)
        };

        _db.IrRegistros.Add(ir1);
        _db.IrRegistros.Add(ir2);
        _db.SaveChanges();

        // Act
        var irsFebreiro = _db.IrRegistros
            .Where(i => i.ClienteId == cliente.Id && i.MesReferencia == "202602")
            .ToList();

        // Assert
        Assert.Single(irsFebreiro);
        Assert.Equal("202602", irsFebreiro[0].MesReferencia);
    }

    #endregion

    #region Complex Queries

    [Fact]
    public void QueryCustodiaComResumo_DeveCalcularValorTotalCorreto()
    {
        // Arrange
        var custodia = new Custodia
        {
            Itens = new List<CustodiaItem>
            {
                new() { Ticker = "PETR4", Quantidade = 100, PrecoMedio = 30.50m },
                new() { Ticker = "VALE3", Quantidade = 50, PrecoMedio = 60.00m }
            }
        };
        _db.Custodias.Add(custodia);
        _db.SaveChanges();

        // Act
        var custodiaBD = _db.Custodias
            .Include(c => c.Itens)
            .FirstOrDefault();

        var valorTotal = custodiaBD.Itens.Sum(i => i.Quantidade * i.PrecoMedio);

        // Assert
        Assert.Equal(3050m + 3000m, valorTotal);
    }

    [Fact]
    public void QueryClienteComHistoricoValoresECustodia_DeveCarregarTudo()
    {
        // Arrange
        var cliente = new Cliente
        {
            Nome = "João Silva",
            CPF = "12345678901",
            ValorMensal = 1000m,
            Ativo = true,
            Custodia = new Custodia()
        };
        _db.Clientes.Add(cliente);
        _db.SaveChanges();

        // Adicionar histórico
        cliente.HistoricoValores.Add(new ValorMensalHistorico
        {
            ValorAnterior = 1000m,
            ValorNovo = 2000m,
            DataAlteracao = DateTime.UtcNow
        });
        _db.SaveChanges();

        // Act
        var clienteBD = _db.Clientes
            .Include(c => c.Custodia)
            .Include(c => c.HistoricoValores)
            .FirstOrDefault(c => c.Id == cliente.Id);

        // Assert
        Assert.NotNull(clienteBD);
        Assert.NotNull(clienteBD.Custodia);
        Assert.Single(clienteBD.HistoricoValores);
    }

    #endregion
}
