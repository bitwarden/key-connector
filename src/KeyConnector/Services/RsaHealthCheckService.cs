using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Bit.KeyConnector.Services
{
    public class RsaHealthCheckService : IHealthCheck
    {
        private IRsaKeyService _rsaKeyService;

        public RsaHealthCheckService(IRsaKeyService rsaKeyService)
        {
            _rsaKeyService = rsaKeyService;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var publicKey = await _rsaKeyService.GetPublicKeyAsync();

            return publicKey != null
                ? HealthCheckResult.Healthy("Healthy")
                : HealthCheckResult.Unhealthy("Unhealthy");
        }
    }
}
