using System;
using System.Threading;
using System.Threading.Tasks;
using Bit.CryptoAgent.Repositories.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bit.CryptoAgent.HostedServices
{
    public class DatabaseMigrationHostedService : IHostedService, IDisposable
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<DatabaseMigrationHostedService> _logger;

        public DatabaseMigrationHostedService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<DatabaseMigrationHostedService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var databaseContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            // Wait 1 second to allow database to come online
            await Task.Delay(1000, cancellationToken);

            var maxMigrationAttempts = 10;
            for (var i = 1; i <= maxMigrationAttempts; i++)
            {
                try
                {
                    databaseContext.Database.Migrate();
                    break;
                }
                catch (Exception e)
                {
                    if (i >= maxMigrationAttempts)
                    {
                        _logger.LogError(e, "Database failed to migrate.");
                        throw;
                    }
                    else
                    {
                        _logger.LogError(e,
                            "Database unavailable for migration. Trying again (attempt #{0})...", i + 1);
                        // Wait 5 seconds to allow database to come online
                        await Task.Delay(5000, cancellationToken);
                    }
                }
            }
        }

        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public virtual void Dispose()
        { }
    }

}
