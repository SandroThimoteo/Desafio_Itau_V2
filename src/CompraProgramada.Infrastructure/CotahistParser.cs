using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompraProgramada.Infrastructure
{
    public class CotacaoB3
    {
        public DateTime DataPregao { get; set; }
        public string Ticker { get; set; } = string.Empty;
        public string CodigoBDI { get; set; } = string.Empty;
        public int TipoMercado { get; set; }
        public string NomeEmpresa { get; set; } = string.Empty;
        public decimal PrecoAbertura { get; set; }
        public decimal PrecoMaximo { get; set; }
        public decimal PrecoMinimo { get; set; }
        public decimal PrecoFechamento { get; set; }
        public decimal PrecoMedio { get; set; }
        public long QuantidadeNegociada { get; set; }
        public decimal VolumeNegociado { get; set; }
    }

    public class CotahistParser
    {
        /// <summary>
        /// Le e faz parse de um arquivo COTAHIST da B3.
        /// Retorna apenas registros de detalhe (TIPREG = 01)
        /// filtrados por mercado a vista (010) e fracionario (020).
        /// </summary>
        public IEnumerable<CotacaoB3> ParseArquivo(string caminhoArquivo)
        {
            var cotacoes = new List<CotacaoB3>();

            // encoding ISO-8859-1
            var encoding = System.Text.Encoding.GetEncoding("ISO-8859-1");
            foreach (var linha in File.ReadLines(caminhoArquivo, encoding))
            {
                if (linha.Length < 245)
                    continue;

                var tipoRegistro = linha.Substring(0, 2);
                if (tipoRegistro != "01")
                    continue;

                var tipoMercado = int.Parse(linha.Substring(24, 3).Trim());

                // Filtrar apenas mercado a vista (010) e fracionario (020)
                if (tipoMercado != 10 && tipoMercado != 20)
                    continue;

                var cotacao = new CotacaoB3
                {
                    DataPregao = DateTime.ParseExact(
                        linha.Substring(2, 8), "yyyyMMdd",
                        System.Globalization.CultureInfo.InvariantCulture),
                    CodigoBDI = linha.Substring(10, 2).Trim(),
                    Ticker = CleanTicker(linha.Substring(12, 12)),
                    TipoMercado = tipoMercado,
                    NomeEmpresa = linha.Substring(27, 12).Trim(),
                    PrecoAbertura = ParsePreco(linha.Substring(56, 13)),
                    PrecoMaximo = ParsePreco(linha.Substring(69, 13)),
                    PrecoMinimo = ParsePreco(linha.Substring(82, 13)),
                    PrecoMedio = ParsePreco(linha.Substring(95, 13)),
                    PrecoFechamento = ParsePreco(linha.Substring(108, 13)),
                    QuantidadeNegociada = ParseLong(linha.Substring(152, 18)),
                    VolumeNegociado = ParsePreco(linha.Substring(170, 18))
                };

                cotacoes.Add(cotacao);
            }

            return cotacoes;
        }

        public CotacaoB3? ObterCotacaoFechamento(string pastaCotacoes, string ticker)
        {
            var arquivos = Directory.GetFiles(pastaCotacoes, "COTAHIST_D*.TXT")
                .OrderByDescending(f => f)
                .ToList();

            foreach (var arquivo in arquivos)
            {
                var cotacoes = ParseArquivo(arquivo);
                var cotacao = cotacoes
                    .Where(c => c.Ticker.Equals(ticker, StringComparison.OrdinalIgnoreCase))
                    .Where(c => c.TipoMercado == 10) // Mercado a vista
                    .FirstOrDefault();

                if (cotacao != null)
                    return cotacao;
            }

            return null;
        }

        private decimal ParsePreco(string valorBruto)
        {
            if (long.TryParse(valorBruto.Trim(), out var valor))
                return valor / 100m;
            return 0m;
        }

        private long ParseLong(string valorBruto)
        {
            if (long.TryParse(valorBruto.Trim(), out var valor))
                return valor;
            return 0L;
        }

        private string CleanTicker(string raw)
        {
            // trim spaces
            var t = raw.Trim();
            // remove any leading zeros which some arquivos pad erroneously
            return t.TrimStart('0');
        }
    }
}
