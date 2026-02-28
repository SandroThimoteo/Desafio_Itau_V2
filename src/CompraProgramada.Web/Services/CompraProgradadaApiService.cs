using System.Net.Http.Json;

namespace CompraProgramada.Web.Services
{
    /// <summary>
    /// Serviço HTTP para comunicação com a API de Compra Programada
    /// </summary>
    public class CompraProgradadaApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl;

        public CompraProgradadaApiService(HttpClient httpClient, string apiBaseUrl = "http://localhost:5000/api")
        {
            _httpClient = httpClient;
            _apiBaseUrl = apiBaseUrl;
        }

        /// <summary>
        /// Obter carteira de um cliente (posição de ativos)
        /// </summary>
        public async Task<CarteiraResponse?> GetCarteiraAsync(long clienteId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<CarteiraResponse>(
                    $"{_apiBaseUrl}/clientes/{clienteId}/carteira"
                );
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Erro ao buscar carteira do cliente {clienteId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obter informações de rentabilidade de um cliente
        /// </summary>
        public async Task<RentabilidadeResponse?> GetRentabilidadeAsync(long clienteId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<RentabilidadeResponse>(
                    $"{_apiBaseUrl}/clientes/{clienteId}/rentabilidade"
                );
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Erro ao buscar rentabilidade do cliente {clienteId}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Cadastrar novo cliente (adesão ao produto)
        /// </summary>
        public async Task<ClienteResponse?> AderirProdutoAsync(string nome, string cpf, string email, decimal valorMensal)
        {
            try
            {
                var request = new
                {
                    Nome = nome,
                    CPF = cpf,
                    Email = email,
                    ValorMensal = valorMensal
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/clientes/adesao",
                    request
                );

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Erro na adesão: {errorContent}");
                }

                var json = await response.Content.ReadAsStringAsync();
                return System.Text.Json.JsonSerializer.Deserialize<ClienteResponse>(json);
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Erro ao aderir ao produto: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Alterar valor mensal de aporte de um cliente
        /// </summary>
        public async Task<bool> AlterarValorMensalAsync(long clienteId, decimal novoValor)
        {
            try
            {
                var request = new { NovoValorMensal = novoValor };

                var response = await _httpClient.PutAsJsonAsync(
                    $"{_apiBaseUrl}/clientes/{clienteId}/valor-mensal",
                    request
                );

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Erro ao alterar valor mensal: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Sair do produto (encerrar adesão)
        /// </summary>
        public async Task<bool> SairDoProdutoAsync(long clienteId)
        {
            try
            {
                var response = await _httpClient.PostAsync(
                    $"{_apiBaseUrl}/clientes/{clienteId}/saida",
                    null
                );

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Erro ao sair do produto: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obter cesta de recomendação atual (Admin)
        /// </summary>
        public async Task<CestaResponse?> GetCestaAtualAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<CestaResponse>(
                    $"{_apiBaseUrl}/admin/cesta-atual"
                );
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Erro ao buscar cesta atual: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Atualizar cesta de recomendação (Admin)
        /// </summary>
        public async Task<bool> AtualizarCestaAsync(string nome, List<CestaItemRequest> itens)
        {
            try
            {
                var request = new
                {
                    Nome = nome,
                    Itens = itens
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_apiBaseUrl}/admin/atualizar-cesta",
                    request
                );

                return response.IsSuccessStatusCode;
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Erro ao atualizar cesta: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Obter histórico de cestas (Admin)
        /// </summary>
        public async Task<List<CestaHistoricoResponse>?> GetHistoricoCestasAsync()
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<CestaHistoricoResponse>>(
                    $"{_apiBaseUrl}/admin/historico-cestas"
                );
            }
            catch (HttpRequestException ex)
            {
                throw new Exception($"Erro ao buscar histórico de cestas: {ex.Message}", ex);
            }
        }
    }

    // DTOs para Request/Response
    public class CarteiraResponse
    {
        public long ClienteId { get; set; }
        public string ClienteNome { get; set; } = string.Empty;
        public List<AtivoCarteira> Ativos { get; set; } = new();
        public decimal SaldoTotal { get; set; }
        public decimal PLTotal { get; set; }
        public decimal RentabilidadePercentual { get; set; }
    }

    public class AtivoCarteira
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Quantidade { get; set; }
        public decimal PrecoMedio { get; set; }
        public decimal ValorAtual { get; set; }
        public decimal PL { get; set; }
        public decimal Percentual { get; set; }
    }

    public class RentabilidadeResponse
    {
        public long ClienteId { get; set; }
        public decimal SaldoTotal { get; set; }
        public decimal RendimentosTotal { get; set; }
        public decimal LucroTotal { get; set; }
        public decimal RentabilidadePercentualUltimo30Dias { get; set; }
        public decimal RentabilidadePercentualAnual { get; set; }
        public List<RentabilidadeItemResponse> Itens { get; set; } = new();
    }

    public class RentabilidadeItemResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal QuantidadeAtual { get; set; }
        public decimal ValorInvestido { get; set; }
        public decimal ValorAtual { get; set; }
        public decimal Lucro { get; set; }
        public decimal RentabilidadePercentual { get; set; }
    }

    public class ClienteResponse
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public decimal ValorMensal { get; set; }
        public DateTime DataAdesao { get; set; }
        public string ContaGrafica { get; set; } = string.Empty;
    }

    public class CestaResponse
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public bool Ativa { get; set; }
        public DateTime DataCriacao { get; set; }
        public List<CestaItemResponse> Itens { get; set; } = new();
    }

    public class CestaItemResponse
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }

    public class CestaItemRequest
    {
        public string Ticker { get; set; } = string.Empty;
        public decimal Percentual { get; set; }
    }

    public class CestaHistoricoResponse
    {
        public long Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public DateTime DataCriacao { get; set; }
        public DateTime? DataDesativacao { get; set; }
        public bool Ativa { get; set; }
        public List<CestaItemResponse> Itens { get; set; } = new();
    }
}
