using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ApplicationDbContext _db;

        public CestaController(CestaService service, ApplicationDbContext db)
        {
            _service = service;
            _db = db;
        }

        [HttpPost]
        public ActionResult Cadastrar([FromBody] CestaRequest request)
        {
            try
            {
                var cestaAnterior = _db.Cestas.Include(c => c.Itens).FirstOrDefault(c => c.Ativa);
                var cesta = _service.CriarOuAtualizarCesta(request.Nome, request.Itens, cestaAnterior);

                if (cestaAnterior != null)
                    _db.Cestas.Update(cestaAnterior);

                _db.Cestas.Add(cesta);
                _db.SaveChanges();

                return Created($"/api/admin/cesta/atual", new { mensagem = "Cesta cadastrada/alterada", id = cesta.Id });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { erro = ex.Message });
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
            var custodias = _db.Custodias.Include(c => c.Itens).ToList();
            return Ok(custodias);
        }
    }
}
