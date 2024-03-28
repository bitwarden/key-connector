using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bit.KeyConnector.Repositories;
using Bit.KeyConnector.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bit.KeyConnector.HostedServices
{
    public class TransferToHostedService : IHostedService, IDisposable
    {
        private readonly IUserKeyRepository _userKeyRepository;
        private readonly ICryptoService _cryptoService;
        private readonly KeyConnectorSettings _keyConnectorSettings;
        private readonly ILogger<TransferToHostedService> _logger;

        public TransferToHostedService(
            IUserKeyRepository userKeyRepository,
            ICryptoService cryptoService,
            KeyConnectorSettings keyConnectorSettings,
            ILogger<TransferToHostedService> logger)
        {
            _userKeyRepository = userKeyRepository;
            _cryptoService = cryptoService;
            _keyConnectorSettings = keyConnectorSettings;
            _logger = logger;
        }

        public virtual async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!ShouldRun())
            {
                return;
            }

            _logger.LogInformation("Starting service to transfer to new database.");
            WriteLockfile();

            var transferToProvider = ConfigureTransferToServices();
            var transferToCryptoService = transferToProvider.GetRequiredService<ICryptoService>();
            var transferToUserKeyRepository = transferToProvider.GetRequiredService<IUserKeyRepository>();

            var userKeys = await _userKeyRepository.ReadAllAsync();
            _logger.LogInformation("Found {0} user keys to transfer.", userKeys.Count);
            foreach (var userKey in userKeys)
            {
                try
                {
                    var key = await _cryptoService.AesDecryptToB64Async(userKey.Key);
                    userKey.Key = await transferToCryptoService.AesEncryptToB64Async(key);
                    await transferToUserKeyRepository.CreateAsync(userKey);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to transfer user key {0}. Skip it.", userKey.Id);
                }
            }
            _logger.LogInformation("Finished transferring user keys.");
        }

        public virtual Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(0);
        }

        public virtual void Dispose()
        { }

        private bool ShouldRun()
        {
            if (string.IsNullOrWhiteSpace(_keyConnectorSettings.TransferTo.LockfilePath))
            {
                return false;
            }

            if (File.Exists(_keyConnectorSettings.TransferTo.LockfilePath))
            {
                _logger.LogInformation("Not running transfer service due to lock file existing at {0}.",
                    _keyConnectorSettings.TransferTo.LockfilePath);
                return false;
            }

            if (_keyConnectorSettings.TransferTo?.RsaKey == null ||
                _keyConnectorSettings.TransferTo.Database == null)
            {
                _logger.LogInformation("Not running transfer service due to missing settings.");
                return false;
            }

            return true;
        }

        private void WriteLockfile()
        {
            using var fs = File.Create(_keyConnectorSettings.TransferTo.LockfilePath);
            var nowString = Encoding.UTF8.GetBytes(DateTime.UtcNow.ToString());
            fs.Write(nowString, 0, nowString.Length);
        }

        private IServiceProvider ConfigureTransferToServices()
        {
            var settings = new KeyConnectorSettings
            {
                RsaKey = _keyConnectorSettings.TransferTo.RsaKey,
                Certificate = _keyConnectorSettings.TransferTo.Certificate,
                Database = _keyConnectorSettings.TransferTo.Database
            };
            var transferToServices = new ServiceCollection();
            transferToServices.AddSingleton(s => settings);
            transferToServices.AddRsaKeyProvider(settings);
            transferToServices.AddBaseServices();
            var efDatabaseProvider = transferToServices.AddDatabase(settings);
            var transferToProvider = transferToServices.BuildServiceProvider();
            return transferToProvider;
        }
    }
}
