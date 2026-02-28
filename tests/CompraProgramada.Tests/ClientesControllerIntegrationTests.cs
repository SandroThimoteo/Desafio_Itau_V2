using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CompraProgramada.Tests
{
    public class ClientesControllerIntegrationTests
    {
        [Fact]
        public async Task Adesao_ComDadosValidos_DeveRetornarCreated()
        {
            using var factory = new ApiTestWebApplicationFactory();
            using var client = factory.CreateClient();

            var request = new
            {
                nome = "Cliente API",
                cpf = "12345678901",
                email = "cliente@api.com",
                valorMensal = 1000m
            };

            var response = await client.PostAsJsonAsync("/api/clientes/adesao", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        [Fact]
        public async Task Adesao_ComValorMensalInvalido_DeveRetornarBadRequest()
        {
            using var factory = new ApiTestWebApplicationFactory();
            using var client = factory.CreateClient();

            var request = new
            {
                nome = "Cliente API",
                cpf = "12345678901",
                email = "cliente@api.com",
                valorMensal = 99m
            };

            var response = await client.PostAsJsonAsync("/api/clientes/adesao", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AlterarValor_ComClienteInexistente_DeveRetornarNotFound()
        {
            using var factory = new ApiTestWebApplicationFactory();
            using var client = factory.CreateClient();

            var request = new { novoValorMensal = 1500m };

            var response = await client.PutAsJsonAsync("/api/clientes/999/valor-mensal", request);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Saida_ComClienteInexistente_DeveRetornarNotFound()
        {
            using var factory = new ApiTestWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.PostAsync("/api/clientes/999/saida", null);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ConsultarCarteira_ComClienteExistente_DeveRetornarOk()
        {
            using var factory = new ApiTestWebApplicationFactory();
            using var client = factory.CreateClient();

            long clienteId;
            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var cliente = new Cliente
                {
                    Nome = "Cliente Carteira",
                    CPF = "98765432100",
                    Email = "carteira@api.com",
                    ValorMensal = 1200m,
                    Ativo = true,
                    DataAdesao = System.DateTime.UtcNow,
                    ContaGrafica = new ContaGrafica
                    {
                        NumeroConta = "CLI-0001",
                        Tipo = ContaTipo.Filhote,
                        DataCriacao = System.DateTime.UtcNow
                    },
                    Custodia = new Custodia()
                };

                db.Clientes.Add(cliente);
                db.SaveChanges();
                clienteId = cliente.Id;
            }

            var response = await client.GetAsync($"/api/clientes/{clienteId}/carteira");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task ConsultarRentabilidade_ComClienteInexistente_DeveRetornarNotFound()
        {
            using var factory = new ApiTestWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/clientes/999/rentabilidade");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
