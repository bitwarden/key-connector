using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Bit.KeyConnector.Services
{
    public class FilesystemCertificateProviderService : ICertificateProviderService
    {
        private readonly KeyConnectorSettings _settings;

        public FilesystemCertificateProviderService(KeyConnectorSettings settings)
        {
            _settings = settings;
        }

        public Task<X509Certificate2> GetCertificateAsync()
        {
            var cert = new X509Certificate2(_settings.Certificate.FilesystemPath,
                _settings.Certificate.FilesystemPassword);
            return Task.FromResult(cert);
        }
    }
}
