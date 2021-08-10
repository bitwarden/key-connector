using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bit.CryptoAgent.Services
{
    public class StoreCertificateProviderService : ICertificateProviderService
    {
        private readonly CryptoAgentSettings _settings;

        public StoreCertificateProviderService(CryptoAgentSettings settings)
        {
            _settings = settings;
        }

        public Task<X509Certificate2> GetCertificateAsync()
        {
            X509Certificate2 cert = null;
            var certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            certStore.Open(OpenFlags.ReadOnly);
            var certCollection = certStore.Certificates.Find(X509FindType.FindByThumbprint,
                CleanThumbprint(_settings.Certificate.StoreThumbprint), false);
            if (certCollection.Count > 0)
            {
                cert = certCollection[0];
            }
            certStore.Close();
            return Task.FromResult(cert);
        }

        public static string CleanThumbprint(string thumbprint)
        {
            // Clean possible garbage characters from thumbprint copy/paste
            // ref http://stackoverflow.com/questions/8448147/problems-with-x509store-certificates-find-findbythumbprint
            return Regex.Replace(thumbprint, @"[^\da-fA-F]", string.Empty).ToUpper();
        }
    }
}
