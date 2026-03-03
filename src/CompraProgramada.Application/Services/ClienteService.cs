using System;
using System.Collections.Generic;
using System.Linq;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Application.Services
{
    public class ClienteService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ClienteService> _logger;

        public ClienteService(ApplicationDbContext db, ILogger<ClienteService> logger)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logger = logger;
        }

        public Cliente AdicionarCliente(string nome, string cpf, string email, decimal valorMensal)
        {
            _logger.LogInformation("Iniciando adesão de novo cliente - CPF: {CPF}, Nome: {Nome}", cpf, nome);

            // validações básicas conforme as regras RN-001 a RN-006
            if (string.IsNullOrWhiteSpace(nome))
            {
                _logger.LogWarning("Tentativa de adicionar cliente com nome inválido - CPF: {CPF}", cpf);
                throw new ArgumentException("Nome obrigatório", nameof(nome));
            }
            if (string.IsNullOrWhiteSpace(cpf))
            {
                _logger.LogWarning("Tentativa de adicionar cliente com CPF inválido");
                throw new ArgumentException("CPF obrigatório", nameof(cpf));
            }
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                _logger.LogWarning("Tentativa de adicionar cliente com email inválido - CPF: {CPF}", cpf);
                throw new ArgumentException("E-mail inválido", nameof(email));
            }
            if (valorMensal < 100m)
            {
                _logger.LogWarning("Tentativa de adicionar cliente com valor mensal inválido - Valor: R$ {ValorMensal}", valorMensal);
                throw new ArgumentException("O valor mensal minimo e de R$ 100,00.", "VALOR_MENSAL_INVALIDO");
            }
            
            // RN-002: CPF deve ser único (validação de duplicidade)
            if (_db.Clientes.Any(c => c.CPF == cpf))
            {
                _logger.LogWarning("Tentativa de adicionar cliente com CPF duplicado - CPF: {CPF}", cpf);
                throw new ArgumentException("CPF ja cadastrado no sistema.", "CLIENTE_CPF_DUPLICADO");
            }

            var cliente = new Cliente
            {
                Nome = nome,
                CPF = cpf,
                Email = email,
                ValorMensal = valorMensal,
                Ativo = true,
                DataAdesao = DateTime.UtcNow,
                ContaGrafica = new ContaGrafica
                {
                    NumeroConta = "FLH-PENDENTE",
                    Tipo = ContaTipo.Filhote,
                    DataCriacao = DateTime.UtcNow
                },
                Custodia = new Custodia()
            };

            _logger.LogInformation(
                "Cliente criado com sucesso - CPF: {CPF}, Conta Gráfica: {ContaGrafica}, Valor Mensal: R$ {ValorMensal}",
                cpf,
                cliente.ContaGrafica.NumeroConta,
                valorMensal
            );

            // salvamento em banco ficaria aqui
            return cliente;
        }

        public Cliente SairDoProduto(Cliente cliente)
        {
            if (cliente == null) throw new ArgumentNullException(nameof(cliente));
            if (!cliente.Ativo)
            {
                _logger.LogWarning("Tentativa de saída de cliente já inativo - ClienteId: {ClienteId}", cliente.Id);
                throw new InvalidOperationException("CLIENTE_JA_INATIVO"); // código de erro padrão
            }
            
            _logger.LogInformation("Cliente {ClienteId} saindo do produto", cliente.Id);
            cliente.Ativo = false;
            cliente.DataSaida = DateTime.UtcNow;
            return cliente;
        }

        public void AlterarValorMensal(Cliente cliente, decimal novoValor)
        {
            if (cliente == null) throw new ArgumentNullException(nameof(cliente));
            if (novoValor < 100m)
                throw new ArgumentException("O valor mensal minimo e de R$ 100,00.", "VALOR_MENSAL_INVALIDO");
            decimal anterior = cliente.ValorMensal;
            
            _logger.LogInformation(
                "Alterando valor mensal do cliente {ClienteId} - De: R$ {ValorAnterior} Para: R$ {NovoValor}",
                cliente.Id,
                anterior,
                novoValor
            );
            
            cliente.ValorMensal = novoValor;
            cliente.HistoricoValores.Add(new ValorMensalHistorico
            {
                ValorAnterior = anterior,
                ValorNovo = novoValor,
                DataAlteracao = DateTime.UtcNow
            });
        }
    }
}