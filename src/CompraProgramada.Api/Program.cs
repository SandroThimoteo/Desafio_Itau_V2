using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Infrastructure;
using CompraProgramada.Application.Services;
using CompraProgramada.Api.Services;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext com MySQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 29))
    ));

// Kafka — usa kafka:29092 quando rodando em container, localhost:9092 localmente
var kafkaBootstrap = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
var kafkaTopic     = builder.Configuration["Kafka:Topic"]            ?? "ir-dedo-duro";
builder.Services.AddSingleton(new KafkaProducer(kafkaBootstrap, kafkaTopic));
builder.Services.AddSingleton<IrPublisher>();

// Application Services
builder.Services.AddScoped<CestaService>();
builder.Services.AddScoped<ClienteService>();
builder.Services.AddScoped<MotorCompraService>();
builder.Services.AddScoped<IRebalanceService, RebalanceService>();
builder.Services.AddScoped<RentabilidadeService>();
builder.Services.AddSingleton<MotorAgendamentoStatusStore>();
builder.Services.AddHostedService<MotorCompraAgendadoWorker>();

var app = builder.Build();

// Aplicar migrations automaticamente ao subir
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Use minimal route registration to satisfy analyzers (ASP0014)
app.MapControllers();

app.Run();
