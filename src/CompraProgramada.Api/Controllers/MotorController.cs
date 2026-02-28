using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using CompraProgramada.Application.Services;
using CompraProgramada.Api.Services;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Api.Controllers
{
    [ApiController]
    [Route("api/motor")]
    public class MotorController : ControllerBase
    {
        private readonly MotorCompraService _motor;
        private readonly MotorAgendamentoStatusStore _statusStore;
        private readonly ApplicationDbContext _db;

        public MotorController(MotorCompraService motor, MotorAgendamentoStatusStore statusStore, ApplicationDbContext db)
        {
            _motor = motor;
            _statusStore = statusStore;
            _db = db;
        }

        [HttpPost("executar-compra")]
        public ActionResult ExecutarCompra([FromBody] MotorRequest request)
        {
            var clientesAtivos = _db.Clientes.Where(c => c.Ativo).ToList();
            if (!clientesAtivos.Any())
                return BadRequest(new { erro = "Nenhum cliente ativo encontrado." });

            var cesta = _db.Cestas.Include(c => c.Itens).FirstOrDefault(c => c.Ativa);
            if (cesta == null)
                return BadRequest(new { erro = "Nenhuma cesta ativa configurada." });

            // Saldo master zerado para execução manual simplificada
            var saldoMaster = new Dictionary<string, decimal>();

            try
            {
                var resultado = _motor.ExecutarCompra(request.DataReferencia, clientesAtivos, cesta, saldoMaster);
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
}
