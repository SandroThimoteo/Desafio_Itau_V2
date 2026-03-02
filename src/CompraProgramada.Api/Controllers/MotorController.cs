using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CompraProgramada.Application.Services;
using CompraProgramada.Api.Services;
using CompraProgramada.Api.DTOs;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using CompraProgramada.Domain.Entities;

namespace CompraProgramada.Api.Controllers
{
    [ApiController]
    [Route("api/motor")]
    public class MotorController : ControllerBase
    {
        private readonly MotorCompraService _motor;
        private readonly IRebalanceService _rebalanceService;
        private readonly MotorAgendamentoStatusStore _statusStore;
        private readonly ApplicationDbContext _db;

        public MotorController(MotorCompraService motor, IRebalanceService rebalanceService, MotorAgendamentoStatusStore statusStore, ApplicationDbContext db)
        {
            _motor = motor;
            _rebalanceService = rebalanceService;
            _statusStore = statusStore;
            _db = db;
        }

        [HttpPost("executar-compra")]
        public ActionResult<MotorExecutionResponse> ExecutarCompra([FromBody] MotorRequest request)
        {
            var clientesAtivos = _db.Clientes
    .Where(c => c.Ativo)
    .Include(c => c.Custodia)
        .ThenInclude(cu => cu.Itens)
    .Include(c => c.ContaGrafica)
    .ToList();
            if (!clientesAtivos.Any())
                return BadRequest(new { erro = "Nenhum cliente ativo encontrado." });

            var cesta = _db.Cestas.Include(c => c.Itens).FirstOrDefault(c => c.Ativa);
            if (cesta == null)
                return BadRequest(new { erro = "Nenhuma cesta ativa configurada." });

            // Carregar saldo master real do banco (resíduos de execuções anteriores)
            var contaMaster = _db.ContasGraficas.FirstOrDefault(c => c.Tipo == ContaTipo.Master);
            var saldoMaster = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            if (contaMaster != null)
            {
                var custodiaMaster = _db.Custodias
                    .Include(c => c.Itens)
                    .FirstOrDefault(c => c.ContaGraficaId == contaMaster.Id);
                if (custodiaMaster != null)
                    foreach (var item in custodiaMaster.Itens)
                        saldoMaster[item.Ticker] = item.Quantidade;
            }

            try
            {
                var resultado = _motor.ExecutarCompra(request.DataReferencia, clientesAtivos, cesta, saldoMaster);

                // Mapear para o DTO esperado
                var response = new MotorExecutionResponse
                {
                    DataExecucao = resultado.DataExecucao,
                    TotalClientes = resultado.TotalClientes,
                    TotalConsolidado = Math.Round(resultado.TotalConsolidado, 2),
                    EventosIRPublicados = resultado.TotalEventosIR,
                    Mensagem = $"Compra programada executada com sucesso para {resultado.TotalClientes} clientes."
                };

                // Agrupar ordens por ticker
                var ordensAgrupadas = resultado.Ordens
                    .GroupBy(o => o.Ticker)
                    .Select(g => new OrdemCompraDTO
                    {
                        Ticker = g.Key,
                        QuantidadeTotal = g.Sum(o => o.Quantidade),
                        Detalhes = g.Select(o => new DetalheOrdemDTO
                        {
                            Tipo = "FRACIONARIO",
                            Ticker = o.Ticker + "F",
                            Quantidade = o.Quantidade
                        }).ToList(),
                        PrecoUnitario = g.First().PrecoUnitario,
                        ValorTotal = Math.Round(g.Sum(o => o.Quantidade * o.PrecoUnitario), 2)
                    }).ToList();

                response.OrdensCompra = ordensAgrupadas;

                // Distribuições por cliente
                var distribuicoes = new List<DistribuicaoDTO>();
                foreach (var clienteId in resultado.ClientesDistribuidos)
                {
                    var cliente = clientesAtivos.FirstOrDefault(c => c.Id == clienteId);
                    if (cliente != null)
                    {
                        var aporte = cliente.ValorMensal / 3m;
                        var proporcao = aporte / resultado.TotalConsolidado;

                        var ativosCliente = new List<AtivoDistribuicaoDTO>();
                        foreach (var ordem in resultado.Ordens)
                        {
                            var quantidadeParaCliente = Math.Floor(ordem.Quantidade * proporcao);
                            if (quantidadeParaCliente > 0)
                            {
                                ativosCliente.Add(new AtivoDistribuicaoDTO
                                {
                                    Ticker = ordem.Ticker,
                                    Quantidade = quantidadeParaCliente
                                });
                            }
                        }

                        distribuicoes.Add(new DistribuicaoDTO
                        {
                            ClienteId = clienteId,
                            Nome = cliente.Nome,
                            ValorAporte = Math.Round(aporte, 2),
                            Ativos = ativosCliente
                        });
                    }
                }

                response.Distribuicoes = distribuicoes;

                // Resíduos reais calculados pelo motor após distribuição
                response.ResiduosCustMaster = saldoMaster
                    .Where(kv => kv.Value > 0)
                    .Select(kv => new ResiduoDTO { Ticker = kv.Key, Quantidade = kv.Value })
                    .ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpPost("rebalancear-desvio")]
        public async Task<ActionResult> RebalancearPorDesvio([FromBody] RebalanceRequest request)
        {
            try
            {
                // Tolerância padrão: 5% se não informada
                decimal tolerancia = request.ToleranciaPercentual ?? 5m;
                if (tolerancia < 0 || tolerancia > 100)
                    return BadRequest(new { erro = "Tolerância deve estar entre 0 e 100%." });

                var resultado = await _rebalanceService.RebalancearPorDesvioAsync(tolerancia, DateTime.UtcNow);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = ex.Message });
            }
        }

        [HttpGet("agendamento/status")]
        public ActionResult ObterStatusAgendamento()
        {
            var snapshot = _statusStore.ObterSnapshot();

            var pendentes = CalendarioCompraProgramada.ObterCiclosPendentes(
                DateTime.UtcNow,
                _statusStore.ObterCiclosExecutados())
                .Select(c => new { c.Chave, c.DiaBase, c.DataReferenciaUtc })
                .ToList();

            return Ok(new
            {
                status = snapshot,
                ciclosPendentes = pendentes
            });
        }
    }

    public class MotorRequest
    {
        public DateTime DataReferencia { get; set; }
    }

    public class RebalanceRequest
    {
        public decimal? ToleranciaPercentual { get; set; }
    }
}