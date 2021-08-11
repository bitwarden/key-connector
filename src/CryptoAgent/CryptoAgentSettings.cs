namespace Bit.CryptoAgent
{
    public class CryptoAgentSettings
    {
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
            // vault...
            // Other HSMs...
        }

        public class DatabaseSettings
        {
            public string JsonFilePath { get; set; }
            public string SqlServerConnectionString { get; set; }
            public string MySqlConnectionString { get; set; }
            public string PostgreSqlConnectionString { get; set; }
        }
    }
}
