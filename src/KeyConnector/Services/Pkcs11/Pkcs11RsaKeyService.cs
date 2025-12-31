using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;

namespace Bit.KeyConnector.Services.Pkcs11
{
    public class Pkcs11RsaKeyService : IRsaKeyService
    {
        private readonly ICertificateProviderService _certificateProviderService;
        private readonly ICryptoFunctionService _cryptoFunctionService;
        private readonly IPkcs11InteropFactory _pkcs11InteropFactory;
        private readonly KeyConnectorSettings _settings;

        private X509Certificate2 _certificate;

        public Pkcs11RsaKeyService(
            ICertificateProviderService certificateProviderService,
            ICryptoFunctionService cryptoFunctionService,
            IPkcs11InteropFactory pkcs11LibraryFactory,
            KeyConnectorSettings settings)
        {
            _certificateProviderService = certificateProviderService;
            _cryptoFunctionService = cryptoFunctionService;
            _pkcs11InteropFactory = pkcs11LibraryFactory;
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
                return Task.FromResult<byte[]>(null);
            }

            using var library = LoadLibrary();
            using var session = CreateNewSession(library);
            var privateKey = GetPrivateKey(session);

            var mechanismParams = _pkcs11InteropFactory.CreateCkRsaPkcsOaepParams(
                ConvertUtils.UInt64FromCKM(CKM.CKM_SHA_1),
                ConvertUtils.UInt64FromCKG(CKG.CKG_MGF1_SHA1),
                ConvertUtils.UInt64FromUInt32(CKZ.CKZ_DATA_SPECIFIED),
                null);
            var mechanism = _pkcs11InteropFactory.CreateMechanism(CKM.CKM_RSA_PKCS_OAEP, mechanismParams);
            var plainData = session.Decrypt(mechanism, privateKey, data);

            session.Logout();
            return Task.FromResult(plainData);
        }

        public Task<byte[]> SignAsync(byte[] data)
        {
            if (data == null)
            {
                return Task.FromResult<byte[]>(null);
            }

            using var library = LoadLibrary();
            using var session = CreateNewSession(library);
            var privateKey = GetPrivateKey(session);

            var mechanism = _pkcs11InteropFactory.CreateMechanism(CKM.CKM_SHA256_RSA_PKCS);
            var signature = session.Sign(mechanism, privateKey, data);

            session.Logout();
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
                _pkcs11InteropFactory.CreateObjectAttribute(CKA.CKA_CLASS, CKO.CKO_PRIVATE_KEY),
                _pkcs11InteropFactory.CreateObjectAttribute(CKA.CKA_TOKEN, true)
            };
            if (_settings.RsaKey.Pkcs11PrivateKeyId.HasValue)
            {
                attributes.Add(_pkcs11InteropFactory.CreateObjectAttribute(CKA.CKA_ID,
                    _settings.RsaKey.Pkcs11PrivateKeyId.Value));
            }
            else
            {
                attributes.Add(_pkcs11InteropFactory.CreateObjectAttribute(CKA.CKA_LABEL,
                    _settings.RsaKey.Pkcs11PrivateKeyLabel));
            }

            var objects = session.FindAllObjects(attributes);
            return objects.Count switch
            {
                0 => throw new System.Exception("Private key not found."),
                > 1 => throw new System.Exception(
                    "More than one private key was found. Use a more specific identifier."),
                _ => objects.Single()
            };
        }

        private IPkcs11Library LoadLibrary()
        {
            var libPath = _settings.RsaKey.Pkcs11LibraryPath;
            if (string.IsNullOrWhiteSpace(libPath))
            {
                var provider = _settings.RsaKey.Pkcs11Provider?.ToLowerInvariant();
                libPath = provider switch
                {
                    "yubihsm" => "/usr/lib/x86_64-linux-gnu/pkcs11/yubihsm_pkcs11.so",
                    "opensc" => "/usr/lib/x86_64-linux-gnu/opensc-pkcs11.so",
                    "futurex" => "/usr/lib/x86_64-linux-gnu/fxpkcs11/libfxpkcs11.so",
                    _ => throw new Exception("Please provide a library path or known provider.")
                };
            }

            var library = _pkcs11InteropFactory.LoadPkcs11Library(libPath, AppType.MultiThreaded);
            if (library == null)
            {
                throw new Exception("Cannot load library.");
            }

            return library;
        }

        private ISession CreateNewSession(IPkcs11Library library)
        {
            var slotTokenSerialNumber = _settings.RsaKey.Pkcs11SlotTokenSerialNumber?.ToLowerInvariant();
            var userTypeSetting = _settings.RsaKey.Pkcs11LoginUserType?.ToLowerInvariant();
            var loginPin = _settings.RsaKey.Pkcs11LoginPin;

            ISlot chosenSlot = null;
            var slots = library.GetSlotList(SlotsType.WithOrWithoutTokenPresent);
            foreach (var slot in slots)
            {
                var slotInfo = slot.GetSlotInfo();
                if (!slotInfo.SlotFlags.TokenPresent)
                {
                    continue;
                }

                try
                {
                    var tokenInfo = slot.GetTokenInfo();
                    if (tokenInfo?.SerialNumber?.ToLowerInvariant() == slotTokenSerialNumber)
                    {
                        chosenSlot = slot;
                        break;
                    }
                }
                catch (Pkcs11Exception) {}
            }

            if (chosenSlot == null)
            {
                throw new Exception("Cannot locate token slot.");
            }

            // TODO: read only?
            var session = chosenSlot.OpenSession(SessionType.ReadWrite);

            var userType = userTypeSetting switch
            {
                "so" => CKU.CKU_SO,
                "context_specific" => CKU.CKU_CONTEXT_SPECIFIC,
                _ => CKU.CKU_USER
            };

            session.Login(userType, loginPin);
            return session;
        }
    }
}
