using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Services;
using CompraProgramada.Infrastructure;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CompraProgramada.Application.Services
{
    public class RebalanceService : IRebalanceService
    {
        private readonly ApplicationDbContext _db;
        private readonly IrPublisher _irPublisher;
        private readonly CotahistParser _parser = new CotahistParser();
        private readonly ILogger<RebalanceService> _logger;

        private readonly string _pastaCotacoes;

        public RebalanceService(ApplicationDbContext db, IrPublisher irPublisher, ILogger<RebalanceService> logger, IConfiguration configuration)
        {
            _db = db;
            _irPublisher = irPublisher;
            _logger = logger;
            _pastaCotacoes = configuration["Cotacoes:PastaCotahist"] ?? "cotacoes";
        }

        public async Task<RebalanceResultado> RebalancearPorMudancaDeCestaAsync(long cestaId, DateTime dataReferencia, CancellationToken ct = default)
        {
            _logger.LogInformation(
                "Iniciando rebalanceamento por mudança de cesta - CestaId: {CestaId}, DataReferencia: {DataReferencia}",
                cestaId,
                dataReferencia.Date
            );

            var resultado = new RebalanceResultado();

            var cesta = await _db.Cestas
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Id == cestaId, ct);

            if (cesta == null)
            {
                _logger.LogError("Cesta não encontrada - CestaId: {CestaId}", cestaId);
                resultado.Mensagens.Add("Cesta informada nao encontrada.");
                return resultado;
            }

            var clientes = await _db.Clientes
                .Include(c => c.Custodia)
                .ThenInclude(c => c.Itens)
                .Where(c => c.Ativo)
                .ToListAsync(ct);

            resultado.ClientesProcessados = clientes.Count;
            
            _logger.LogInformation(
                "Rebalanceamento iniciado para {ClientesCount} clientes ativos",
                clientes.Count
            );            foreach (var cliente in clientes)
            {
                if (cliente.Custodia == null)
                    continue;

                var tickersAlvo = cesta.Itens
                    .Select(i => i.Ticker)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                var ativosFora = cliente.Custodia.Itens
                    .Where(i => !tickersAlvo.Contains(RemoverSufixoFracionario(i.Ticker)) && i.Quantidade > 0)
                    .ToList();

                if (!ativosFora.Any())
                    continue;

                decimal valorTotalVendidoCliente = 0m;
                decimal totalVendasMes = 0m;
                var lucros = new List<decimal>();

                foreach (var ativo in ativosFora)
                {
                    var cotacao = _parser.ObterCotacaoFechamento(_pastaCotacoes, RemoverSufixoFracionario(ativo.Ticker));
                    var precoVenda = cotacao?.PrecoFechamento ?? ativo.PrecoMedio;

                    decimal valorVenda = ativo.Quantidade * precoVenda;
                    decimal lucroAtivo = ativo.Quantidade * (precoVenda - ativo.PrecoMedio);

                    valorTotalVendidoCliente += valorVenda;
                    totalVendasMes += valorVenda;
                    lucros.Add(lucroAtivo);

                    resultado.OrdensVendaGeradas++;
                    resultado.ValorTotalVendido += valorVenda;

                    ativo.Quantidade = 0m;
                }

                cliente.Custodia.Itens = cliente.Custodia.Itens.Where(i => i.Quantidade > 0m).ToList();

                var itensCompra = CriarComprasPorPercentual(cesta, valorTotalVendidoCliente);
                if (itensCompra.Any())
                {
                    var ordem = OrdemCompra.Criar(cliente.Id, itensCompra);
                    ordem.MarcarExecutada();
                    _db.OrdensCompra.Add(ordem);

                    AtualizarCustodiaCompra(cliente, itensCompra);

                    resultado.OrdensCompraGeradas += itensCompra.Count;
                    resultado.ValorTotalComprado += itensCompra.Sum(i => i.ValorItem);
                }

                var irVenda = CalculosFinanceiros.CalcularIrVendaMonthly(totalVendasMes, lucros);
                if (totalVendasMes > 20000m)
                {
                    var evt = new IrVendaEvent
                    {
                        ClienteId = cliente.Id,
                        CPF = cliente.CPF,
                        MesReferencia = dataReferencia.ToString("yyyy-MM"),
                        TotalVendasMes = totalVendasMes,
                        LucroLiquido = lucros.Sum(),
                        ValorIR = irVenda,
                        DataCalculo = DateTime.UtcNow
                    };

                    _db.IrRegistros.Add(new IrRegistro
                    {
                        ClienteId = cliente.Id,
                        Tipo = "VENDA",
                        Ticker = null,
                        MesReferencia = evt.MesReferencia,
                        ValorOperacao = totalVendasMes,
                        LucroLiquido = evt.LucroLiquido,
                        Aliquota = evt.Aliquota,
                        ValorIR = evt.ValorIR,
                        DataEvento = evt.DataCalculo
                    });

                    await _irPublisher.PublishVenda(evt);
                    resultado.EventosIrPublicados++;
                }
            }

            await _db.SaveChangesAsync(ct);
            resultado.Mensagens.Add("Rebalanceamento por mudanca de cesta executado.");
            return resultado;
        }

        public async Task<RebalanceResultado> RebalancearPorDesvioAsync(decimal toleranciaPercentual, DateTime dataReferencia, CancellationToken ct = default)
        {
            var resultado = new RebalanceResultado();

            var cestaAtiva = await _db.Cestas
                .Include(c => c.Itens)
                .FirstOrDefaultAsync(c => c.Ativa, ct);

            if (cestaAtiva == null)
            {
                resultado.Mensagens.Add("Nao existe cesta ativa.");
                return resultado;
            }

            var clientes = await _db.Clientes
                .Include(c => c.Custodia)
                .ThenInclude(c => c.Itens)
                .Where(c => c.Ativo)
                .ToListAsync(ct);

            resultado.ClientesProcessados = clientes.Count;

            foreach (var cliente in clientes)
            {
                if (cliente.Custodia == null || !cliente.Custodia.Itens.Any())
                    continue;

                var valorTotalCarteira = cliente.Custodia.Itens.Sum(i => i.Quantidade * i.PrecoMedio);
                if (valorTotalCarteira <= 0)
                    continue;

                decimal caixaGerado = 0m;
                decimal totalVendasMes = 0m;
                var lucros = new List<decimal>();

                foreach (var itemCarteira in cliente.Custodia.Itens.ToList())
                {
                    var tickerBase = RemoverSufixoFracionario(itemCarteira.Ticker);
                    var alvo = cestaAtiva.Itens.FirstOrDefault(i => i.Ticker.Equals(tickerBase, StringComparison.OrdinalIgnoreCase));
                    decimal percentualAlvo = alvo?.Percentual ?? 0m;

                    var percentualAtual = ((itemCarteira.Quantidade * itemCarteira.PrecoMedio) / valorTotalCarteira) * 100m;
                    var desvio = percentualAtual - percentualAlvo;

                    if (desvio <= toleranciaPercentual)
                        continue;

                    var valorExcesso = valorTotalCarteira * ((desvio - toleranciaPercentual) / 100m);
                    if (valorExcesso <= 0)
                        continue;

                    var quantidadeVenda = Math.Min(itemCarteira.Quantidade, Math.Floor(valorExcesso / itemCarteira.PrecoMedio));
                    if (quantidadeVenda <= 0)
                        continue;

                    var cotacaoVenda = _parser.ObterCotacaoFechamento(_pastaCotacoes, RemoverSufixoFracionario(itemCarteira.Ticker));
                    var precoVendaDesvio = cotacaoVenda?.PrecoFechamento ?? itemCarteira.PrecoMedio;

                    itemCarteira.Quantidade -= quantidadeVenda;
                    var valorVenda = quantidadeVenda * precoVendaDesvio;
                    var lucroDesvio = quantidadeVenda * (precoVendaDesvio - itemCarteira.PrecoMedio);

                    caixaGerado += valorVenda;
                    totalVendasMes += valorVenda;
                    lucros.Add(lucroDesvio);
                    resultado.OrdensVendaGeradas++;
                    resultado.ValorTotalVendido += valorVenda;
                }

                cliente.Custodia.Itens = cliente.Custodia.Itens.Where(i => i.Quantidade > 0).ToList();

                if (caixaGerado > 0)
                {
                    var itensCompra = CriarComprasPorPercentual(cestaAtiva, caixaGerado);
                    if (itensCompra.Any())
                    {
                        var ordem = OrdemCompra.Criar(cliente.Id, itensCompra);
                        ordem.MarcarExecutada();
                        _db.OrdensCompra.Add(ordem);

                        AtualizarCustodiaCompra(cliente, itensCompra);
                        resultado.OrdensCompraGeradas += itensCompra.Count;
                        resultado.ValorTotalComprado += itensCompra.Sum(i => i.ValorItem);
                    }
                }

                var irVenda = CalculosFinanceiros.CalcularIrVendaMonthly(totalVendasMes, lucros);
                if (totalVendasMes > 20000m)
                {
                    var evt = new IrVendaEvent
                    {
                        ClienteId = cliente.Id,
                        CPF = cliente.CPF,
                        MesReferencia = dataReferencia.ToString("yyyy-MM"),
                        TotalVendasMes = totalVendasMes,
                        LucroLiquido = lucros.Sum(),
                        ValorIR = irVenda,
                        DataCalculo = DateTime.UtcNow
                    };

                    _db.IrRegistros.Add(new IrRegistro
                    {
                        ClienteId = cliente.Id,
                        Tipo = "VENDA",
                        Ticker = null,
                        MesReferencia = evt.MesReferencia,
                        ValorOperacao = totalVendasMes,
                        LucroLiquido = evt.LucroLiquido,
                        Aliquota = evt.Aliquota,
                        ValorIR = evt.ValorIR,
                        DataEvento = evt.DataCalculo
                    });

                    await _irPublisher.PublishVenda(evt);
                    resultado.EventosIrPublicados++;
                }
            }

            await _db.SaveChangesAsync(ct);
            resultado.Mensagens.Add("Rebalanceamento por desvio executado.");
            return resultado;
        }

        private List<OrdemCompraItem> CriarComprasPorPercentual(CestaTopFive cesta, decimal valorBase)
        {
            var itens = new List<OrdemCompraItem>();

            foreach (var item in cesta.Itens)
            {
                var cotacao = _parser.ObterCotacaoFechamento(_pastaCotacoes, item.Ticker);
                if (cotacao == null || cotacao.PrecoFechamento <= 0)
                    continue;

                var valorDestino = valorBase * (item.Percentual / 100m);
                var quantidade = Math.Floor(valorDestino / cotacao.PrecoFechamento);
                if (quantidade <= 0)
                    continue;

                var lotes = Math.Floor(quantidade / 100m) * 100m;
                var fracionario = quantidade - lotes;

                if (lotes > 0)
                    itens.Add(OrdemCompraItem.Criar(item.Ticker, lotes, cotacao.PrecoFechamento));
                if (fracionario > 0)
                    itens.Add(OrdemCompraItem.Criar(item.Ticker + "F", fracionario, cotacao.PrecoFechamento, fracionario: true));
            }

            return itens;
        }

        private void AtualizarCustodiaCompra(Cliente cliente, List<OrdemCompraItem> itensCompra)
        {
            if (cliente.Custodia == null)
                cliente.Custodia = new Custodia();

            foreach (var item in itensCompra)
            {
                var existente = cliente.Custodia.Itens.FirstOrDefault(i => i.Ticker.Equals(item.Ticker, StringComparison.OrdinalIgnoreCase));

                if (existente == null)
                {
                    cliente.Custodia.Itens.Add(new CustodiaItem
                    {
                        Ticker = item.Ticker,
                        Quantidade = item.Quantidade,
                        PrecoMedio = item.PrecoUnitario
                    });
                    continue;
                }

                var novoPrecoMedio = CalculosFinanceiros.CalcularPrecoMedio(
                    existente.Quantidade,
                    existente.PrecoMedio,
                    item.Quantidade,
                    item.PrecoUnitario);

                existente.Quantidade += item.Quantidade;
                existente.PrecoMedio = novoPrecoMedio;
            }
        }

        private string RemoverSufixoFracionario(string ticker)
        {
            return ticker.EndsWith("F", StringComparison.OrdinalIgnoreCase)
                ? ticker.Substring(0, ticker.Length - 1)
                : ticker;
        }
    }
}