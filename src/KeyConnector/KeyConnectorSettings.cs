namespace Bit.KeyConnector
{
    public class KeyConnectorSettings
    {
        public string WebVaultUri { get; set; }
        public string IdentityServerUri { get; set; }

        public DatabaseSettings Database { get; set; }
        public CertificateSettings Certificate { get; set; }
        public RsaKeySettings RsaKey { get; set; }

        public class CertificateSettings
        {
            public string Provider { get; set; }
            // filesystem
            public string FilesystemPath { get; set; }
            public string FilesystemPassword { get; set; }
            // store
            public string StoreThumbprint { get; set; }
            // azurestorage
            public string AzureStorageConnectionString { get; set; }
            public string AzureStorageContainer { get; set; }
            public string AzureStorageFileName { get; set; }
            public string AzureStorageFilePassword { get; set; }
            // azurekv
            public string AzureKeyvaultUri { get; set; }
            public string AzureKeyvaultCertificateName { get; set; }
            public string AzureKeyvaultAdTenantId { get; set; }
            public string AzureKeyvaultAdAppId { get; set; }
            public string AzureKeyvaultAdSecret { get; set; }
            // vault
            public string VaultServerUri { get; set; }
            public string VaultToken { get; set; }
            public string VaultSecretMountPoint { get; set; }
            public string VaultSecretPath { get; set; }
            public string VaultSecretDataKey { get; set; }
            public string VaultSecretFilePassword { get; set; }
        }

        public class RsaKeySettings
        {
            public string Provider { get; set; }
            // azurekv
            public string AzureKeyvaultUri { get; set; }
            public string AzureKeyvaultKeyName { get; set; }
            public string AzureKeyvaultAdTenantId { get; set; }
            public string AzureKeyvaultAdAppId { get; set; }
            public string AzureKeyvaultAdSecret { get; set; }
            // gcpkms
            public string GoogleCloudProjectId { get; set; }
            public string GoogleCloudLocationId { get; set; }
            public string GoogleCloudKeyringId { get; set; }
            public string GoogleCloudKeyId { get; set; }
            public string GoogleCloudKeyVersionId { get; set; }
            // awskms
            public string AwsAccessKeyId { get; set; }
            public string AwsAccessKeySecret { get; set; }
            public string AwsRegion { get; set; }
            public string AwsKeyId { get; set; }
            public bool AwsUseSymmetricEncryption { get; set; }
            // pkcs11
            //      Providers:
            //      yubihsm
            //      opensc
            public string Pkcs11Provider { get; set; }
            public string Pkcs11LibraryPath { get; set; }
            public string Pkcs11SlotTokenSerialNumber { get; set; }
            public string Pkcs11LoginUserType { get; set; }
            public string Pkcs11LoginPin { get; set; }
            public string Pkcs11PrivateKeyLabel { get; set; }
            public ulong? Pkcs11PrivateKeyId { get; set; }
        }

        public class DatabaseSettings
        {
            public string Provider { get; set; }
            // json
            public string JsonFilePath { get; set; }
            // sqlserver
            public string SqlServerConnectionString { get; set; }
            // postgresql
            public string PostgreSqlConnectionString { get; set; }
            // mysql
            public string MySqlConnectionString { get; set; }
            // sqlite
            public string SqliteConnectionString { get; set; }
            // mongo
            public string MongoConnectionString { get; set; }
            public string MongoDatabaseName { get; set; }
        }
    }
}
