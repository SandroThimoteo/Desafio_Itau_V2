using System;
using System.Threading;
using System.Threading.Tasks;
using CompraProgramada.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace CompraProgramada.Api.Controllers
{
    [ApiController]
    [Route("api/rebalance")]
    public class RebalanceController : ControllerBase
    {
        private readonly IRebalanceService _service;

        public RebalanceController(IRebalanceService service)
        {
            _service = service;
        }

        [HttpPost("mudanca-cesta/{cestaId:long}")]
        public async Task<ActionResult> RebalancearMudancaCesta(long cestaId, [FromQuery] DateTime? dataReferencia, CancellationToken ct)
        {
            var data = dataReferencia ?? DateTime.UtcNow;
            var resultado = await _service.RebalancearPorMudancaDeCestaAsync(cestaId, data, ct);
            return Ok(resultado);
        }

        [HttpPost("desvio")]
        public async Task<ActionResult> RebalancearDesvio([FromQuery] decimal toleranciaPercentual = 2.5m, [FromQuery] DateTime? dataReferencia = null, CancellationToken ct = default)
        {
            var data = dataReferencia ?? DateTime.UtcNow;
            var resultado = await _service.RebalancearPorDesvioAsync(toleranciaPercentual, data, ct);
            return Ok(resultado);
        }
    }
}
