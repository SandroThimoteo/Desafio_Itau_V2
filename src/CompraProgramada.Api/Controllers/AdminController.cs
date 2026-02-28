using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
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
        public async Task<ActionResult> Cadastrar([FromBody] CestaRequest request, CancellationToken ct)
        {
            try
            {
                var cestaAnterior = _db.Cestas.Include(c => c.Itens).FirstOrDefault(c => c.Ativa);
                var cesta = _service.CriarOuAtualizarCesta(request.Nome, request.Itens, cestaAnterior);

                if (cestaAnterior != null)
                    _db.Cestas.Update(cestaAnterior);

                _db.Cestas.Add(cesta);
                _db.SaveChanges();

                RebalanceResultado? rebalance = null;
                if (cestaAnterior != null)
                {
                    rebalance = await _rebalanceService.RebalancearPorMudancaDeCestaAsync(cesta.Id, DateTime.UtcNow, ct);
                }

                return Created($"/api/admin/cesta/atual", new
                {
                    mensagem = "Cesta cadastrada/alterada",
                    id = cesta.Id,
                    rebalanceamentoExecutado = cestaAnterior != null,
                    resumoRebalanceamento = rebalance
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { erro = "Falha ao processar rebalanceamento apos alterar a cesta.", detalhe = ex.Message });
            }
        }

        [HttpGet("atual")]
        public ActionResult ObterAtual()
        {
            var cesta = _db.Cestas.Include(c => c.Itens).FirstOrDefault(c => c.Ativa);
            if (cesta == null) return NotFound(new { erro = "Nenhuma cesta ativa encontrada" });
            return Ok(cesta);
        }

        [HttpGet("historico")]
        public ActionResult Historico()
        {
            var cestas = _db.Cestas.Include(c => c.Itens).OrderByDescending(c => c.DataCriacao).ToList();
            return Ok(cestas);
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
        public ActionResult ConsultarCustodia()
        {
            var contaMaster = _db.ContasGraficas.FirstOrDefault(c => c.Tipo == ContaTipo.Master);
            if (contaMaster == null)
                return NotFound(new { erro = "Conta master nao encontrada" });

            var custodiaMaster = _db.Custodias
                .Include(c => c.Itens)
                .FirstOrDefault(c => c.ContaGraficaId == contaMaster.Id);

            if (custodiaMaster == null)
                return Ok(new { contaMasterId = contaMaster.Id, numeroConta = contaMaster.NumeroConta, itens = new List<object>() });

            return Ok(new
            {
                contaMasterId = contaMaster.Id,
                numeroConta = contaMaster.NumeroConta,
                itens = custodiaMaster.Itens.Select(i => new { i.Ticker, i.Quantidade, i.PrecoMedio })
            });
        }
    }
}
