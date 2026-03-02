using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CompraProgramada.Application.Services;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Api.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CompraProgramada.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientesController : ControllerBase
    {
        private readonly ClienteService _service;
        private readonly RentabilidadeService _rentabilidadeService;
        private readonly ApplicationDbContext _db;
        private readonly string _pastaCotacoes;

        public ClientesController(ClienteService service, RentabilidadeService rentabilidadeService, ApplicationDbContext db, IConfiguration configuration)
        {
            _service = service;
            _rentabilidadeService = rentabilidadeService;
            _db = db;
            _pastaCotacoes = configuration["Cotacoes:PastaCotahist"] ?? "cotacoes";
        }

        [HttpPost("adesao")]
        public ActionResult<AdesaoResponse> Adesao([FromBody] AdesaoRequest request)
        {
            try
            {
                var cliente = _service.AdicionarCliente(request.Nome, request.CPF, request.Email, request.ValorMensal);
                _db.Clientes.Add(cliente);
                _db.SaveChanges();

                var response = new AdesaoResponse
                {
                    ClienteId = cliente.Id,
                    Nome = cliente.Nome,
                    CPF = cliente.CPF,
                    Email = cliente.Email,
                    ValorMensal = cliente.ValorMensal,
                    Ativo = cliente.Ativo,
                    DataAdesao = cliente.DataAdesao,
                    ContaGrafica = new ContaGraficaDTO
                    {
                        Id = cliente.ContaGrafica.Id,
                        NumeroConta = cliente.ContaGrafica.NumeroConta,
                        Tipo = cliente.ContaGrafica.Tipo.ToString().ToUpper(),
                        DataCriacao = cliente.ContaGrafica.DataCriacao
                    }
                };

                return CreatedAtAction(nameof(ConsultarCarteira), new { clienteId = cliente.Id }, response);
            }
            catch (ArgumentException ex) when (ex.ParamName == "CLIENTE_CPF_DUPLICADO")
            {
                return Conflict(new { erro = ex.Message, codigo = ex.ParamName });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { erro = ex.Message, codigo = ex.ParamName });
            }
        }

        [HttpPost("{clienteId}/saida")]
        public ActionResult<SaidaResponse> Saida(long clienteId)
        {
            var cliente = _db.Clientes.Find(clienteId);
            if (cliente == null) return NotFound(new { erro = "Cliente não encontrado.", codigo = "CLIENTE_NAO_ENCONTRADO" });

            try
            {
                _service.SairDoProduto(cliente);
                _db.SaveChanges();

                var response = new SaidaResponse
                {
                    ClienteId = clienteId,
                    Nome = cliente.Nome,
                    Ativo = false,
                    DataSaida = cliente.DataSaida ?? DateTime.UtcNow,
                    Mensagem = "Adesao encerrada. Sua posicao em custodia foi mantida."
                };

                return Ok(response);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { erro = ex.Message });
            }
        }

        [HttpPut("{clienteId}/valor-mensal")]
        public ActionResult<AlterarValorResponse> AlterarValor(long clienteId, [FromBody] AlterarValorRequest request)
        {
            var cliente = _db.Clientes.Find(clienteId);
            if (cliente == null) return NotFound(new { erro = "Cliente não encontrado", codigo = "CLIENTE_NAO_ENCONTRADO" });

            var anterior = cliente.ValorMensal;
            _service.AlterarValorMensal(cliente, request.NovoValorMensal);
            _db.SaveChanges();

            var response = new AlterarValorResponse
            {
                ClienteId = clienteId,
                ValorMensalAnterior = anterior,
                ValorMensalNovo = request.NovoValorMensal,
                DataAlteracao = DateTime.UtcNow,
                Mensagem = "Valor mensal atualizado. O novo valor sera considerado a partir da proxima data de compra."
            };

            return Ok(response);
        }

        [HttpGet("{clienteId}/carteira")]
        public ActionResult<CarteiraResponse> ConsultarCarteira(long clienteId)
        {
            var cliente = _db.Clientes
                .Include(c => c.Custodia)
                .ThenInclude(c => c.Itens)
                .FirstOrDefault(c => c.Id == clienteId);

            if (cliente == null) return NotFound(new { erro = "Cliente não encontrado", codigo = "CLIENTE_NAO_ENCONTRADO" });

            var parser = new CompraProgramada.Infrastructure.CotahistParser();
            var ativos = new List<AtivoCarteiraDTO>();

            decimal valorTotalInvestido = 0m;
            decimal valorAtualCarteira = 0m;

            foreach (var item in cliente.Custodia?.Itens ?? new List<CustodiaItem>())
            {
                var tickerBase = RemoverSufixoFracionario(item.Ticker);
                var cotacao = parser.ObterCotacaoFechamento(_pastaCotacoes, tickerBase)
                             ?? parser.ObterCotacaoFechamento(_pastaCotacoes, item.Ticker);

                var precoAtual = cotacao?.PrecoFechamento ?? item.PrecoMedio;
                var valorInvestido = item.Quantidade * item.PrecoMedio;
                var valorAtual = item.Quantidade * precoAtual;
                var pl = valorAtual - valorInvestido;
                var plPercentual = valorInvestido <= 0 ? 0m : (pl / valorInvestido) * 100m;

                valorTotalInvestido += valorInvestido;
                valorAtualCarteira += valorAtual;

                ativos.Add(new AtivoCarteiraDTO
                {
                    Ticker = item.Ticker,
                    Quantidade = item.Quantidade,
                    PrecoMedio = Math.Round(item.PrecoMedio, 2),
                    CotacaoAtual = Math.Round(precoAtual, 2),
                    ValorAtual = Math.Round(valorAtual, 2),
                    Pl = Math.Round(pl, 2),
                    PlPercentual = Math.Round(plPercentual, 2),
                    ComposicaoCarteira = valorAtualCarteira > 0 ? Math.Round((valorAtual / valorAtualCarteira) * 100m, 2) : 0m
                });
            }

            // Recalcular composição (soma dos valores atuais para percentual correto)
            foreach (var ativo in ativos)
            {
                ativo.ComposicaoCarteira = valorAtualCarteira > 0 ? Math.Round((ativo.ValorAtual / valorAtualCarteira) * 100m, 2) : 0m;
            }

            var plTotal = valorAtualCarteira - valorTotalInvestido;
            var rentabilidade = valorTotalInvestido > 0 ? (plTotal / valorTotalInvestido) * 100m : 0m;

            var response = new CarteiraResponse
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                ContaGrafica = cliente.ContaGrafica.NumeroConta,
                DataConsulta = DateTime.UtcNow,
                Resumo = new ResumoCarteiraDTO
                {
                    ValorTotalInvestido = Math.Round(valorTotalInvestido, 2),
                    ValorAtualCarteira = Math.Round(valorAtualCarteira, 2),
                    PlTotal = Math.Round(plTotal, 2),
                    RentabilidadePercentual = Math.Round(rentabilidade, 2)
                },
                Ativos = ativos
            };

            return Ok(response);
        }

        [HttpGet("{clienteId}/rentabilidade")]
        public ActionResult<RentabilidadeResponse> ConsultarRentabilidade(long clienteId)
        {
            var cliente = _db.Clientes
                .Include(c => c.Custodia)
                .ThenInclude(c => c.Itens)
                .FirstOrDefault(c => c.Id == clienteId);

            if (cliente == null)
                return NotFound(new { erro = "Cliente não encontrado", codigo = "CLIENTE_NAO_ENCONTRADO" });

            var parser = new CompraProgramada.Infrastructure.CotahistParser();

            // Montar dicionário de cotações atuais por ticker (evita buscar múltiplas vezes)
            var cotacoes = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in cliente.Custodia?.Itens ?? new List<CustodiaItem>())
            {
                var tickerBase = RemoverSufixoFracionario(item.Ticker);
                if (!cotacoes.ContainsKey(tickerBase))
                {
                    var cot = parser.ObterCotacaoFechamento(_pastaCotacoes, tickerBase);
                    // Para fracionário sem cotação própria, usar o ticker base
                    cotacoes[tickerBase] = cot?.PrecoFechamento ?? item.PrecoMedio;
                }
            }

            // Calcular valor total investido e valor atual com cotações reais
            decimal valorTotalInvestido = 0m;
            decimal valorAtualCarteira = 0m;

            foreach (var item in cliente.Custodia?.Itens ?? new List<CustodiaItem>())
            {
                var tickerBase = RemoverSufixoFracionario(item.Ticker);
                var precoAtual = cotacoes.GetValueOrDefault(tickerBase, item.PrecoMedio);

                valorTotalInvestido += item.Quantidade * item.PrecoMedio;
                valorAtualCarteira  += item.Quantidade * precoAtual;
            }

            var plTotal = valorAtualCarteira - valorTotalInvestido;
            var rentabilidadeTotal = valorTotalInvestido > 0
                ? (plTotal / valorTotalInvestido) * 100m
                : 0m;

            // Histórico de aportes e evolução da carteira por ordem de compra
            var ordens = _db.OrdensCompra
                .Where(o => o.ClienteId == clienteId)
                .OrderBy(o => o.DataCriacao)
                .ToList();

            var historicoAportes = new List<AporteDTO>();
            var evolucaoCarteira = new List<EvolucaoCarteiraDTO>();

            decimal acumuladoInvestido = 0m;
            int totalAportes = ordens.Count;

            // Fator de valorização atual da carteira (valorAtual / valorInvestido)
            // Usado para estimar o valor de mercado proporcional em cada ponto histórico
            decimal fatorValorizacao = valorTotalInvestido > 0
                ? valorAtualCarteira / valorTotalInvestido
                : 1m;

            for (int i = 0; i < ordens.Count; i++)
            {
                var ordem = ordens[i];
                acumuladoInvestido += ordem.ValorTotal;

                historicoAportes.Add(new AporteDTO
                {
                    Data = ordem.DataCriacao.ToString("yyyy-MM-dd"),
                    Valor = Math.Round(ordem.ValorTotal, 2),
                    Parcela = $"{i + 1}/{totalAportes}"
                });

                // Valor de carteira estimado naquele momento:
                // proporcional ao investido acumulado × fator de valorização atual
                var valorCarteiraEstimado = acumuladoInvestido * fatorValorizacao;
                var rentabilidadePonto = acumuladoInvestido > 0
                    ? ((valorCarteiraEstimado - acumuladoInvestido) / acumuladoInvestido) * 100m
                    : 0m;

                evolucaoCarteira.Add(new EvolucaoCarteiraDTO
                {
                    Data = ordem.DataCriacao.ToString("yyyy-MM-dd"),
                    ValorCarteira = Math.Round(valorCarteiraEstimado, 2),
                    ValorInvestido = Math.Round(acumuladoInvestido, 2),
                    Rentabilidade = Math.Round(rentabilidadePonto, 2)
                });
            }

            var response = new RentabilidadeResponse
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                DataConsulta = DateTime.UtcNow,
                Rentabilidade = new RentabilidadeSummaryDTO
                {
                    ValorTotalInvestido = Math.Round(valorTotalInvestido, 2),
                    ValorAtualCarteira  = Math.Round(valorAtualCarteira, 2),
                    PlTotal             = Math.Round(plTotal, 2),
                    RentabilidadePercentual = Math.Round(rentabilidadeTotal, 2)
                },
                HistoricoAportes = historicoAportes,
                EvolucaoCarteira = evolucaoCarteira
            };

            return Ok(response);
        }

        private string RemoverSufixoFracionario(string ticker)
        {
            return ticker.EndsWith("F") ? ticker[..^1] : ticker;
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