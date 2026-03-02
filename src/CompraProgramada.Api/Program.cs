using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using CompraProgramada.Infrastructure.Data;
using CompraProgramada.Infrastructure;
using CompraProgramada.Application.Services;
using CompraProgramada.Api.Services;
using CompraProgramada.Api.Middleware;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configurar logging estruturado
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// CORS — permitir requisições do frontend React
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

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
    if (db.Database.IsRelational())
    {
        db.Database.Migrate();
    }
    else
    {
        db.Database.EnsureCreated();
    }
}

// Habilitar Swagger em Development e Production (para facilitar desenvolvimento local)
app.UseDeveloperExceptionPage();

// Middleware CORS manual — garante headers em todas as requisições
app.Use(async (context, next) =>
{
    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
    context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
    context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 200;
        await context.Response.CompleteAsync();
        return;
    }
    await next();
});

// Middleware de Correlation ID para rastreamento de requisições
app.UseMiddleware<CorrelationIdMiddleware>();

// Habilitar CORS
app.UseCors("AllowBlazorFrontend");

app.UseSwagger();
app.UseSwaggerUI();

// Use minimal route registration to satisfy analyzers (ASP0014)
app.MapControllers();

app.Run();

public partial class Program
{
}