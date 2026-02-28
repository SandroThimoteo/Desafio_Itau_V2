using System;
using System.Collections.Generic;
using System.Linq;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Domain.Services;
using CompraProgramada.Infrastructure;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Application.Services
{
    public class MotorCompraService
    {
        private readonly CotahistParser _parser = new CotahistParser();
        private readonly ApplicationDbContext _db;
        private readonly IrPublisher _irPublisher;
        private readonly ILogger<MotorCompraService> _logger;

        public MotorCompraService(ApplicationDbContext db, IrPublisher irPublisher, ILogger<MotorCompraService> logger)
        {
            _db = db;
            _irPublisher = irPublisher;
            _logger = logger;
        }

        /// <summary>
        /// Executa a compra programada na data informada (dias 5/15/25 ou adiados para útil).
        /// Persistindo ordens de compra por cliente, atualizando custódias e publicando eventos de IR.
        /// </summary>
        public MotorResultado ExecutarCompra(DateTime dataReferencia, List<Cliente> clientesAtivos, CestaTopFive cesta, Dictionary<string, decimal> saldoMaster)
        {
            _logger.LogInformation(
                "Iniciando execução do motor de compra para {ClientesCount} clientes em {DataReferencia}",
                clientesAtivos.Count,
                dataReferencia.Date
            );

            // calculo de 1/3 do valor mensal para cada cliente
            decimal totalConsolidado = clientesAtivos.Sum(c => c.ValorMensal / 3m);
            var resultado = new MotorResultado
            {
                DataExecucao = DateTime.UtcNow,
                TotalClientes = clientesAtivos.Count,
                TotalConsolidado = totalConsolidado
            };

            _logger.LogInformation("Total consolidado para compra: R$ {TotalConsolidado}", totalConsolidado);

            // buscar cotações de cada ticker
            var cotacoes = new Dictionary<string, CotacaoB3>();
            foreach (var item in cesta.Itens)
            {
                var cot = _parser.ObterCotacaoFechamento("cotacoes", item.Ticker);
                if (cot == null)
                {
                    _logger.LogError("Cotação não encontrada para o ticker {Ticker}", item.Ticker);
                    throw new Exception($"COTACAO_NAO_ENCONTRADA: {item.Ticker}");
                }
                cotacoes[item.Ticker] = cot;
                _logger.LogDebug("Cotação carregada para {Ticker}: R$ {Preco}", item.Ticker, cot.PrecoFechamento);
            }

            var custodiaMaster = ObterOuCriarCustodiaMaster();
            var saldoMasterPersistido = custodiaMaster.Itens
                .ToDictionary(i => i.Ticker, i => i.Quantidade, StringComparer.OrdinalIgnoreCase);

            // calcular quantidade consolidada por ativo (considerando consumo de saldo master)
            var compraConsolidada = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); // quantidade efetivamente comprada no ciclo
            var totalParaDistribuir = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); // quantidade alvo para distribuir no ciclo
            var saldoConsumidoMaster = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase); // quanto do saldo master foi usado

            foreach (var item in cesta.Itens)
            {
                decimal valorAtivo = totalConsolidado * (item.Percentual / 100m);
                var cot = cotacoes[item.Ticker];
                decimal quantidadeAlvo = Math.Floor(valorAtivo / cot.PrecoFechamento);

                var saldoDisponivel = saldoMasterPersistido.GetValueOrDefault(item.Ticker, 0m);
                var quantidadeUsadaMaster = Math.Min(saldoDisponivel, quantidadeAlvo);
                var quantidadeComprar = quantidadeAlvo - quantidadeUsadaMaster;

                totalParaDistribuir[item.Ticker] = quantidadeAlvo;
                compraConsolidada[item.Ticker] = quantidadeComprar;
                saldoConsumidoMaster[item.Ticker] = quantidadeUsadaMaster;
            }

            // mapa para acumular quantidades distribuídas por ticker
            var distribuidoPorTicker = totalParaDistribuir.ToDictionary(k => k.Key, k => 0m, StringComparer.OrdinalIgnoreCase);

            // distribui para cada cliente proporcionalmente ao aporte 1/3
            foreach (var cliente in clientesAtivos)
            {
                decimal aporte = cliente.ValorMensal / 3m;
                resultado.ClientesDistribuidos.Add(cliente.Id);

                decimal proporcao = aporte / totalConsolidado;
                var ordemItens = new List<OrdemCompraItem>();

                foreach (var kv in totalParaDistribuir)
                {
                    var ticker = kv.Key;
                    var qtdTotal = kv.Value;
                    var quantidadeParaCliente = Math.Floor(qtdTotal * proporcao);
                    if (quantidadeParaCliente <= 0) continue;

                    distribuidoPorTicker[ticker] += quantidadeParaCliente;

                    var cot = cotacoes[ticker];
                    decimal lotes = Math.Floor(quantidadeParaCliente / 100m) * 100m;
                    decimal fracionario = quantidadeParaCliente - lotes;

                    if (lotes > 0)
                        ordemItens.Add(OrdemCompraItem.Criar(ticker, lotes, cot.PrecoFechamento, fracionario: false));
                    if (fracionario > 0)
                        ordemItens.Add(OrdemCompraItem.Criar(ticker + "F", fracionario, cot.PrecoFechamento, fracionario: true));
                }

                if (ordemItens.Any())
                {
                    var ordem = OrdemCompra.Criar(cliente.Id, ordemItens);
                    ordem.MarcarExecutada(); // já executada pela lógica do motor
                    _db.OrdensCompra.Add(ordem);

                    var distribuicao = new Distribuicao
                    {
                        ClienteId = cliente.Id,
                        Data = DateTime.UtcNow,
                        Itens = ordemItens.Select(i => new DistribuicaoItem
                        {
                            Ticker = i.Ticker,
                            Quantidade = i.Quantidade
                        }).ToList()
                    };
                    _db.Distribuicoes.Add(distribuicao);

                    AtualizarCustodia(cliente, ordemItens);

                    foreach (var item in ordemItens)
                    {
                        var valorOperacao = item.ValorItem;
                        var irValor = CalculosFinanceiros.CalcularIrDedoDuro(valorOperacao);
                        var evt = new IrDedoDuroEvent
                        {
                            ClienteId = cliente.Id,
                            CPF = cliente.CPF,
                            Ticker = item.Ticker,
                            Quantidade = item.Quantidade,
                            PrecoUnitario = item.PrecoUnitario,
                            ValorOperacao = valorOperacao,
                            ValorIR = irValor,
                            DataOperacao = DateTime.UtcNow
                        };

                        _db.IrRegistros.Add(new IrRegistro
                        {
                            ClienteId = cliente.Id,
                            Tipo = "DEDO_DURO",
                            Ticker = item.Ticker,
                            ValorOperacao = valorOperacao,
                            LucroLiquido = 0m,
                            Aliquota = evt.Aliquota,
                            ValorIR = irValor,
                            DataEvento = evt.DataOperacao
                        });

                        _irPublisher.PublishDedoDuro(evt).GetAwaiter().GetResult();
                    }
                }
            }

            // calcular resíduos e atualizar saldo master (persistido no banco e no output)
            foreach (var kv in totalParaDistribuir)
            {
                var ticker = kv.Key;
                var quantidadeAlvoDistribuicao = kv.Value;
                var distribuido = distribuidoPorTicker.GetValueOrDefault(ticker);
                var residuoDoCiclo = quantidadeAlvoDistribuicao - distribuido;

                var saldoInicial = saldoMasterPersistido.GetValueOrDefault(ticker, 0m);
                var saldoUsado = saldoConsumidoMaster.GetValueOrDefault(ticker, 0m);
                var novoSaldoMaster = saldoInicial - saldoUsado + residuoDoCiclo;

                AtualizarCustodiaMaster(custodiaMaster, ticker, novoSaldoMaster, cotacoes[ticker].PrecoFechamento);
                saldoMaster[ticker] = novoSaldoMaster;
            }

            _db.SaveChanges();

            // construir ordens consolidadas na resposta (itens efetivamente comprados no ciclo)
            resultado.Ordens = compraConsolidada
                .Where(kv => kv.Value > 0)
                .Select(kv => OrdemCompraItem.Criar(kv.Key, kv.Value, cotacoes[kv.Key].PrecoFechamento))
                .ToList();

            return resultado;
        }

        private Custodia ObterOuCriarCustodiaMaster()
        {
            var contaMaster = _db.ContasGraficas.FirstOrDefault(c => c.Tipo == ContaTipo.Master);
            if (contaMaster == null)
            {
                contaMaster = new ContaGrafica
                {
                    NumeroConta = "MASTER-001",
                    Tipo = ContaTipo.Master,
                    DataCriacao = DateTime.UtcNow
                };
                _db.ContasGraficas.Add(contaMaster);
                _db.SaveChanges();
            }

            var custodiaMaster = _db.Custodias
                .Include(c => c.Itens)
                .FirstOrDefault(c => c.ContaGraficaId == contaMaster.Id);

            if (custodiaMaster == null)
            {
                custodiaMaster = new Custodia
                {
                    ContaGraficaId = contaMaster.Id,
                    Itens = new List<CustodiaItem>()
                };

                _db.Custodias.Add(custodiaMaster);
                _db.SaveChanges();
            }

            return custodiaMaster;
        }

        private void AtualizarCustodiaMaster(Custodia custodiaMaster, string ticker, decimal quantidade, decimal precoReferencia)
        {
            var existente = custodiaMaster.Itens.FirstOrDefault(i => i.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase));

            if (quantidade <= 0)
            {
                if (existente != null)
                {
                    custodiaMaster.Itens.Remove(existente);
                }

                return;
            }

            if (existente == null)
            {
                custodiaMaster.Itens.Add(new CustodiaItem
                {
                    Ticker = ticker,
                    Quantidade = quantidade,
                    PrecoMedio = precoReferencia
                });

                return;
            }

            existente.Quantidade = quantidade;
            if (existente.PrecoMedio <= 0)
            {
                existente.PrecoMedio = precoReferencia;
            }
        }

        private void AtualizarCustodia(Cliente cliente, List<OrdemCompraItem> itens)
        {
            if (cliente.Custodia == null)
                cliente.Custodia = new Custodia();

            foreach (var item in itens)
            {
                // remover sufixo 'F' ao atualizar preço médio? manter como ticker distinto
                var ticker = item.Ticker;
                var existente = cliente.Custodia.Itens.FirstOrDefault(i => i.Ticker == ticker);
                if (existente == null)
                {
                    cliente.Custodia.Itens.Add(new CustodiaItem
                    {
                        Ticker = ticker,
                        Quantidade = item.Quantidade,
                        PrecoMedio = item.PrecoUnitario
                    });
                }
                else
                {
                    var novoQtd = existente.Quantidade + item.Quantidade;
                    existente.PrecoMedio = CalculosFinanceiros.CalcularPrecoMedio(
                        existente.Quantidade, existente.PrecoMedio,
                        item.Quantidade, item.PrecoUnitario);
                    existente.Quantidade = novoQtd;
                }
            }
        }
    }

    public class MotorResultado
    {
        public DateTime DataExecucao { get; set; }
        public int TotalClientes { get; set; }
        public decimal TotalConsolidado { get; set; }
        public List<OrdemCompraItem> Ordens { get; set; } = new List<OrdemCompraItem>();
        public List<long> ClientesDistribuidos { get; set; } = new List<long>();
    }
}
