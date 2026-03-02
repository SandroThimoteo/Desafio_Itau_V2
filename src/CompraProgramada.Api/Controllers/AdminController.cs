using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Api.Controllers
{
    [ApiController]
    [Route("api/admin/[controller]")]
    public class CestaController : ControllerBase
    {
        private readonly CestaService _service;
        private readonly IRebalanceService _rebalanceService;
        private readonly ApplicationDbContext _db;

        public CestaController(CestaService service, IRebalanceService rebalanceService, ApplicationDbContext db)
        {
            _service = service;
            _rebalanceService = rebalanceService;
            _db = db;
        }

        [HttpPost]
        public async Task<ActionResult<CestaCreateResponse>> Cadastrar([FromBody] CestaRequest request, CancellationToken ct)
        {
            try
            {
                var cestaAnterior = _db.Cestas.Include(c => c.Itens).FirstOrDefault(c => c.Ativa);
                var cesta = _service.CriarOuAtualizarCesta(request.Nome, request.Itens, cestaAnterior);

                if (cestaAnterior != null)
                {
                    cestaAnterior.Ativa = false;
                    cestaAnterior.DataDesativacao = DateTime.UtcNow;
                    _db.Cestas.Update(cestaAnterior);
                }

                _db.Cestas.Add(cesta);
                _db.SaveChanges();

                var ativosRemovidos = cestaAnterior?.Itens.Select(i => i.Ticker).ToList() ?? new List<string>();
                var ativosAdicionados = cesta.Itens.Select(i => i.Ticker).ToList() ?? new List<string>();

                var clientesAtivos = _db.Clientes.Count(c => c.Ativo);
                var rebalanceamentoDisparado = cestaAnterior != null;

                var response = new CestaCreateResponse
                {
                    CestaId = cesta.Id,
                    Nome = cesta.Nome,
                    Ativa = cesta.Ativa,
                    DataCriacao = cesta.DataCriacao,
                    Itens = cesta.Itens.Select(i => new CestaItemDTO
                    {
                        Ticker = i.Ticker,
                        Percentual = Math.Round(i.Percentual, 2)
                    }).ToList(),
                    RebalanceamentoDisparado = rebalanceamentoDisparado,
                    Mensagem = cestaAnterior == null
                        ? "Primeira cesta cadastrada com sucesso."
                        : $"Cesta atualizada. Rebalanceamento disparado para {clientesAtivos} clientes ativos."
                };

                if (cestaAnterior != null)
                {
                    response.CestaAnteriorDesativada = new CestaAnteriorDTO
                    {
                        CestaId = cestaAnterior.Id,
                        Nome = cestaAnterior.Nome,
                        DataDesativacao = cestaAnterior.DataDesativacao ?? DateTime.UtcNow
                    };

                    response.AtivosRemovidos = ativosRemovidos.Except(ativosAdicionados).ToList();
                    response.AtivosAdicionados = ativosAdicionados.Except(ativosRemovidos).ToList();

                    // RN-019: acionar rebalanceamento para todos os clientes ativos
                    _ = _rebalanceService.RebalancearPorMudancaDeCestaAsync(cesta.Id, DateTime.UtcNow, ct);
                }

                return Created($"/api/admin/cesta/atual", response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { erro = ex.Message, codigo = ex.ParamName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = "Falha ao processar cesta.", detalhe = ex.Message });
            }
        }

        [HttpGet("atual")]
        public ActionResult<CestaAtualResponse> ObterAtual()
        {
            var cesta = _db.Cestas.Include(c => c.Itens).FirstOrDefault(c => c.Ativa);
            if (cesta == null) return NotFound(new { erro = "Nenhuma cesta ativa encontrada", codigo = "CESTA_NAO_ENCONTRADA" });

            var parser = new CompraProgramada.Infrastructure.CotahistParser();

            var response = new CestaAtualResponse
            {
                CestaId = cesta.Id,
                Nome = cesta.Nome,
                Ativa = cesta.Ativa,
                DataCriacao = cesta.DataCriacao,
                Itens = cesta.Itens.Select(i =>
                {
                    var cotacao = parser.ObterCotacaoFechamento("cotacoes", i.Ticker);
                    return new CestaItemDTO
                    {
                        Ticker = i.Ticker,
                        Percentual = Math.Round(i.Percentual, 2),
                        CotacaoAtual = cotacao?.PrecoFechamento ?? 0m
                    };
                }).ToList()
            };

            return Ok(response);
        }

        [HttpGet("historico")]
        public ActionResult<List<CestaHistoricoResponse>> Historico()
        {
            var cestas = _db.Cestas
                .Include(c => c.Itens)
                .OrderByDescending(c => c.DataCriacao)
                .ToList();

            var response = cestas.Select(c => new CestaHistoricoResponse
            {
                CestaId = c.Id,
                Nome = c.Nome,
                Ativa = c.Ativa,
                DataCriacao = c.DataCriacao,
                DataDesativacao = c.DataDesativacao,
                Itens = c.Itens.Select(i => new CestaItemDTO
                {
                    Ticker = i.Ticker,
                    Percentual = Math.Round(i.Percentual, 2)
                }).ToList()
            }).ToList();

            return Ok(new { cestas = response });
        }
    }

    public class CestaRequest
    {
        public string Nome { get; set; } = string.Empty;
        public List<CestaItem> Itens { get; set; } = new();
    }

    [ApiController]
    [Route("api/admin/conta-master")]
    public class ContaMasterController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public ContaMasterController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("custodia")]
        public ActionResult<ContaMasterResponse> ConsultarCustodia()
        {
            var contaMaster = _db.ContasGraficas.FirstOrDefault(c => c.Tipo == ContaTipo.Master);
            if (contaMaster == null)
                return NotFound(new { erro = "Conta master não encontrada", codigo = "CONTA_MASTER_NAO_ENCONTRADA" });

            var custodiaMaster = _db.Custodias
                .Include(c => c.Itens)
                .FirstOrDefault(c => c.ContaGraficaId == contaMaster.Id);

            var parser = new CompraProgramada.Infrastructure.CotahistParser();
            decimal valorTotalResiduo = 0m;

            var custodiaItems = new List<CustodiaItemDTO>();
            if (custodiaMaster != null)
            {
                foreach (var item in custodiaMaster.Itens)
                {
                    var tickerBase = item.Ticker.EndsWith("F") ? item.Ticker[..^1] : item.Ticker;
                    var cotacao = parser.ObterCotacaoFechamento("cotacoes", tickerBase)
                                 ?? parser.ObterCotacaoFechamento("cotacoes", item.Ticker);

                    var precoAtual = cotacao?.PrecoFechamento ?? item.PrecoMedio;
                    var valorAtual = item.Quantidade * precoAtual;
                    valorTotalResiduo += valorAtual;

                    custodiaItems.Add(new CustodiaItemDTO
                    {
                        Ticker = item.Ticker,
                        Quantidade = item.Quantidade,
                        PrecoMedio = Math.Round(item.PrecoMedio, 2),
                        ValorAtual = Math.Round(valorAtual, 2),
                        Origem = "Residuo distribuicao"
                    });
                }
            }

            var response = new ContaMasterResponse
            {
                ContaMaster = new ContaMasterDTO
                {
                    Id = contaMaster.Id,
                    NumeroConta = contaMaster.NumeroConta,
                    Tipo = "MASTER"
                },
                Custodia = custodiaItems,
                ValorTotalResiduo = Math.Round(valorTotalResiduo, 2)
            };

            return Ok(response);
        }
    }
}
