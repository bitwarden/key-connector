using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;

namespace Bit.KeyConnector.Services.ClientFactories
{
    public class HashicorpVaultClientFactory : IHashicorpVaultClientFactory
    {
        public IVaultClient CreateClient(string serverUri, string token)
        {
            var authMethod = new TokenAuthMethodInfo(token);
            var vaultClientSettings = new VaultClientSettings(serverUri, authMethod);
            return new VaultClient(vaultClientSettings);
        }
    }
}
