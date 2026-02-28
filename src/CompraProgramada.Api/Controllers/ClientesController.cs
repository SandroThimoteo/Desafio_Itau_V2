using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly ClienteService _service;
        private readonly RentabilidadeService _rentabilidadeService;
        private readonly ApplicationDbContext _db;

        public ClientesController(ClienteService service, RentabilidadeService rentabilidadeService, ApplicationDbContext db)
        {
            _service = service;
            _rentabilidadeService = rentabilidadeService;
            _db = db;
        }

        [HttpPost("adesao")]
        public ActionResult<Cliente> Adesao([FromBody] AdesaoRequest request)
        {
            try
            {
                var cliente = _service.AdicionarCliente(request.Nome, request.CPF, request.Email, request.ValorMensal);
                _db.Clientes.Add(cliente);
                _db.SaveChanges();
                return CreatedAtAction(nameof(ConsultarCarteira), new { clienteId = cliente.Id }, cliente);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { erro = ex.Message, codigo = ex.ParamName });
            }
        }

        [HttpPost("{clienteId}/saida")]
        public ActionResult Saida(long clienteId)
        {
            var cliente = _db.Clientes.Find(clienteId);
            if (cliente == null) return NotFound(new { erro = "Cliente não encontrado" });

            try
            {
                _service.SairDoProduto(cliente);
                _db.SaveChanges();
                return Ok(new { clienteId, ativo = false, dataSaida = cliente.DataSaida, mensagem = "Adesao encerrada. Sua posicao em custodia foi mantida." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpPut("{clienteId}/valor-mensal")]
        public ActionResult AlterarValor(long clienteId, [FromBody] AlterarValorRequest request)
        {
            var cliente = _db.Clientes.Find(clienteId);
            if (cliente == null) return NotFound(new { erro = "Cliente não encontrado" });

            var anterior = cliente.ValorMensal;
            _service.AlterarValorMensal(cliente, request.NovoValorMensal);
            _db.SaveChanges();

            return Ok(new { clienteId, valorMensalAnterior = anterior, valorMensalNovo = request.NovoValorMensal, dataAlteracao = DateTime.UtcNow, mensagem = "Valor mensal atualizado. O novo valor sera considerado a partir da proxima data de compra." });
        }

        [HttpGet("{clienteId}/carteira")]
        public ActionResult ConsultarCarteira(long clienteId)
        {
            var cliente = _db.Clientes
                .Include(c => c.Custodia)
                .FirstOrDefault(c => c.Id == clienteId);

            if (cliente == null) return NotFound(new { erro = "Cliente não encontrado" });

            return Ok(new
            {
                clienteId = cliente.Id,
                nome = cliente.Nome,
                valorMensal = cliente.ValorMensal,
                ativo = cliente.Ativo,
                custodia = cliente.Custodia?.Itens
            });
        }

        [HttpGet("{clienteId}/rentabilidade")]
        public ActionResult ConsultarRentabilidade(long clienteId)
        {
            var resultado = _rentabilidadeService.Calcular(clienteId);
            if (resultado == null)
                return NotFound(new { erro = "Cliente não encontrado" });

            return Ok(resultado);
        }
    }

    public class AdesaoRequest
    {
        public string Nome { get; set; } = string.Empty;
        public string CPF { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal ValorMensal { get; set; }
    }

    public class AlterarValorRequest
    {
        public decimal NovoValorMensal { get; set; }
    }
}

