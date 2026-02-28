using System;
using System.Collections.Generic;
using System.Linq;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;

namespace CompraProgramada.Application.Services
{
    public class ClienteService
    {
        private readonly ApplicationDbContext _db;

        public ClienteService(ApplicationDbContext db)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public Cliente AdicionarCliente(string nome, string cpf, string email, decimal valorMensal)
        {
            // validações básicas conforme as regras RN-001 a RN-006
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome obrigatório", nameof(nome));
            if (string.IsNullOrWhiteSpace(cpf))
                throw new ArgumentException("CPF obrigatório", nameof(cpf));
            if (valorMensal < 100m)
                throw new ArgumentException("Valor mensal mínimo é R$ 100,00", nameof(valorMensal));
            
            // RN-002: CPF deve ser único (validação de duplicidade)
            if (_db.Clientes.Any(c => c.CPF == cpf))
                throw new ArgumentException("CPF ja cadastrado no sistema.", "CLIENTE_CPF_DUPLICADO");

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
                    NumeroConta = "FLH-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper(),
                    Tipo = ContaTipo.Filhote,
                    DataCriacao = DateTime.UtcNow
                },
                Custodia = new Custodia()
            };

            // salvamento em banco ficaria aqui
            return cliente;
        }

        public Cliente SairDoProduto(Cliente cliente)
        {
            if (cliente == null) throw new ArgumentNullException(nameof(cliente));
            if (!cliente.Ativo)
                throw new InvalidOperationException("CLIENTE_JA_INATIVO"); // código de erro padrão
            cliente.Ativo = false;
            cliente.DataSaida = DateTime.UtcNow;
            return cliente;
        }

        public void AlterarValorMensal(Cliente cliente, decimal novoValor)
        {
            if (cliente == null) throw new ArgumentNullException(nameof(cliente));
            decimal anterior = cliente.ValorMensal;
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
