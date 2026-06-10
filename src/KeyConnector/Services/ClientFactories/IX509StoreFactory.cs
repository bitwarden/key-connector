using System.Security.Cryptography.X509Certificates;

namespace Bit.KeyConnector.Services.ClientFactories
{
    public interface IX509StoreFactory
    {
        X509Certificate2Collection FindByThumbprint(string thumbprint);
    }
}
