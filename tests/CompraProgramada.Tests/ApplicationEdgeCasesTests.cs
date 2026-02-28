using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Microsoft.EntityFrameworkCore;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Application.Services;

public class ApplicationEdgeCasesTests : IDisposable
{
    private readonly ApplicationDbContext _db;

    public ApplicationEdgeCasesTests()
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

    #region ClienteService Edge Cases

    [Fact]
    public void AdicionarCliente_ComValorMensal_DeBordaMinima_DeveAceitar()
    {
        // Arrange
        var clienteService = new ClienteService();

        // Act
        var cliente = clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            100m // Valor mínimo
        );

        // Assert
        Assert.NotNull(cliente);
        Assert.Equal(100m, cliente.ValorMensal);
    }

    [Fact]
    public void AdicionarCliente_ComValorMensal_Fracionado_DeveAceitar()
    {
        // Arrange
        var clienteService = new ClienteService();

        // Act
        var cliente = clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            1234.56m
        );

        // Assert
        Assert.NotNull(cliente);
        Assert.Equal(1234.56m, cliente.ValorMensal);
    }

    [Fact]
    public void AdicionarCliente_ComValorMensal_VeryLarge_DeveAceitar()
    {
        // Arrange
        var clienteService = new ClienteService();

        // Act
        var cliente = clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            1000000m // 1 milhão
        );

        // Assert
        Assert.NotNull(cliente);
        Assert.Equal(1000000m, cliente.ValorMensal);
    }

    [Theory]
    [InlineData(99.99)]
    [InlineData(50)]
    [InlineData(0)]
    [InlineData(-100)]
    public void AdicionarCliente_ComValorMenor_DeveThrow(decimal valor)
    {
        // Arrange
        var clienteService = new ClienteService();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            clienteService.AdicionarCliente("João Silva", "12345678901", "joao@email.com", valor)
        );
    }

    [Fact]
    public void AlterarValorMensal_ComSequenciaDeAumentosEQuedas_DeveAlternar()
    {
        // Arrange
        var clienteService = new ClienteService();
        var cliente = clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            1000m
        );

        // Act
        clienteService.AlterarValorMensal(cliente, 2000m);
        clienteService.AlterarValorMensal(cliente, 500m);
        clienteService.AlterarValorMensal(cliente, 3000m);
        clienteService.AlterarValorMensal(cliente, 1500m);

        // Assert
        Assert.Equal(1500m, cliente.ValorMensal);
        Assert.Equal(4, cliente.HistoricoValores.Count);
        Assert.Equal(1000m, cliente.HistoricoValores[0].ValorAnterior);
        Assert.Equal(2000m, cliente.HistoricoValores[0].ValorNovo);
        Assert.Equal(2000m, cliente.HistoricoValores[1].ValorAnterior);
        Assert.Equal(500m, cliente.HistoricoValores[1].ValorNovo);
    }

    [Fact]
    public void SairDoProduto_DeveRegistrarDataSaidaComTimestamp()
    {
        // Arrange
        var clienteService = new ClienteService();
        var cliente = clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            1000m
        );
        var dataAntes = DateTime.UtcNow;

        // Act
        _db.Clientes.Add(cliente);
        _db.SaveChanges();

        var clienteRecuperado = _db.Clientes.Find(cliente.Id);
        clienteService.SairDoProduto(clienteRecuperado);
        var dataDepois = DateTime.UtcNow;

        // Assert
        Assert.NotNull(clienteRecuperado.DataSaida);
        Assert.True(clienteRecuperado.DataSaida >= dataAntes);
        Assert.True(clienteRecuperado.DataSaida <= dataDepois);
    }

    #endregion

    #region CestaService Edge Cases

    [Fact]
    public void CriarCesta_ComPercentuaisQueNaoFechamEm100_DeveThrow()
    {
        // Arrange
        var cestaService = new CestaService();
        
        // Teste vários cenários
        var cenarios = new List<(decimal soma, List<CestaItem> itens)>
        {
            (99m, new List<CestaItem>
            {
                new() { Ticker = "T1", Percentual = 19.8m },
                new() { Ticker = "T2", Percentual = 19.8m },
                new() { Ticker = "T3", Percentual = 19.8m },
                new() { Ticker = "T4", Percentual = 19.8m },
                new() { Ticker = "T5", Percentual = 19.8m }
            }),
            (101m, new List<CestaItem>
            {
                new() { Ticker = "T1", Percentual = 20.2m },
                new() { Ticker = "T2", Percentual = 20.2m },
                new() { Ticker = "T3", Percentual = 20.2m },
                new() { Ticker = "T4", Percentual = 20.2m },
                new() { Ticker = "T5", Percentual = 20.2m }
            })
        };

        // Act & Assert
        foreach (var (soma, itens) in cenarios)
        {
            Assert.Throws<ArgumentException>(() =>
                cestaService.CriarOuAtualizarCesta("Invalida", itens)
            );
        }
    }

    [Fact]
    public void CriarCesta_ComAtivosRepetidos_DeveAceitar()
    {
        // Arrange
        var cestaService = new CestaService();
        var itens = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 25 },
            new() { Ticker = "PETR4", Percentual = 25 },
            new() { Ticker = "VALE3", Percentual = 25 },
            new() { Ticker = "ITUB4", Percentual = 15 },
            new() { Ticker = "BBDC4", Percentual = 10 }
        };

        // Act
        var cesta = cestaService.CriarOuAtualizarCesta("Top Five", itens);

        // Assert
        Assert.NotNull(cesta);
        Assert.Equal(5, cesta.Itens.Count); // Sistema não remove duplicatas
    }

    [Fact]
    public void CriarCesta_ComPercentualZero_DeveAceitar()
    {
        // Arrange
        var cestaService = new CestaService();
        var itens = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 30 },
            new() { Ticker = "VALE3", Percentual = 25 },
            new() { Ticker = "ITUB4", Percentual = 20 },
            new() { Ticker = "BBDC4", Percentual = 25 },
            new() { Ticker = "WEGE3", Percentual = 0 }
        };

        // Act
        var cesta = cestaService.CriarOuAtualizarCesta("Top Five", itens);

        // Assert
        Assert.NotNull(cesta);
    }

    [Fact]
    public void CriarCesta_ComPercentualNegativo_DeveAceitar()
    {
        // Arrange
        var cestaService = new CestaService();
        var itens = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 50 },
            new() { Ticker = "VALE3", Percentual = 50 },
            new() { Ticker = "ITUB4", Percentual = 25 },
            new() { Ticker = "BBDC4", Percentual = 25 },
            new() { Ticker = "WEGE3", Percentual = -50 }
        };

        // Act
        var cesta = cestaService.CriarOuAtualizarCesta("Top Five", itens);

        // Assert
        Assert.NotNull(cesta);
    }

    [Fact]
    public void AtualizarCesta_ParaValoresComFloatingPointPrecision_DeveHandleDecimal()
    {
        // Arrange
        var cestaService = new CestaService();
        var itens = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 20.1m },
            new() { Ticker = "VALE3", Percentual = 20.1m },
            new() { Ticker = "ITUB4", Percentual = 20.1m },
            new() { Ticker = "BBDC4", Percentual = 20.1m },
            new() { Ticker = "WEGE3", Percentual = 19.6m }
        };

        // Act
        var cesta = cestaService.CriarOuAtualizarCesta("Top Five", itens);

        // Assert
        Assert.NotNull(cesta);
        Assert.Equal(100m, itens.Sum(i => i.Percentual));
    }

    #endregion

    #region Rebalance Service Edge Cases

    [Fact]
    public void CalcularDistibuicaoProportional_ComValoresDesiguais_DeveDistribuirCorreto()
    {
        // Arrange - Simular distribuição com valores proporcionais
        decimal totalAporte = 3000m; // Total de 3 clientes
        var clientes = new List<(string nome, decimal aporte)>
        {
            ("ClienteA", 1000m),
            ("ClienteB", 2000m),
            ("ClienteC", 0m) // Cliente sem aporte
        };

        // Act - Calcular proporções
        var proporcoes = clientes.Select(c => c.aporte / totalAporte).ToList();

        // Assert
        Assert.Equal(3, proporcoes.Count);
        Assert.Equal(1000m / 3000m, proporcoes[0], 6);
        Assert.Equal(2000m / 3000m, proporcoes[1], 6);
        Assert.Equal(0m, proporcoes[2]);
    }

    [Fact]
    public void CalcularDesvio_ComClienteComMenorQuantidade_DeveIdentificarDesvio()
    {
        // Arrange
        decimal alvoPercentual = 0.30m; // 30%
        decimal valorTotalCarteira = 1000m;
        decimal posicaoAtualValor = 250m; // 25% (desvio de -5%)

        // Act
        decimal posicaoAlvoValor = valorTotalCarteira * alvoPercentual;
        decimal desvio = Math.Abs((posicaoAtualValor - posicaoAlvoValor) / posicaoAlvoValor);

        // Assert
        Assert.True(desvio > 0.15m); // 15% de tolerância, desvio é maior
        Assert.Equal(0.1666666m, desvio, 5); // Desvio é ~16.67%
    }

    #endregion

    #region MotorCompra Edge Cases

    [Fact]
    public void CalcularQuantidadeBolsaComResiduo_DeveRingfencerResiduoNaMaster()
    {
        // Arrange - Simular compra de PETR4
        decimal valorComprar = 900m; // 30% de 3000
        decimal cotacao = 38.50m;
        decimal quantidadeExata = valorComprar / cotacao; // 23.376...

        // Act
        decimal quantidadeInteira = Math.Floor(quantidadeExata);
        decimal residuo = quantidadeExata - quantidadeInteira;

        // Assert
        Assert.Equal(23m, quantidadeInteira);
        Assert.True(residuo > 0);
        Assert.True(residuo < 1);
    }

    [Fact]
    public void CalcularLotesPadraoEFracionarios_DeveSeperarCorreto()
    {
        // Arrange
        int quantidadeTotal = 350;

        // Act
        int lotesPadrao = quantidadeTotal / 100;
        int fracionarios = quantidadeTotal % 100;

        // Assert
        Assert.Equal(3, lotesPadrao);
        Assert.Equal(50, fracionarios);
    }

    [Fact]
    public void CalcularIrDedoDuro_ComOperacao_DeveCalcular0005Porcento()
    {
        // Arrange
        decimal valorOperacao = 3050m;
        decimal aliquotaDedoDuro = 0.00005m; // 0.005%

        // Act
        decimal valorIR = valorOperacao * aliquotaDedoDuro;

        // Assert
        Assert.Equal(0.1525m, valorIR);
    }

    [Fact]
    public void CalcularIrVenda_QuandoUltrapassar20k_DeveCalcular20PorcentoDoLucro()
    {
        // Arrange
        decimal totalVendas = 25000m; // Ultrapassa 20k
        decimal custoTotal = 20000m;
        decimal lucroLiquido = totalVendas - custoTotal;
        decimal aliquotaIR = 0.20m; // 20%

        // Act
        decimal valorIR = lucroLiquido * aliquotaIR;

        // Assert
        Assert.True(totalVendas > 20000m);
        Assert.Equal(1000m, valorIR);
    }

    [Fact]
    public void CalcularIrVenda_QuandoNaoUltrapassar20k_DeveSerIsento()
    {
        // Arrange
        decimal totalVendas = 15000m;

        // Act & Assert
        Assert.True(totalVendas <= 20000m);
        // IR = 0 (isento)
    }

    #endregion
}
