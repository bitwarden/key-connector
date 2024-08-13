using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Net.Pkcs11Interop.Common;
using Net.Pkcs11Interop.HighLevelAPI;
using Net.Pkcs11Interop.HighLevelAPI.MechanismParams;

namespace Bit.KeyConnector.Services.Pkcs11;

public class Pkcs11InteropFactory : IPkcs11InteropFactory
{
   private readonly Pkcs11InteropFactories _factories;

   public Pkcs11InteropFactory()
    {
        if (Platform.IsLinux)
        {
            // https://github.com/Pkcs11Interop/Pkcs11Interop/issues/239
            NativeLibrary.SetDllImportResolver(typeof(Pkcs11InteropFactories).Assembly, CustomDllImportResolver);
        }
        _factories = new Pkcs11InteropFactories();
    }

    public IPkcs11Library LoadPkcs11Library(string libraryPath, AppType appType)
    {
        return _factories.Pkcs11LibraryFactory.LoadPkcs11Library(_factories, libraryPath, appType);
    }
    
    public ICkRsaPkcsOaepParams CreateCkRsaPkcsOaepParams(ulong hashAlg, ulong mgf, ulong source, byte[] sourceData)
    {
        return _factories.MechanismParamsFactory.CreateCkRsaPkcsOaepParams(hashAlg, mgf, source, sourceData);
    }
    
    public IMechanism CreateMechanism(CKM type)
    {
        return _factories.MechanismFactory.Create(type);
    }
    
    public IMechanism CreateMechanism(CKM type, IMechanismParams parameters)
    {
        return _factories.MechanismFactory.Create(type, parameters);
    }

    public IObjectAttribute CreateObjectAttribute(CKA type, CKO value)
    {
        return _factories.ObjectAttributeFactory.Create(type, value);
    }

    public IObjectAttribute CreateObjectAttribute(CKA type, bool value)
    {
        return _factories.ObjectAttributeFactory.Create(type, value);
    }
    
    public IObjectAttribute CreateObjectAttribute(CKA type, ulong value)
    {
        return _factories.ObjectAttributeFactory.Create(type, value);
    }
    
    public IObjectAttribute CreateObjectAttribute(CKA type, string value)
    {
        return _factories.ObjectAttributeFactory.Create(type, value);
    }


    private static IntPtr CustomDllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? dllImportSearchPath)
    {
        var mappedLibraryName = (libraryName == "libdl") ? "libdl.so.2" : libraryName;
        return NativeLibrary.Load(mappedLibraryName, assembly, dllImportSearchPath);
    }
}
