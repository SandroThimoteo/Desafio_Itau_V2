using System;
using System.Collections.Generic;
using System.Linq;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Services;
using CompraProgramada.Infrastructure;

namespace CompraProgramada.Application.Services
{
    public class MotorCompraService
    {
        private readonly CotahistParser _parser = new CotahistParser();

        /// <summary>
        /// Executa a compra programada na data informada (dias 5/15/25 ou adiados para útil).
        /// </summary>
        public MotorResultado ExecutarCompra(DateTime dataReferencia, List<Cliente> clientesAtivos, CestaTopFive cesta, Dictionary<string, decimal> saldoMaster)
        {
            // calculo de 1/3 do valor mensal para cada cliente
            decimal totalConsolidado = clientesAtivos.Sum(c => c.ValorMensal / 3m);
            var resultado = new MotorResultado
            {
                DataExecucao = DateTime.UtcNow,
                TotalClientes = clientesAtivos.Count,
                TotalConsolidado = totalConsolidado
            };

            // buscar cotações de cada ticker
            var cotacoes = new Dictionary<string, CotacaoB3>();
            foreach (var item in cesta.Itens)
            {
                var cot = _parser.ObterCotacaoFechamento("cotacoes", item.Ticker);
                if (cot == null) throw new Exception($"COTACAO_NAO_ENCONTRADA: {item.Ticker}");
                cotacoes[item.Ticker] = cot;
            }

            // calcular quantidade a comprar
            var ordens = new List<OrdemCompraItem>();
            foreach (var item in cesta.Itens)
            {
                decimal valorAtivo = totalConsolidado * (item.Percentual / 100m);
                var cot = cotacoes[item.Ticker];

                decimal qtd = Math.Floor(valorAtivo / cot.PrecoFechamento);
                if (saldoMaster.TryGetValue(item.Ticker, out var saldoAnterior))
                {
                    qtd = Math.Max(0, qtd - saldoAnterior);
                }

                // separar padrão / fracionário
                decimal lotes = Math.Floor(qtd / 100m) * 100m;
                decimal fracionario = qtd - lotes;

                if (lotes > 0)
                    ordens.Add(OrdemCompraItem.Criar(item.Ticker, lotes, cot.PrecoFechamento, fracionario: false));
                if (fracionario > 0)
                    ordens.Add(OrdemCompraItem.Criar(item.Ticker + "F", fracionario, cot.PrecoFechamento, fracionario: true));
            }

            resultado.Ordens = ordens;
            
            // distribuição simplificada omitida
            return resultado;
        }
    }

    public class MotorResultado
    {
        public DateTime DataExecucao { get; set; }
        public int TotalClientes { get; set; }
        public decimal TotalConsolidado { get; set; }
        public List<OrdemCompraItem> Ordens { get; set; } = new List<OrdemCompraItem>();
    }
}
