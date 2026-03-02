using System;
using System.Linq;
using CompraProgramada.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Api.Controllers
{
    [ApiController]
    [Route("api/operacoes")]
    public class OperacoesController : ControllerBase
    {
        private readonly ApplicationDbContext _db;

        public OperacoesController(ApplicationDbContext db)
        {
            _db = db;
        }

        [HttpGet("ordens")]
        public ActionResult ConsultarOrdens([FromQuery] long? clienteId, [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim)
        {
            var query = _db.OrdensCompra
                .Include(o => o.Itens)
                .AsQueryable();

            if (clienteId.HasValue)
                query = query.Where(o => o.ClienteId == clienteId.Value);
            if (inicio.HasValue)
                query = query.Where(o => o.DataCriacao >= inicio.Value);
            if (fim.HasValue)
                query = query.Where(o => o.DataCriacao <= fim.Value);

            var ordens = query
                .OrderByDescending(o => o.DataCriacao)
                .Select(o => new
                {
                    o.Id,
                    o.ClienteId,
                    DataCriacao = o.DataCriacao,
                    o.DataConclusao,
                    Status = o.Status.ToString(),
                    Executada = o.Status.ToString() == "Concluida" || o.Status.ToString() == "Executada",
                    o.ValorTotal,
                    Itens = o.Itens.Select(i => new { i.Ticker, i.Quantidade, i.PrecoUnitario, i.ValorItem, i.Fracionario })
                })
                .ToList();

            return Ok(ordens);
        }

        [HttpGet("distribuicoes")]
        public ActionResult ConsultarDistribuicoes([FromQuery] long? clienteId, [FromQuery] DateTime? inicio, [FromQuery] DateTime? fim)
        {
            var query = _db.Distribuicoes
                .Include(d => d.Itens)
                .AsQueryable();

            if (clienteId.HasValue)
                query = query.Where(d => d.ClienteId == clienteId.Value);
            if (inicio.HasValue)
                query = query.Where(d => d.Data >= inicio.Value);
            if (fim.HasValue)
                query = query.Where(d => d.Data <= fim.Value);

            var distribuicoes = query
                .OrderByDescending(d => d.Data)
                .Select(d => new
                {
                    d.Id,
                    d.ClienteId,
                    d.Data,
                    d.ValorAporte,
                    Itens = d.Itens.Select(i => new { i.Ticker, i.Quantidade })
                })
                .ToList();

            return Ok(distribuicoes);
        }

        [HttpGet("ir")]
        public ActionResult ConsultarIr([FromQuery] long? clienteId, [FromQuery] string? tipo, [FromQuery] string? mesReferencia)
        {
            var query = _db.IrRegistros.AsQueryable();

            if (clienteId.HasValue)
                query = query.Where(i => i.ClienteId == clienteId.Value);
            if (!string.IsNullOrWhiteSpace(tipo))
                query = query.Where(i => i.Tipo.ToUpper() == tipo.ToUpper());
            if (!string.IsNullOrWhiteSpace(mesReferencia))
                query = query.Where(i => i.MesReferencia == mesReferencia);

            var historico = query
                .OrderByDescending(i => i.DataEvento)
                .Select(i => new
                {
                    i.Id,
                    i.ClienteId,
                    i.Tipo,
                    i.Ticker,
                    i.MesReferencia,
                    i.ValorOperacao,
                    i.LucroLiquido,
                    i.Aliquota,
                    i.ValorIR,
                    i.DataEvento
                })
                .ToList();

            return Ok(historico);
        }
    }
}