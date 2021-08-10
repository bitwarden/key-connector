using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Bit.CryptoAgent.Services
{
    public interface ICertificateProviderService
    {
        Task<X509Certificate2> GetCertificateAsync();
    }
}
