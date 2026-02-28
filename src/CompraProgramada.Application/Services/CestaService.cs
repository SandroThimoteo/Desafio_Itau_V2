using System;
using System.Collections.Generic;
using System.Linq;
using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Application.Services
{
    public class CestaService
    {
        // Repositórios seriam injetados

        public CestaTopFive CriarOuAtualizarCesta(string nome, List<CestaItem> itens, CestaTopFive? cestaAnterior = null)
        {
            if (itens == null || itens.Count != 5)
                throw new ArgumentException("A cesta deve conter exatamente 5 ativos.", "QUANTIDADE_ATIVOS_INVALIDA");

            decimal soma = itens.Sum(i => i.Percentual);
            if (soma != 100m)
                throw new ArgumentException($"A soma dos percentuais deve ser exatamente 100%. Soma atual: {soma}.", "PERCENTUAIS_INVALIDOS");

            var cesta = new CestaTopFive
            {
                Nome = nome,
                Ativa = true,
                DataCriacao = DateTime.UtcNow,
                Itens = itens
            };

            if (cestaAnterior != null)
            {
                cestaAnterior.Ativa = false;
                cestaAnterior.DataDesativacao = DateTime.UtcNow;
                // disparar rebalanceamento para clientes ativos...
            }

            return cesta;
        }
    }
}
