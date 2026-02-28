using System;
using System.Collections.Generic;
using System.Linq;
using CompraProgramada.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Application.Services
{
    public class CestaService
    {
        private readonly ILogger<CestaService> _logger;

        public CestaService(ILogger<CestaService> logger)
        {
            _logger = logger;
        }

        // Repositórios seriam injetados

        public CestaTopFive CriarOuAtualizarCesta(string nome, List<CestaItem> itens, CestaTopFive? cestaAnterior = null)
        {
            _logger.LogInformation("Criando ou atualizando cesta Top Five - Nome: {CestaNome}, Quantidade de Itens: {QtdItens}", nome, itens?.Count ?? 0);

            if (itens == null || itens.Count != 5)
            {
                _logger.LogError("Quantidade de itens inválida na cesta - Esperado: 5, Recebido: {QtdItens}", itens?.Count ?? 0);
                throw new ArgumentException("A cesta deve conter exatamente 5 ativos.", "QUANTIDADE_ATIVOS_INVALIDA");
            }

            decimal soma = itens.Sum(i => i.Percentual);
            if (soma != 100m)
            {
                _logger.LogError("Soma de percentuais inválida - Esperado: 100%, Recebido: {SomaPercentuais}", soma);
                throw new ArgumentException($"A soma dos percentuais deve ser exatamente 100%. Soma atual: {soma}.", "PERCENTUAIS_INVALIDOS");
            }

            var cesta = new CestaTopFive
            {
                Nome = nome,
                Ativa = true,
                DataCriacao = DateTime.UtcNow,
                Itens = itens
            };

            if (cestaAnterior != null)
            {
                _logger.LogInformation(
                    "Desativando cesta anterior - CestaAnteriorId: {CestaAnteriorId}, Nova CestaId será gerada",
                    cestaAnterior.Id
                );
                cestaAnterior.Ativa = false;
                cestaAnterior.DataDesativacao = DateTime.UtcNow;
                // disparar rebalanceamento para clientes ativos...
            }

            return cesta;
        }
    }
}
