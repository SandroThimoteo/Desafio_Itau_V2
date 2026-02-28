using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CompraProgramada.Application.Services;
using CompraProgramada.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Api.Services
{
    public class MotorCompraAgendadoWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly MotorAgendamentoStatusStore _statusStore;
        private readonly ILogger<MotorCompraAgendadoWorker> _logger;

        public MotorCompraAgendadoWorker(
            IServiceScopeFactory scopeFactory,
            MotorAgendamentoStatusStore statusStore,
            ILogger<MotorCompraAgendadoWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _statusStore = statusStore;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await ExecutarCiclosPendentes(stoppingToken);

                try
                {
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        private async Task ExecutarCiclosPendentes(CancellationToken ct)
        {
            var ciclosPendentes = CalendarioCompraProgramada.ObterCiclosPendentes(
                DateTime.UtcNow,
                _statusStore.ObterCiclosExecutados());

            if (!ciclosPendentes.Any())
                return;

            foreach (var ciclo in ciclosPendentes)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var motor = scope.ServiceProvider.GetRequiredService<MotorCompraService>();

                    var clientesAtivos = db.Clientes.Where(c => c.Ativo).ToList();
                    var cesta = db.Cestas.Include(c => c.Itens).FirstOrDefault(c => c.Ativa);

                    if (!clientesAtivos.Any() || cesta == null)
                    {
                        _logger.LogWarning("Motor agendado ignorado no ciclo {Ciclo} por falta de clientes ativos ou cesta ativa.", ciclo.Chave);
                        continue;
                    }

                    var saldoMaster = new Dictionary<string, decimal>();
                    motor.ExecutarCompra(ciclo.DataReferenciaUtc, clientesAtivos, cesta, saldoMaster);

                    _statusStore.RegistrarSucesso(ciclo.Chave, ciclo.DataReferenciaUtc);
                    _logger.LogInformation("Motor agendado executado com sucesso para ciclo {Ciclo}.", ciclo.Chave);
                }
                catch (Exception ex)
                {
                    _statusStore.RegistrarFalha(ciclo.DataReferenciaUtc, ex.Message);
                    _logger.LogError(ex, "Falha ao executar motor agendado no ciclo {Ciclo}.", ciclo.Chave);
                }
            }

            await Task.CompletedTask;
        }
    }
}
