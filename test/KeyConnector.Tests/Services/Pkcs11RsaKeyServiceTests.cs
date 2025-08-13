using Bit.KeyConnector.Services;
using Xunit;
using NSubstitute;
using Bit.KeyConnector;
using System.Threading.Tasks;
using Net.Pkcs11Interop.HighLevelAPI;
using Net.Pkcs11Interop.Common;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Bit.KeyConnector.Services.Pkcs11;
using Net.Pkcs11Interop.HighLevelAPI.MechanismParams;

namespace KeyConnector.Tests.Services;

internal delegate void InitializationModifier(
    ICertificateProviderService certificateProviderService,
    ICryptoFunctionService cryptoFunctionService,
    IPkcs11InteropFactory pkcs11InteropFactory,
    KeyConnectorSettings keyConnectorSettings
);

public class Pkcs11RsaKeyServiceTests
{
    private readonly byte[] _data = "data"u8.ToArray();
    private readonly byte[] _encryptedData = "encryptedData"u8.ToArray();
    private readonly byte[] _decryptedData = "decryptedData"u8.ToArray();
    private readonly byte[] _signature = "signature"u8.ToArray();

    private static Pkcs11RsaKeyService InitializeService(InitializationModifier modifier = null)
    {
        var certificateProviderService = Substitute.For<ICertificateProviderService>();
        var cryptoFunctionService = Substitute.For<ICryptoFunctionService>();
        var pkcs11InteropFactory = Substitute.For<IPkcs11InteropFactory>();
        var settings = new KeyConnectorSettings();
        modifier?.Invoke(certificateProviderService, cryptoFunctionService, pkcs11InteropFactory, settings);

        return new Pkcs11RsaKeyService(certificateProviderService, cryptoFunctionService, pkcs11InteropFactory,
            settings);
    }
    
    // EncryptAsync Tests

    [Fact]
    public async Task EncryptAsync_ReturnsEncryptedData()
    {
        // Create certificate
        var cert = CreateCertificate();
        var publicKey = cert.GetRSAPublicKey()!.ExportSubjectPublicKeyInfo();

        // Initialize sut
        var sut = InitializeService((certProvider, cryptoService, _, _) =>
        {
            certProvider.GetCertificateAsync().Returns(cert);
            cryptoService.RsaEncryptAsync(
                    _data,
                    Arg.Is<byte[]>(input => publicKey.SequenceEqual(input)))
                .Returns(_encryptedData);
        });

        // Act
        var result = await sut.EncryptAsync(_data);

        // Assert
        Assert.Equal("encryptedData"u8.ToArray(), result);
    }

    [Fact]
    public async Task EncryptAsync_ReturnsNull_GivenNullData()
    {
        var sut = InitializeService();
        Assert.Null(await sut.EncryptAsync(null));
    }
    
    // DecryptAsync Tests

    [Fact]
    public async Task DecryptAsync_ReturnsDecryptedData()
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();
        var slot = library.AddSlot("chosenSerialNumber");
        var session = slot.AddSession();
        var privateKey = session.AddPrivateKey();

        var mechanismParams = Substitute.For<ICkRsaPkcsOaepParams>();
        var mechanism = Substitute.For<IMechanism>();


        session.Decrypt(mechanism, privateKey, _data).Returns(_decryptedData);

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings
            {
                Pkcs11Provider = "yubihsm",
                Pkcs11SlotTokenSerialNumber = "chosenSerialNumber",
                Pkcs11LoginUserType = "userType",
                Pkcs11LoginPin = "loginPin"
            };
            interopFactory.LoadPkcs11Library(default, default).ReturnsForAnyArgs(library);
            interopFactory.CreateCkRsaPkcsOaepParams(default, default, default, default)
                .ReturnsForAnyArgs(mechanismParams);
            interopFactory.CreateMechanism(Arg.Any<CKM>(), mechanismParams).Returns(mechanism);
        });

        // Act
        var result = await sut.DecryptAsync(_data);

        // Assert
        Assert.Equal("decryptedData"u8.ToArray(), result);
    }

    [Fact]
    public async Task DecryptAsync_LogsOutOfSession_GivenValidData()
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();
        var slot = library.AddSlot("chosenSerialNumber");
        var session = slot.AddSession();
        var privateKey = session.AddPrivateKey();

        var mechanismParams = Substitute.For<ICkRsaPkcsOaepParams>();
        var mechanism = Substitute.For<IMechanism>();

        session.Decrypt(mechanism, privateKey, _data).Returns(_decryptedData);

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings
            {
                Pkcs11Provider = "yubihsm",
                Pkcs11SlotTokenSerialNumber = "chosenSerialNumber",
                Pkcs11LoginUserType = "userType",
                Pkcs11LoginPin = "loginPin"
            };
            interopFactory.LoadPkcs11Library(default, default).ReturnsForAnyArgs(library);
            interopFactory.CreateCkRsaPkcsOaepParams(default, default, default, default)
                .ReturnsForAnyArgs(mechanismParams);
            interopFactory.CreateMechanism(Arg.Any<CKM>(), mechanismParams).Returns(mechanism);
        });

        // Act
        await sut.DecryptAsync(_data);

        // Assert
        session.Received().Logout();
    }

    [Theory]
    [InlineData("yubihsm", "/usr/lib/x86_64-linux-gnu/pkcs11/yubihsm_pkcs11.so")]
    [InlineData("opensc", "/usr/lib/x86_64-linux-gnu/opensc-pkcs11.so")]
    [InlineData("futurex", "/usr/lib/x86_64-linux-gnu/fxpkcs11/libfxpkcs11.so")]
    public async Task DecryptAsync_UsesCorrectProviderPath_WhenLoadingLibrary(string provider, string path)
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();
        IPkcs11InteropFactory pkcs11InteropFactory = null;

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings { Pkcs11Provider = provider };
            interopFactory.LoadPkcs11Library(path, Arg.Any<AppType>()).Returns(library);
            pkcs11InteropFactory = interopFactory;
        });

        // Act
        try
        {
            await sut.DecryptAsync(_data);
        }
        catch
        {
        }

        // Assert
        pkcs11InteropFactory.Received().LoadPkcs11Library(path, Arg.Any<AppType>());
    }

    [Fact]
    public async Task DecryptAsync_ThrowsException_WhenSlotHasNoTokenPresent()
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();
        library.AddSlot("chosenSerialNumber", false);

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings
            {
                Pkcs11Provider = "yubihsm", Pkcs11SlotTokenSerialNumber = "chosenSerialNumber",
            };
            interopFactory.LoadPkcs11Library(default, default).ReturnsForAnyArgs(library);
        });

        // Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () => await sut.DecryptAsync(_data));
        Assert.Contains("Cannot locate token slot.", exception.Message);
    }

    [Fact]
    public async Task DecryptAsync_ChoosesCorrectSlot_GivenASlotSerialNumber()
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();

        var slot1 = Substitute.For<ISlot>();
        slot1.GetSlotInfo().SlotFlags.TokenPresent.Returns(true);
        slot1.GetTokenInfo().SerialNumber.Returns("chosenSerialNumber");
        var slot2 = Substitute.For<ISlot>();
        slot2.GetSlotInfo().SlotFlags.TokenPresent.Returns(true);
        slot2.GetTokenInfo().SerialNumber.Returns("wrongSerialNumber");
        library.GetSlotList(default).ReturnsForAnyArgs([slot1, slot2]);

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings
            {
                Pkcs11Provider = "yubihsm", Pkcs11SlotTokenSerialNumber = "chosenSerialNumber",
            };
            interopFactory.LoadPkcs11Library(default, default).ReturnsForAnyArgs(library);
        });

        // Act
        try
        {
            await sut.DecryptAsync(_data);
        }
        catch
        {
        }

        // Assert
        slot1.Received().OpenSession(Arg.Any<SessionType>());
    }

    [Theory]
    [InlineData(null, CKU.CKU_USER)]
    [InlineData("so", CKU.CKU_SO)]
    [InlineData("context_specific", CKU.CKU_CONTEXT_SPECIFIC)]
    public async Task DecryptAsync_UsesCorrectUserType_GivenInSettings(string userTypeSetting, CKU expectedUserType)
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();
        var slot = library.AddSlot("chosenSerialNumber");
        var session = slot.AddSession();

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings
            {
                Pkcs11Provider = "yubihsm",
                Pkcs11SlotTokenSerialNumber = "chosenSerialNumber",
                Pkcs11LoginUserType = userTypeSetting,
                Pkcs11LoginPin = "loginPin"
            };
            interopFactory.LoadPkcs11Library(default, default).ReturnsForAnyArgs(library);
        });

        // Act
        try
        {
            await sut.DecryptAsync(_data);
        }
        catch
        {
        }

        // Assert
        session.Received().Login(expectedUserType, "loginPin");
    }

    [Theory]
    [InlineData(123UL, CKA.CKA_ID)]
    [InlineData(null, CKA.CKA_LABEL)]
    public async Task DecryptAsync_UsesPrivateKeyId_IfAvailableInSettings(ulong? id, CKA expectedType)
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();
        var slot = library.AddSlot("chosenSerialNumber");
        var session = slot.AddSession();
        session.AddPrivateKey();

        IPkcs11InteropFactory pkcs11InteropFactory = null;

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings
            {
                Pkcs11Provider = "yubihsm",
                Pkcs11SlotTokenSerialNumber = "chosenSerialNumber",
                Pkcs11LoginUserType = "userType",
                Pkcs11LoginPin = "loginPin",
                Pkcs11PrivateKeyId = id,
                Pkcs11PrivateKeyLabel = "label"
            };
            interopFactory.LoadPkcs11Library(default, default).ReturnsForAnyArgs(library);
            pkcs11InteropFactory = interopFactory;
        });

        // Act
        await sut.DecryptAsync(_data);

        // Assert
        if (id is not null)
        {
            pkcs11InteropFactory.Received().CreateObjectAttribute(expectedType, id.Value);
        }
        else
        {
            pkcs11InteropFactory.Received().CreateObjectAttribute(expectedType, "label");
        }
    }

    [Fact]
    public async Task DecryptAsync_ReturnsNull_GivenNullData()
    {
        var sut = InitializeService();
        Assert.Null(await sut.DecryptAsync(null));
    }
    
    // SignAsync Tests

    [Fact]
    public async Task SignAsync_ReturnsSignature()
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();
        var slot = library.AddSlot("chosenSerialNumber");
        var session = slot.AddSession();
        var privateKey = session.AddPrivateKey();
        var mechanism = Substitute.For<IMechanism>();

        session.Sign(mechanism, privateKey, _data).Returns(_signature);

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings
            {
                Pkcs11Provider = "yubihsm",
                Pkcs11SlotTokenSerialNumber = "chosenSerialNumber",
                Pkcs11LoginUserType = "userType",
                Pkcs11LoginPin = "loginPin"
            };
            interopFactory.LoadPkcs11Library(default, default).ReturnsForAnyArgs(library);
            interopFactory.CreateMechanism(Arg.Any<CKM>()).Returns(mechanism);
        });

        // Act
        var result = await sut.SignAsync(_data);

        // Assert
        Assert.Equal("signature"u8.ToArray(), result);
    }

    [Fact]
    public async Task SignAsync_LogsOutOfSession_GivenValidData()
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();
        var slot = library.AddSlot("chosenSerialNumber");
        var session = slot.AddSession();
        var privateKey = session.AddPrivateKey();
        var mechanism = Substitute.For<IMechanism>();

        session.Sign(mechanism, privateKey, _data).Returns(_signature);

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings
            {
                Pkcs11Provider = "yubihsm",
                Pkcs11SlotTokenSerialNumber = "chosenSerialNumber",
                Pkcs11LoginUserType = "userType",
                Pkcs11LoginPin = "loginPin"
            };

            interopFactory.LoadPkcs11Library(default, default).ReturnsForAnyArgs(library);
            interopFactory.CreateMechanism(Arg.Any<CKM>()).Returns(mechanism);
        });

        // Act
        await sut.SignAsync(_data);

        // Assert
        session.Received().Logout();
    }

    [Theory]
    [InlineData("yubihsm", "/usr/lib/x86_64-linux-gnu/pkcs11/yubihsm_pkcs11.so")]
    [InlineData("opensc", "/usr/lib/x86_64-linux-gnu/opensc-pkcs11.so")]
    [InlineData("futurex", "/usr/lib/x86_64-linux-gnu/fxpkcs11/libfxpkcs11.so")]
    public async Task SignAsync_UsesCorrectProviderPath_WhenLoadingLibrary(string provider, string path)
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();
        IPkcs11InteropFactory pkcs11InteropFactory = null;

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings { Pkcs11Provider = provider };
            interopFactory.LoadPkcs11Library(path, Arg.Any<AppType>()).Returns(library);
            pkcs11InteropFactory = interopFactory;
        });

        // Act
        try
        {
            await sut.SignAsync(_data);
        }
        catch
        {
        }

        // Assert
        pkcs11InteropFactory.Received().LoadPkcs11Library(path, Arg.Any<AppType>());
    }

    [Fact]
    public async Task SignAsync_ThrowsException_WhenSlotHasNoTokenPresent()
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();
        library.AddSlot("chosenSerialNumber", false);

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings
            {
                Pkcs11Provider = "yubihsm", Pkcs11SlotTokenSerialNumber = "chosenSerialNumber",
            };
            interopFactory.LoadPkcs11Library(default, default).ReturnsForAnyArgs(library);
        });

        // Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () => await sut.SignAsync(_data));
        Assert.Contains("Cannot locate token slot.", exception.Message);
    }

    [Fact]
    public async Task SignAsync_ChoosesCorrectSlot_GivenASlotSerialNumber()
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();

        var slot1 = Substitute.For<ISlot>();
        slot1.GetSlotInfo().SlotFlags.TokenPresent.Returns(true);
        slot1.GetTokenInfo().SerialNumber.Returns("chosenSerialNumber");
        var slot2 = Substitute.For<ISlot>();
        slot2.GetSlotInfo().SlotFlags.TokenPresent.Returns(true);
        slot2.GetTokenInfo().SerialNumber.Returns("wrongSerialNumber");
        library.GetSlotList(default).ReturnsForAnyArgs([slot1, slot2]);

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings
            {
                Pkcs11Provider = "yubihsm", Pkcs11SlotTokenSerialNumber = "chosenSerialNumber",
            };
            interopFactory.LoadPkcs11Library(default, default).ReturnsForAnyArgs(library);
        });

        // Act
        try
        {
            await sut.SignAsync(_data);
        }
        catch
        {
        }

        // Assert
        slot1.Received().OpenSession(Arg.Any<SessionType>());
    }

    [Theory]
    [InlineData(123UL, CKA.CKA_ID)]
    [InlineData(null, CKA.CKA_LABEL)]
    public async Task SignAsync_UsesPrivateKeyId_IfAvailableInSettings(ulong? id, CKA expectedType)
    {
        // Create mocks
        var library = Substitute.For<IPkcs11Library>();
        var slot = library.AddSlot("chosenSerialNumber");
        var session = slot.AddSession();
        session.AddPrivateKey();

        IPkcs11InteropFactory pkcs11InteropFactory = null;

        // Initialize sut
        var sut = InitializeService((_, _, interopFactory, settings) =>
        {
            settings.RsaKey = new KeyConnectorSettings.RsaKeySettings
            {
                Pkcs11Provider = "yubihsm",
                Pkcs11SlotTokenSerialNumber = "chosenSerialNumber",
                Pkcs11LoginUserType = "userType",
                Pkcs11LoginPin = "loginPin",
                Pkcs11PrivateKeyId = id,
                Pkcs11PrivateKeyLabel = "label"
            };
            interopFactory.LoadPkcs11Library(default, default).ReturnsForAnyArgs(library);
            pkcs11InteropFactory = interopFactory;
        });

        // Act
        await sut.SignAsync(_data);

        // Assert
        if (id is not null)
        {
            pkcs11InteropFactory.Received().CreateObjectAttribute(expectedType, id.Value);
        }
        else
        {
            pkcs11InteropFactory.Received().CreateObjectAttribute(expectedType, "label");
        }
    }

    [Fact]
    public async Task SignAsync_ReturnsNull_GivenNullData()
    {
        var sut = InitializeService();
        Assert.Null(await sut.SignAsync(null));
    }

    private static X509Certificate2 CreateCertificate()
    {
        var rsa = RSA.Create(2048);
        var req = new CertificateRequest("CN=Experimental Issuing Authority", rsa, HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var cert = req.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
        return cert;
    }
}

internal static class Extensions
{
    public static ISlot AddSlot(this IPkcs11Library library, string serialNumber, bool tokenPresent = true)
    {
        var slot = Substitute.For<ISlot>();
        slot.GetSlotInfo().SlotFlags.TokenPresent.Returns(tokenPresent);
        slot.GetTokenInfo().SerialNumber.Returns(serialNumber);
        library.GetSlotList(default).ReturnsForAnyArgs([slot]);
        return slot;
    }

    public static ISession AddSession(this ISlot slot)
    {
        var session = Substitute.For<ISession>();
        slot.OpenSession(default).ReturnsForAnyArgs(session);
        return session;
    }

    public static IObjectHandle AddPrivateKey(this ISession session)
    {
        var privateKey = Substitute.For<IObjectHandle>();
        session.FindAllObjects(default).ReturnsForAnyArgs([privateKey]);
        return privateKey;
    }
}
