using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bit.KeyConnector.Services.ClientFactories;

namespace Bit.KeyConnector.Services.CertificateProviders
{
    public class StoreCertificateProviderService : ICertificateProviderService
    {
        private readonly KeyConnectorSettings _settings;
        private readonly IX509StoreFactory _storeFactory;

        public StoreCertificateProviderService(KeyConnectorSettings settings, IX509StoreFactory storeFactory)
        {
            _settings = settings;
            _storeFactory = storeFactory;
        }

        public Task<X509Certificate2> GetCertificateAsync()
        {
            X509Certificate2 cert = null;
            var certCollection = _storeFactory.FindByThumbprint(
                CleanThumbprint(_settings.Certificate.StoreThumbprint));
            if (certCollection.Count > 0)
            {
                cert = certCollection[0];
            }
            return Task.FromResult(cert);
        }

        private static string CleanThumbprint(string thumbprint)
        {
            // Clean possible garbage characters from thumbprint copy/paste
            // ref http://stackoverflow.com/questions/8448147/problems-with-x509store-certificates-find-findbythumbprint
            return Regex.Replace(thumbprint, @"[^\da-fA-F]", string.Empty).ToUpper();
        }
    }
}
