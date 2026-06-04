using System.Security.Cryptography.X509Certificates;

namespace Bit.KeyConnector.Services.ClientFactories
{
    public class X509StoreFactory : IX509StoreFactory
    {
        public X509Certificate2Collection FindByThumbprint(string thumbprint)
        {
            using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            return store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
        }
    }
}
