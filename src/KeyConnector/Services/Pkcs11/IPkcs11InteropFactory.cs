using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using Net.Pkcs11Interop.HighLevelAPI.Factories;
using Net.Pkcs11Interop.HighLevelAPI.MechanismParams;

namespace Bit.KeyConnector.Services.Pkcs11;

public interface IPkcs11InteropFactory
{
    /// <inheritdoc cref="IPkcs11LibraryFactory.LoadPkcs11Library(Pkcs11InteropFactories, string, AppType)"/>
    IPkcs11Library LoadPkcs11Library(string libraryPath, AppType appType);
    /// <inheritdoc cref="IMechanismParamsFactory.CreateCkRsaPkcsOaepParams(ulong, ulong, ulong, byte[])"/>
    ICkRsaPkcsOaepParams CreateCkRsaPkcsOaepParams(ulong hashAlg, ulong mgf, ulong source, byte[] sourceData);
    /// <inheritdoc cref="IMechanismFactory.Create(CKM)"/>
    IMechanism CreateMechanism(CKM type);
    /// <inheritdoc cref="IMechanismFactory.Create(CKM, IMechanismParams)"/>
    IMechanism CreateMechanism(CKM type, IMechanismParams parameters);
    /// <inheritdoc cref="IObjectAttributeFactory.Create(CKA, CKO)"/>
    public IObjectAttribute CreateObjectAttribute(CKA type, CKO value);
    /// <inheritdoc cref="IObjectAttributeFactory.Create(CKA, bool)"/>
    public IObjectAttribute CreateObjectAttribute(CKA type, bool value);
    /// <inheritdoc cref="IObjectAttributeFactory.Create(CKA, ulong)"/>
    public IObjectAttribute CreateObjectAttribute(CKA type, ulong value);
    /// <inheritdoc cref="IObjectAttributeFactory.Create(CKA, string)"/>
    public IObjectAttribute CreateObjectAttribute(CKA type, string value);
}
