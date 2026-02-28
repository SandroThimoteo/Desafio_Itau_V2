using System;
using System.Collections.Generic;
using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Application.Services
{
    public class ClienteService
    {
        // In a complete implementation these would be interfaces injected via DI
        // for repository access. Here we just define method signatures.

        public Cliente AdicionarCliente(string nome, string cpf, string email, decimal valorMensal)
        {
            // validações básicas conforme as regras RN-001 a RN-006
            if (string.IsNullOrWhiteSpace(nome))
                throw new ArgumentException("Nome obrigatório");
            if (string.IsNullOrWhiteSpace(cpf))
                throw new ArgumentException("CPF obrigatório");
            if (valorMensal < 100m)
                throw new ArgumentException("Valor mensal mínimo é R$ 100,00");

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
