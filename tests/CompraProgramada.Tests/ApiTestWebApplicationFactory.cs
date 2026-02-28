using System;
using System.Linq;
using CompraProgramada.Api.Services;
using CompraProgramada.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CompraProgramada.Tests
{
    public sealed class ApiTestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private readonly string _databaseName = $"CompraProgramadaTests_{Guid.NewGuid()}";

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                var dbContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                var dbContextServiceDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(ApplicationDbContext));
                if (dbContextServiceDescriptor != null)
                {
                    services.Remove(dbContextServiceDescriptor);
                }

                var workerDescriptors = services
                    .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(MotorCompraAgendadoWorker))
                    .ToList();

                foreach (var workerDescriptor in workerDescriptors)
                {
                    services.Remove(workerDescriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase(_databaseName));
            });
        }
    }
}
