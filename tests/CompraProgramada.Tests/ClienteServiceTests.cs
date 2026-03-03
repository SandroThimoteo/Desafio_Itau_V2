using System;
using System.Collections.Generic;
using Xunit;
using Microsoft.EntityFrameworkCore;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Application.Services;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Tests;

public class ClienteServiceTests : IDisposable
{
    private readonly ClienteService _clienteService;
    private readonly ApplicationDbContext _db;

    public ClienteServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);
        _db.Database.EnsureCreated();
        _clienteService = new ClienteService(_db, LoggerTestHelper.CreateMockLogger<ClienteService>());
    }

    public void Dispose()
    {
        _db?.Dispose();
    }

    [Fact]
    public void AdicionarCliente_ComDadosValidos_DeveRetornarClienteComContaGrafica()
    {
        // Act
        var cliente = _clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            1000m
        );

        // Assert
        Assert.NotNull(cliente);
        Assert.Equal("João Silva", cliente.Nome);
        Assert.Equal("12345678901", cliente.CPF);
        Assert.Equal("joao@email.com", cliente.Email);
        Assert.Equal(1000m, cliente.ValorMensal);
        Assert.True(cliente.Ativo);
        Assert.NotNull(cliente.ContaGrafica);
        Assert.Equal(ContaTipo.Filhote, cliente.ContaGrafica.Tipo);
        Assert.StartsWith("FLH-", cliente.ContaGrafica.NumeroConta);
    }

    [Fact]
    public void AdicionarCliente_ComNomeVazio_DeveThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _clienteService.AdicionarCliente("", "12345678901", "joao@email.com", 1000m)
        );
        Assert.Contains("Nome obrigatório", ex.Message);
    }

    [Fact]
    public void AdicionarCliente_ComCPFVazio_DeveThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _clienteService.AdicionarCliente("João Silva", "", "joao@email.com", 1000m)
        );
        Assert.Contains("CPF obrigatório", ex.Message);
    }

    [Fact]
    public void AdicionarCliente_ComValorMenorQueMinimo_DeveThrowArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _clienteService.AdicionarCliente("João Silva", "12345678901", "joao@email.com", 50m)
        );
        Assert.Contains("mínimo", ex.Message);
    }

    [Fact]
    public void AdicionarCliente_ComValorIgualAoMinimo_DeveCriarClienteComSucesso()
    {
        // Act
        var cliente = _clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            100m
        );

        // Assert
        Assert.NotNull(cliente);
        Assert.Equal(100m, cliente.ValorMensal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void AdicionarCliente_ComNomeInvalido_DeveThrowArgumentException(string nome)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _clienteService.AdicionarCliente(nome, "12345678901", "joao@email.com", 1000m)
        );
    }

    [Fact]
    public void SairDoProduto_ComClienteAtivo_DeveDesativarCliente()
    {
        // Arrange
        var cliente = _clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            1000m
        );
        Assert.True(cliente.Ativo);

        // Act
        var resultado = _clienteService.SairDoProduto(cliente);

        // Assert
        Assert.False(resultado.Ativo);
        Assert.NotNull(resultado.DataSaida);
    }

    [Fact]
    public void SairDoProduto_ComClienteInativo_DeveThrowInvalidOperationException()
    {
        // Arrange
        var cliente = _clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            1000m
        );
        _clienteService.SairDoProduto(cliente);
        Assert.False(cliente.Ativo);

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            _clienteService.SairDoProduto(cliente)
        );
        Assert.Contains("CLIENTE_JA_INATIVO", ex.Message);
    }

    [Fact]
    public void SairDoProduto_ComClienteNull_DeveThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _clienteService.SairDoProduto(null)
        );
    }

    [Fact]
    public void AlterarValorMensal_ComClienteAtivo_DeveAtualizarValor()
    {
        // Arrange
        var cliente = _clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            1000m
        );
        decimal valorAnterior = cliente.ValorMensal;

        // Act
        _clienteService.AlterarValorMensal(cliente, 2000m);

        // Assert
        Assert.Equal(2000m, cliente.ValorMensal);
        Assert.Single(cliente.HistoricoValores);
        Assert.Equal(valorAnterior, cliente.HistoricoValores[0].ValorAnterior);
        Assert.Equal(2000m, cliente.HistoricoValores[0].ValorNovo);
    }

    [Fact]
    public void AlterarValorMensal_ComMultiplasAlteracoes_DeveManterHistoricoCompleto()
    {
        // Arrange
        var cliente = _clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            1000m
        );

        // Act
        _clienteService.AlterarValorMensal(cliente, 2000m);
        _clienteService.AlterarValorMensal(cliente, 3000m);
        _clienteService.AlterarValorMensal(cliente, 1500m);

        // Assert
        Assert.Equal(1500m, cliente.ValorMensal);
        Assert.Equal(3, cliente.HistoricoValores.Count);
        Assert.Equal(1000m, cliente.HistoricoValores[0].ValorAnterior);
        Assert.Equal(2000m, cliente.HistoricoValores[0].ValorNovo);
        Assert.Equal(3000m, cliente.HistoricoValores[1].ValorNovo);
        Assert.Equal(1500m, cliente.HistoricoValores[2].ValorNovo);
    }

    [Fact]
    public void AlterarValorMensal_ComClienteNull_DeveThrowArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _clienteService.AlterarValorMensal(null, 2000m)
        );
    }

    [Fact]
    public void AlterarValorMensal_ComValorZero_DeveAtualizarValor()
    {
        // Arrange
        var cliente = _clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            1000m
        );

        // Act
        _clienteService.AlterarValorMensal(cliente, 0m);

        // Assert
        Assert.Equal(0m, cliente.ValorMensal);
    }

    [Fact]
    public void AlterarValorMensal_ComValorNegativo_DeveAtualizarValor()
    {
        // Arrange
        var cliente = _clienteService.AdicionarCliente(
            "João Silva",
            "12345678901",
            "joao@email.com",
            1000m
        );

        // Act
        _clienteService.AlterarValorMensal(cliente, -500m);

        // Assert
        Assert.Equal(-500m, cliente.ValorMensal);
    }
}
