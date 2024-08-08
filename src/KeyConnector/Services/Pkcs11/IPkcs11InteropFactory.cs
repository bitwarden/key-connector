using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using Net.Pkcs11Interop.HighLevelAPI.MechanismParams;

namespace Bit.KeyConnector.Services.Pkcs11;

public interface IPkcs11InteropFactory
{
    IPkcs11Library LoadPkcs11Library(string libraryPath, AppType appType);
    ICkRsaPkcsOaepParams CreateCkRsaPkcsOaepParams(ulong hashAlg, ulong mgf, ulong source, byte[] sourceData);
    IMechanism CreateMechanism(CKM type);
    IMechanism CreateMechanism(CKM type, IMechanismParams parameters);
    public IObjectAttribute CreateObjectAttribute(CKA type, CKO value);
    public IObjectAttribute CreateObjectAttribute(CKA type, bool value);
    public IObjectAttribute CreateObjectAttribute(CKA type, ulong value);
    public IObjectAttribute CreateObjectAttribute(CKA type, string value);
}
