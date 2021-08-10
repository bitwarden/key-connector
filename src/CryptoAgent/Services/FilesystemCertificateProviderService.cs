using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Bit.CryptoAgent.Services
{
    public class FilesystemCertificateProviderService : ICertificateProviderService
    {
        private readonly CryptoAgentSettings _settings;

        public FilesystemCertificateProviderService(CryptoAgentSettings settings)
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
