using System;
using System.Threading;
using System.Threading.Tasks;

namespace CompraProgramada.Application.Services
{
    public interface IRebalanceService
    {
        Task<RebalanceResultado> RebalancearPorMudancaDeCestaAsync(long cestaId, DateTime dataReferencia, CancellationToken ct = default);
        Task<RebalanceResultado> RebalancearPorDesvioAsync(decimal toleranciaPercentual, DateTime dataReferencia, CancellationToken ct = default);
    }
}
