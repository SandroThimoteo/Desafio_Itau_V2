using System;
using System.Collections.Generic;
using System.Linq;
using CompraProgramada.Infrastructure;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CompraProgramada.Application.Services
{
    public class RentabilidadeService
    {
        private readonly ApplicationDbContext _db;
        private readonly CotahistParser _parser = new CotahistParser();

        public RentabilidadeService(ApplicationDbContext db)
        {
            _db = db;
        }

        public RentabilidadeCarteiraResponse? Calcular(long clienteId)
        {
            var cliente = _db.Clientes
                .Include(c => c.Custodia)
                .ThenInclude(c => c.Itens)
                .FirstOrDefault(c => c.Id == clienteId);

            if (cliente == null)
                return null;

            var itens = new List<RentabilidadeItemResponse>();

            foreach (var item in cliente.Custodia?.Itens ?? new List<Domain.Entities.CustodiaItem>())
            {
                var tickerBase = RemoverSufixoFracionario(item.Ticker);
                var cotacao = _parser.ObterCotacaoFechamento("cotacoes", tickerBase)
                             ?? _parser.ObterCotacaoFechamento("cotacoes", item.Ticker);

                var precoAtual = cotacao?.PrecoFechamento ?? item.PrecoMedio;
                var valorInvestido = item.Quantidade * item.PrecoMedio;
                var valorAtual = item.Quantidade * precoAtual;
                var pl = valorAtual - valorInvestido;
                var rentabilidade = valorInvestido <= 0 ? 0m : (pl / valorInvestido) * 100m;

                itens.Add(new RentabilidadeItemResponse
                {
                    Ticker = item.Ticker,
                    Quantidade = item.Quantidade,
                    PrecoMedio = item.PrecoMedio,
                    PrecoAtual = precoAtual,
                    ValorInvestido = valorInvestido,
                    ValorAtual = valorAtual,
                    Pl = pl,
                    RentabilidadePercentual = rentabilidade
                });
            }

            var valorInvestidoTotal = itens.Sum(i => i.ValorInvestido);
            var valorAtualTotal = itens.Sum(i => i.ValorAtual);
            var plTotal = valorAtualTotal - valorInvestidoTotal;
            var rentabilidadeTotal = valorInvestidoTotal <= 0 ? 0m : (plTotal / valorInvestidoTotal) * 100m;

            var ordens = _db.OrdensCompra
                .Where(o => o.ClienteId == clienteId)
                .OrderBy(o => o.DataCriacao)
                .ToList();

            decimal acumuladoInvestido = 0m;
            var historico = new List<RentabilidadeHistoricoResponse>();
            foreach (var ordem in ordens)
            {
                acumuladoInvestido += ordem.ValorTotal;
                var plAtualSobreAcumulado = valorAtualTotal - acumuladoInvestido;
                var rentAtualSobreAcumulado = acumuladoInvestido <= 0 ? 0m : (plAtualSobreAcumulado / acumuladoInvestido) * 100m;

                historico.Add(new RentabilidadeHistoricoResponse
                {
                    Data = ordem.DataCriacao,
                    ValorInvestidoAcumulado = acumuladoInvestido,
                    ValorAtualCarteira = valorAtualTotal,
                    PlAtual = plAtualSobreAcumulado,
                    RentabilidadePercentualAtual = rentAtualSobreAcumulado
                });
            }

            return new RentabilidadeCarteiraResponse
            {
                ClienteId = cliente.Id,
                Nome = cliente.Nome,
                Ativo = cliente.Ativo,
                DataReferencia = DateTime.UtcNow,
                SaldoTotal = valorAtualTotal,
                ValorInvestidoTotal = valorInvestidoTotal,
                PlTotal = plTotal,
                RentabilidadePercentual = rentabilidadeTotal,
                Itens = itens,
                HistoricoEvolucao = historico
            };
        }

        private string RemoverSufixoFracionario(string ticker)
        {
            return ticker.EndsWith("F", StringComparison.OrdinalIgnoreCase)
                ? ticker.Substring(0, ticker.Length - 1)
                : ticker;
        }
    }

    public class RentabilidadeCarteiraResponse
    {
        public long ClienteId { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativo { get; set; }
        public DateTime DataReferencia { get; set; }
        public decimal SaldoTotal { get; set; }
        public decimal ValorInvestidoTotal { get; set; }
        public decimal PlTotal { get; set; }
        public decimal RentabilidadePercentual { get; set; }
        public List<RentabilidadeItemResponse> Itens { get; set; } = new List<RentabilidadeItemResponse>();
        public List<RentabilidadeHistoricoResponse> HistoricoEvolucao { get; set; } = new List<RentabilidadeHistoricoResponse>();
    }

    public class RentabilidadeItemResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal PrecoAtual { get; set; }
        public decimal ValorInvestido { get; set; }
        public decimal ValorAtual { get; set; }
        public decimal Pl { get; set; }
        public decimal RentabilidadePercentual { get; set; }
    }

    public class RentabilidadeHistoricoResponse
    {
        public DateTime Data { get; set; }
        public decimal ValorInvestidoAcumulado { get; set; }
        public decimal ValorAtualCarteira { get; set; }
        public decimal PlAtual { get; set; }
        public decimal RentabilidadePercentualAtual { get; set; }
    }
}
