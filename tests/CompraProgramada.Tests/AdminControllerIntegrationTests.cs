using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using CompraProgramada.Domain.Entities;
using CompraProgramada.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CompraProgramada.Tests
{
    public class AdminControllerIntegrationTests
    {
        [Fact]
        public async Task ObterCestaAtual_SemCestaAtiva_DeveRetornarNotFound()
        {
            using var factory = new ApiTestWebApplicationFactory();
            using var client = factory.CreateClient();

            var response = await client.GetAsync("/api/admin/cesta/atual");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task CadastrarCesta_Valida_DeveRetornarCreatedEPermitirConsultaAtual()
        {
            using var factory = new ApiTestWebApplicationFactory();
            using var client = factory.CreateClient();

            var request = new
            {
                nome = "Top Five Fev/2026",
                itens = new[]
                {
                    new { ticker = "PETR4", percentual = 30m },
                    new { ticker = "VALE3", percentual = 25m },
                    new { ticker = "ITUB4", percentual = 20m },
                    new { ticker = "BBDC4", percentual = 15m },
                    new { ticker = "WEGE3", percentual = 10m }
                }
            };

            var createResponse = await client.PostAsJsonAsync("/api/admin/cesta", request);
            var currentResponse = await client.GetAsync("/api/admin/cesta/atual");

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, currentResponse.StatusCode);
        }

        [Fact]
        public async Task CadastrarCesta_ComPercentualInvalido_DeveRetornarBadRequest()
        {
            using var factory = new ApiTestWebApplicationFactory();
            using var client = factory.CreateClient();

            var request = new
            {
                nome = "Top Five Invalida",
                itens = new[]
                {
                    new { ticker = "PETR4", percentual = 40m },
                    new { ticker = "VALE3", percentual = 25m },
                    new { ticker = "ITUB4", percentual = 20m },
                    new { ticker = "BBDC4", percentual = 10m },
                    new { ticker = "WEGE3", percentual = 10m }
                }
            };

            var response = await client.PostAsJsonAsync("/api/admin/cesta", request);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task Historico_ComCestaCadastrada_DeveRetornarOk()
        {
            using var factory = new ApiTestWebApplicationFactory();
            using var client = factory.CreateClient();

            using (var scope = factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Cestas.Add(new CestaTopFive
                {
                    Nome = "Top Five Historico",
                    Ativa = true,
                    DataCriacao = System.DateTime.UtcNow,
                    Itens = new List<CestaItem>
                    {
                        new CestaItem { Ticker = "PETR4", Percentual = 30m },
                        new CestaItem { Ticker = "VALE3", Percentual = 25m },
                        new CestaItem { Ticker = "ITUB4", Percentual = 20m },
                        new CestaItem { Ticker = "BBDC4", Percentual = 15m },
                        new CestaItem { Ticker = "WEGE3", Percentual = 10m }
                    }
                });
                db.SaveChanges();
            }

            var response = await client.GetAsync("/api/admin/cesta/historico");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
