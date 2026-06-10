using System;
using System.IO;

namespace KeyConnector.Tests.Helpers;

internal static class TestCertificateData
{
    internal const string Password = "password123";
    internal const string Thumbprint = "C196A26CB6BDC12A867ECF79C2C5EE8D657D550C";
    internal const string NoPasswordThumbprint = "FA3F9D014B166EBC0B1123432A8621ABB07F391A";

    internal static string PfxFilePath =>
        Path.Combine(AppContext.BaseDirectory, "Resources", "test-cert.pfx");

    internal static string NoPasswordPfxFilePath =>
        Path.Combine(AppContext.BaseDirectory, "Resources", "test-cert-no-password.pfx");

    internal static byte[] PfxBytes => File.ReadAllBytes(PfxFilePath);
    internal static byte[] NoPasswordPfxBytes => File.ReadAllBytes(NoPasswordPfxFilePath);
}
