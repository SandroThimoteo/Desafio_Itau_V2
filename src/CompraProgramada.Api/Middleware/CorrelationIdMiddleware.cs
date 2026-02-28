using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace CompraProgramada.Api.Middleware
{
    /// <summary>
    /// Middleware que adiciona um Correlation ID único a cada requisição HTTP.
    /// Permite rastrear logs relacionados através de todas as camadas da aplicação.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private const string CorrelationIdLogKey = "CorrelationId";
        
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Se não houver Correlation ID na requisição, gerar um novo
            var correlationId = context.Request.Headers[CorrelationIdHeader].ToString();
            
            if (string.IsNullOrEmpty(correlationId))
            {
                correlationId = Guid.NewGuid().ToString();
            }

            // Adicionar Correlation ID ao contexto HTTP e à resposta
            context.Items[CorrelationIdLogKey] = correlationId;
            context.Response.Headers[CorrelationIdHeader] = correlationId;

            // Adicionar Correlation ID ao scope de logging (DiagnosticContext)
            using (_logger.BeginScope(new[] { new KeyValuePair<string, object>(CorrelationIdLogKey, correlationId) }))
            {
                _logger.LogInformation(
                    "Iniciando requisição {Method} {Path} - Correlation ID: {CorrelationId}",
                    context.Request.Method,
                    context.Request.Path,
                    correlationId
                );

                try
                {
                    await _next(context);

                    _logger.LogInformation(
                        "Requisição concluída {Method} {Path} com status {StatusCode}",
                        context.Request.Method,
                        context.Request.Path,
                        context.Response.StatusCode
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Erro ao processar requisição {Method} {Path}",
                        context.Request.Method,
                        context.Request.Path
                    );
                    throw;
                }
            }
        }
    }
}
