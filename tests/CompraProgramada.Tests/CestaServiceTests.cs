using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Application.Services;

public class CestaServiceTests
{
    private readonly CestaService _cestaService;

    public CestaServiceTests()
    {
        _cestaService = new CestaService();
    }

    [Fact]
    public void CriarOuAtualizarCesta_ComValidacaoCompleta_DeveRetornarCestaValida()
    {
        // Arrange
        var itens = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 30 },
            new() { Ticker = "VALE3", Percentual = 25 },
            new() { Ticker = "ITUB4", Percentual = 20 },
            new() { Ticker = "BBDC4", Percentual = 15 },
            new() { Ticker = "WEGE3", Percentual = 10 }
        };

        // Act
        var cesta = _cestaService.CriarOuAtualizarCesta("Top Five", itens);

        // Assert
        Assert.NotNull(cesta);
        Assert.Equal("Top Five", cesta.Nome);
        Assert.True(cesta.Ativa);
        Assert.Equal(5, cesta.Itens.Count);
        Assert.Equal(100m, cesta.Itens.Sum(i => i.Percentual));
    }

    [Fact]
    public void CriarOuAtualizarCesta_ComQuantidadeInvalida_DeveThrowArgumentException()
    {
        // Arrange
        var itens = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 50 },
            new() { Ticker = "VALE3", Percentual = 50 }
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            _cestaService.CriarOuAtualizarCesta("Invalida", itens)
        );
        Assert.Contains("5 ativos", ex.Message);
    }

    [Fact]
    public void CriarOuAtualizarCesta_ComPercentuaiNaoSomando100_DeveThrowArgumentException()
    {
        // Arrange
        var itens = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 30 },
            new() { Ticker = "VALE3", Percentual = 25 },
            new() { Ticker = "ITUB4", Percentual = 20 },
            new() { Ticker = "BBDC4", Percentual = 15 },
            new() { Ticker = "WEGE3", Percentual = 9 } // Total = 99
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            _cestaService.CriarOuAtualizarCesta("Invalida", itens)
        );
        Assert.Contains("100%", ex.Message);
    }

    [Fact]
    public void CriarOuAtualizarCesta_ComPercentuaiMaiorQue100_DeveThrowArgumentException()
    {
        // Arrange
        var itens = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 40 },
            new() { Ticker = "VALE3", Percentual = 30 },
            new() { Ticker = "ITUB4", Percentual = 20 },
            new() { Ticker = "BBDC4", Percentual = 15 },
            new() { Ticker = "WEGE3", Percentual = 5 } // Total = 110
        };

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            _cestaService.CriarOuAtualizarCesta("Invalida", itens)
        );
        Assert.Contains("100%", ex.Message);
    }

    [Fact]
    public void CriarOuAtualizarCesta_ComItensNull_DeveThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => 
            _cestaService.CriarOuAtualizarCesta("Invalida", null)
        );
        Assert.Contains("5 ativos", ex.Message);
    }

    [Fact]
    public void CriarOuAtualizarCesta_ComCestaAnterior_DeveDesativarCestaAnterior()
    {
        // Arrange
        var ItensAnterior = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 20 },
            new() { Ticker = "VALE3", Percentual = 20 },
            new() { Ticker = "ITUB4", Percentual = 20 },
            new() { Ticker = "BBDC4", Percentual = 20 },
            new() { Ticker = "WEGE3", Percentual = 20 }
        };

        var cestaAnterior = _cestaService.CriarOuAtualizarCesta("Antiga", ItensAnterior);
        Assert.True(cestaAnterior.Ativa);

        var itensNova = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 30 },
            new() { Ticker = "VALE3", Percentual = 25 },
            new() { Ticker = "ITUB4", Percentual = 20 },
            new() { Ticker = "BBDC4", Percentual = 15 },
            new() { Ticker = "WEGE3", Percentual = 10 }
        };

        // Act
        var cestaNova = _cestaService.CriarOuAtualizarCesta("Nova", itensNova, cestaAnterior);

        // Assert
        Assert.False(cestaAnterior.Ativa);
        Assert.NotNull(cestaAnterior.DataDesativacao);
        Assert.True(cestaNova.Ativa);
    }

    [Fact]
    public void CriarOuAtualizarCesta_ComPercentuaisDecimais_DeveAceitarPercentiaisCorretos()
    {
        // Arrange
        var itens = new List<CestaItem>
        {
            new() { Ticker = "PETR4", Percentual = 30.5m },
            new() { Ticker = "VALE3", Percentual = 25.5m },
            new() { Ticker = "ITUB4", Percentual = 20.0m },
            new() { Ticker = "BBDC4", Percentual = 14.0m },
            new() { Ticker = "WEGE3", Percentual = 10.0m }
        };

        // Act
        var cesta = _cestaService.CriarOuAtualizarCesta("Top Five", itens);

        // Assert
        Assert.NotNull(cesta);
        Assert.Equal(100m, itens.Sum(i => i.Percentual));
    }
}
