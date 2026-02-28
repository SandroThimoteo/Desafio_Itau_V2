using Microsoft.Extensions.Logging;
using NSubstitute;

namespace CompraProgramada.Tests
{
    /// <summary>
    /// Helper para criar mocks de ILogger em testes
    /// </summary>
    internal static class LoggerTestHelper
    {
        /// <summary>
        /// Cria um logger mock que não faz nada
        /// Útil para testar serviços que têm ILogger como dependência
        /// </summary>
        public static ILogger<T> CreateMockLogger<T>() where T : class
        {
            return Substitute.For<ILogger<T>>();
        }
    }
}
