using VaultSharp;

namespace Bit.KeyConnector.Services.ClientFactories
{
    public interface IHashicorpVaultClientFactory
    {
        IVaultClient CreateClient(string serverUri, string token);
    }
}
