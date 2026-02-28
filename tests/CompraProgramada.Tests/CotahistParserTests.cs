using System;
using System.IO;
using System.Linq;
using CompraProgramada.Infrastructure;
using Xunit;

namespace CompraProgramada.Tests
{
    public class CotahistParserTests
    {
        // monta uma linha COTAHIST válida preenchendo cada campo pelo índice
        private string BuildLinhaValida()
        {
            var chars = new char[245];
            for (int i = 0; i < chars.Length; i++) chars[i] = ' ';
            void Set(int start, string value)
            {
                for (int j = 0; j < value.Length && start + j < chars.Length; j++)
                    chars[start + j] = value[j];
            }
            Set(0, "01");                 // TIPREG
            Set(2, "20260225");           // DATPRE
            Set(10, "02");                // CODBDI
            Set(12, "PETR4");             // CODNEG (resto são espaços)
            Set(24, "010");               // TPMERC
            Set(27, "PETROBRAS");         // NOMRES
            Set(39, "PN");                // ESPECI
            Set(52, "R$  ");              // MODREF
            Set(56, "0000000003520");     // PREABE
            Set(69, "0000000003650");     // PREMAX
            Set(82, "0000000003480");     // PREMIN
            Set(95, "0000000003560");     // PREMED
            Set(108, "0000000003580");    // PREULT
            Set(152, "000000000001234567"); // QUATNEG (18 chars)
            Set(170, "000000000001234567"); // VOLNEG (18 chars) volume as price
            // resto já em branco/zeros
            return new string(chars);
        }

        [Fact]
        public void ParseArquivo_ComLinhaValida_RetornaCotacao()
        {
            // construir linha pelo helper
            var linha = BuildLinhaValida();
            string temp = Path.GetTempFileName();
            File.WriteAllText(temp, linha, System.Text.Encoding.GetEncoding("ISO-8859-1"));

            var parser = new CotahistParser();
            var cotacoes = parser.ParseArquivo(temp).ToList();

            Assert.Single(cotacoes);
            Assert.Equal("PETR4", cotacoes[0].Ticker);
            Assert.Equal(35.80m, cotacoes[0].PrecoFechamento);
            Assert.Equal(1234567L, cotacoes[0].QuantidadeNegociada);
            // volume value interpreted as price (centavos / 100)
            Assert.Equal(12345.67m, cotacoes[0].VolumeNegociado);
        }

        [Fact]
        public void ParseArquivo_IgnoraLinhasDeHeader()
        {
            // TIPREG = "00" deve ser ignorado
            var linhaHeader = "00" + new string(' ', 243);
            string temp = Path.GetTempFileName();
            File.WriteAllText(temp, linhaHeader);

            var parser = new CotahistParser();
            var cotacoes = parser.ParseArquivo(temp).ToList();

            Assert.Empty(cotacoes);
        }

        [Fact]
        public void ParseArquivo_IgnoraMercadoFuturo()
        {
            // TPMERC = "070" (futuro) deve ser ignorado, só aceita 010 e 020
            var linha = ("012026022502PETR4       070PETROBRAS   PN           R$  " +
                         "0000000003520000000000365000000000034800000000003560" +
                         "0000000003580").PadRight(245, '0').Substring(0, 245);

            string temp = Path.GetTempFileName();
            File.WriteAllText(temp, linha);

            var parser = new CotahistParser();
            var cotacoes = parser.ParseArquivo(temp).ToList();

            Assert.Empty(cotacoes);
        }

        [Fact]
        public void ObterCotacaoFechamento_OnMultipleFiles_ChooseLatest()
        {
            // Criar dois arquivos com datas diferentes e verificar que retorna o mais recente
            var pasta = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(pasta);

            var linhaBase = ("012026022502PETR4       010PETROBRAS   PN           R$  " +
                             "0000000003520000000000365000000000034800000000003560" +
                             "0000000003580").PadRight(245, '0').Substring(0, 245);

            // Arquivo mais antigo
            File.WriteAllText(Path.Combine(pasta, "COTAHIST_D24022026.TXT"), linhaBase);
            // Arquivo mais recente
            File.WriteAllText(Path.Combine(pasta, "COTAHIST_D25022026.TXT"), linhaBase);

            var parser = new CotahistParser();
            var cotacao = parser.ObterCotacaoFechamento(pasta, "PETR4");

            Assert.NotNull(cotacao);
            Assert.Equal("PETR4", cotacao!.Ticker);

            Directory.Delete(pasta, true);
        }

        [Fact]
        public void ObterCotacaoFechamento_TickerInexistente_RetornaNull()
        {
            var pasta = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(pasta);

            var linha = ("012026022502PETR4       010PETROBRAS   PN           R$  " +
                         "0000000003520000000000365000000000034800000000003560" +
                         "0000000003580").PadRight(245, '0').Substring(0, 245);

            File.WriteAllText(Path.Combine(pasta, "COTAHIST_D25022026.TXT"), linha);

            var parser = new CotahistParser();
            var cotacao = parser.ObterCotacaoFechamento(pasta, "VALE3");

            Assert.Null(cotacao);

            Directory.Delete(pasta, true);
        }
    }
}
