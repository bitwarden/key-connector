using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;

namespace Bit.CryptoAgent.Services
{
    public class Pkcs11RsaKeyService : IRsaKeyService
    {
        private readonly ICertificateProviderService _certificateProviderService;
        private readonly ICryptoFunctionService _cryptoFunctionService;
        private readonly CryptoAgentSettings _settings;

        private X509Certificate2 _certificate;

        public Pkcs11RsaKeyService(
            ICertificateProviderService certificateProviderService,
            ICryptoFunctionService cryptoFunctionService,
            CryptoAgentSettings settings)
        {
            _certificateProviderService = certificateProviderService;
            _cryptoFunctionService = cryptoFunctionService;
            _settings = settings;
        }

        public async Task<byte[]> EncryptAsync(byte[] data)
        {
            if (data == null)
            {
                return null;
            }
            var encData = await _cryptoFunctionService.RsaEncryptAsync(data, await GetPublicKeyAsync());
            return encData;
        }

        public Task<byte[]> DecryptAsync(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            using var library = LoadLibrary();
            using var session = CreateNewSession(library);
            var privateKey = GetPrivateKey(session);

            var mechanismParams = session.Factories.MechanismParamsFactory.CreateCkRsaPkcsOaepParams(
                ConvertUtils.UInt64FromCKM(CKM.CKM_SHA_1),
                ConvertUtils.UInt64FromCKG(CKG.CKG_MGF1_SHA1),
                ConvertUtils.UInt64FromUInt32(CKZ.CKZ_DATA_SPECIFIED),
                null);
            var mechanism = session.Factories.MechanismFactory.Create(CKM.CKM_RSA_PKCS_OAEP, mechanismParams);
            var plainData = session.Decrypt(mechanism, privateKey, data);

            Cleanup(session, privateKey);
            return Task.FromResult(plainData);
        }

        public Task<byte[]> SignAsync(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            using var library = LoadLibrary();
            using var session = CreateNewSession(library);
            var privateKey = GetPrivateKey(session);

            var mechanism = session.Factories.MechanismFactory.Create(CKM.CKM_SHA256_RSA_PKCS);
            var signature = session.Sign(mechanism, privateKey, data);

            Cleanup(session, privateKey);
            return Task.FromResult(signature);
        }

        public async Task<bool> VerifyAsync(byte[] data, byte[] signature)
        {
            if (data == null || signature == null)
            {
                return false;
            }
            return await _cryptoFunctionService.RsaVerifyAsync(data, signature, await GetPublicKeyAsync());
        }

        public async Task<byte[]> GetPublicKeyAsync()
        {
            var certificate = await GetCertificateAsync();
            return certificate.GetRSAPublicKey().ExportSubjectPublicKeyInfo();
        }

        private async Task<X509Certificate2> GetCertificateAsync()
        {
            if (_certificate == null)
            {
                _certificate = await _certificateProviderService.GetCertificateAsync();
            }

            return _certificate;
        }

        private IObjectHandle GetPrivateKey(ISession session)
        {
            var attributes = new List<IObjectAttribute>
            {
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_CLASS, CKO.CKO_PRIVATE_KEY),
                session.Factories.ObjectAttributeFactory.Create(CKA.CKA_TOKEN, true)
            };
            if (_settings.RsaKey.Pkcs11PrivateKeyId.HasValue)
            {
                attributes.Add(session.Factories.ObjectAttributeFactory.Create(CKA.CKA_ID,
                    _settings.RsaKey.Pkcs11PrivateKeyId.Value));
            }
            else
            {
                attributes.Add(session.Factories.ObjectAttributeFactory.Create(CKA.CKA_LABEL,
                    _settings.RsaKey.Pkcs11PrivateKeyLabel));
            }

            var objects = session.FindAllObjects(attributes);
            if (objects.Count == 0)
            {
                throw new System.Exception("Private key not found.");
            }
            else if (objects.Count > 1)
            {
                throw new System.Exception("More than one private key was found. Use a more specific identifier.");
            }

            return objects.Single();
        }
        private IPkcs11Library LoadLibrary()
        {
            var libPath = _settings.RsaKey.Pkcs11LibraryPath;
            if (string.IsNullOrWhiteSpace(libPath))
            {
                var provider = _settings.RsaKey.Pkcs11Provider?.ToLowerInvariant();
                if (provider == "yubihsm2")
                {
                    // TODO: Verify that this path works for Debian-installed YubiHSM SDKs
                    libPath = "/usr/lib/x86_64-linux-gnu/pkcs11/yubihsm_pkcs11.so";
                }
            }

            var factories = new Pkcs11InteropFactories();
            return factories.Pkcs11LibraryFactory.LoadPkcs11Library(factories, libPath, AppType.MultiThreaded);
        }

        private ISession CreateNewSession(IPkcs11Library library)
        {
            ISlot chosenSlot = null;
            var slots = library.GetSlotList(SlotsType.WithOrWithoutTokenPresent);
            foreach (var slot in slots)
            {
                var slotInfo = slot.GetSlotInfo();
                if (slotInfo.SlotFlags.TokenPresent)
                {
                    var tokenInfo = slot.GetTokenInfo();
                    if (tokenInfo.SerialNumber == _settings.RsaKey.Pkcs11SlotTokenSerialNumber)
                    {
                        chosenSlot = slot;
                        break;
                    }
                }
            }

            if (chosenSlot == null)
            {
                return null;
            }

            var session = chosenSlot.OpenSession(SessionType.ReadWrite);

            var userType = CKU.CKU_USER;
            var userTypeSetting = _settings.RsaKey.Pkcs11LoginUserType?.ToLowerInvariant();
            if (userTypeSetting == "so")
            {
                userType = CKU.CKU_SO;
            }
            else if (userTypeSetting == "context_specific")
            {
                userType = CKU.CKU_CONTEXT_SPECIFIC;
            }
            session.Login(userType, _settings.RsaKey.Pkcs11LoginPin);

            return session;
        }

        private void Cleanup(ISession session, IObjectHandle privateKey)
        {
            session.DestroyObject(privateKey);
            session.Logout();
        }
    }
}
