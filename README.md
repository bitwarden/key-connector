<a href="https://hub.docker.com/r/bitwarden/key-connector" target="_blank">
  <img src="https://img.shields.io/docker/pulls/bitwarden/key-connector.svg" alt="DockerHub" />
</a>

# Bitwarden Key Connector

The Bitwarden Key Connector is a self-hosted web application that stores and provides cryptographic keys to Bitwarden
clients.

The Key Connector project is written in C# using .NET Core with ASP.NET Core. The codebase can be developed, built, run,
and deployed cross-platform on Windows, macOS, and Linux distributions.

## Deploy

The Bitwarden Key Connector can be deployed using the pre-built docker container available on
[DockerHub](https://hub.docker.com/r/bitwarden/key-connector).

## Configuration

A variety of configuration options are available for the Bitwarden Key Connector.

### Bitwarden Server

By default, the Bitwarden server configuration points to the Bitwarden Cloud endpoints. If you are using a
self-hosted Bitwarden installation, you will need to configure the web vault and identity server endpoints.

```
keyConnectorSettings__webVaultUri=https://bitwarden.company.com
keyConnectorSettings__identityServerUri=https://bitwarden.company.com/identity/
```

### Database

A database persists encrypted keys for your users. The following databases are supported to be configured. Migrating
from one database provider to another is not supported at this time.

**JSON File (default)**

```
keyConnectorSettings__database__provider=json
keyConnectorSettings__database__jsonFilePath={FilePath}
```

By default, the application stores the JSON file at the follow path: `/etc/bitwarden/data.json`.

**Microsoft SQL Server**

```
keyConnectorSettings__database__provider=sqlserver
keyConnectorSettings__database__sqlServerConnectionString={ConnectionString}
```

**PostgreSQL**

```
keyConnectorSettings__database__provider=postgresql
keyConnectorSettings__database__postgreSqlConnectionString={ConnectionString}
```

**MySQL/MariaDB**

```
keyConnectorSettings__database__provider=mysql
keyConnectorSettings__database__mySqlConnectionString={ConnectionString}
```

**SQLite**

```
keyConnectorSettings__database__provider=sqlite
keyConnectorSettings__database__sqliteConnectionString={ConnectionString}
```

**MongoDB**

```
keyConnectorSettings__database__provider=mongo
keyConnectorSettings__database__mongoConnectionString={ConnectionString}
keyConnectorSettings__database__mongoDatabaseName={DatabaseName}
```

### RSA Key

The Bitwarden Key Connector uses a RSA key pair to protect user keys at rest. The RSA key pair should be a minimum of
2048 bits in length.

You must configure how the Bitwarden Key Connector accesses and utilizes your RSA key pair.

**Certificate**

An X509 certificate that contains the RSA key pair.

```
keyConnectorSettings__rsaKey__provider=certificate
```

*See additional certificate configuration options below.*

**Azure Key Vault**

You will need to create an Azure Active Directory application that has access to read from the associated Key Vault.

```
keyConnectorSettings__rsaKey__provider=azurekv
keyConnectorSettings__rsaKey__azureKeyvaultUri={URI}
keyConnectorSettings__rsaKey__azureKeyvaultKeyName={KeyName}
keyConnectorSettings__rsaKey__azureKeyvaultAdTenantId={ActiveDirectoryTenantId}
keyConnectorSettings__rsaKey__azureKeyvaultAdAppId={ActiveDirectoryAppId}
keyConnectorSettings__rsaKey__azureKeyvaultAdSecret={ActiveDirectorySecret}
```

**Google Cloud Key Management**

```
keyConnectorSettings__rsaKey__provider=gcpkms
keyConnectorSettings__rsaKey__googleCloudProjectId={ProjectId}
keyConnectorSettings__rsaKey__googleCloudLocationId={LocationId}
keyConnectorSettings__rsaKey__googleCloudKeyringId={KeyringId}
keyConnectorSettings__rsaKey__googleCloudKeyId={KeyId}
keyConnectorSettings__rsaKey__googleCloudKeyVersionId={KeyVersionId}
```

**AWS Key Management Service**

```
keyConnectorSettings__rsaKey__provider=awskms
keyConnectorSettings__rsaKey__awsAccessKeyId={AccessKeyId}
keyConnectorSettings__rsaKey__awsAccessKeySecret={AccessKeySecret}
keyConnectorSettings__rsaKey__awsRegion={RegionName}
keyConnectorSettings__rsaKey__awsKeyId={KeyId}
```

**PKCS11**

Use a physical HSM device with the PKCS11 provider.

```
keyConnectorSettings__rsaKey__provider=pkcs11
# Available providers: yubihsm, opensc
keyConnectorSettings__rsaKey__pkcs11Provider={Provider}
keyConnectorSettings__rsaKey__pkcs11SlotTokenSerialNumber={TokenSerialNumber}
# Available user types: user, so, context_specific
keyConnectorSettings__rsaKey__pkcs11LoginUserType={LoginUserType}
keyConnectorSettings__rsaKey__pkcs11LoginPin={LoginPIN}

# Locate the private key on the device via label *or* ID.
keyConnectorSettings__rsaKey__pkcs11PrivateKeyLabel={PrivateKeyLabel}
keyConnectorSettings__rsaKey__pkcs11PrivateKeyId={PrivateKeyId}
```

*When using the PKCS11 provider to store your private key on an HSM device, the associated public key must be made
available and configured as a certificate (see below).*

### Certificate

The RSA key pair can be provided via certificate configuration. The certificate should be made available as a PKCS12
`.pfx` file. Example:

```
openssl req -x509 -newkey rsa:4096 -sha256 -nodes -keyout bwkc.key
  -out bwkc.crt -subj "/CN=Bitwarden Key Connector" -days 36500

openssl pkcs12 -export -out ./bwkc.pfx -inkey bwkc.key
  -in bwkc.crt -passout pass:{Password}
```

If using the PKCS11 RSA key provider, you will need to make a public key PKCS12 certificate available.

**Filesystem (default)**

```
keyConnectorSettings__certificate__provider=filesystem
keyConnectorSettings__certificate__filesystemPath={Path}
keyConnectorSettings__certificate__filesystemPassword={Password}
```

By default, the application looks for a certificate at the follow path: `/etc/bitwarden/key.pfx`.

**OS Certificate Store**

```
keyConnectorSettings__certificate__provider=store
keyConnectorSettings__certificate__storeThumbprint={Thumbprint}
```

**Azure Blob Storage**

```
keyConnectorSettings__certificate__provider=azurestorage
keyConnectorSettings__certificate__azureStorageConnectionString={ConnectionString}
keyConnectorSettings__certificate__azureStorageContainer={Container}
keyConnectorSettings__certificate__azureStorageFileName={FileName}
keyConnectorSettings__certificate__azureStorageFilePassword={FilePassword}
```

**Azure Key Vault**

You will need to create an Azure Active Directory application that has access to read from the associated Key Vault.

```
keyConnectorSettings__certificate__provider=azurekv
keyConnectorSettings__certificate__azureKeyvaultUri={URI}
keyConnectorSettings__certificate__azureKeyvaultCertificateName={CertificateName}
keyConnectorSettings__certificate__azureKeyvaultAdTenantId={ActiveDirectoryTenantId}
keyConnectorSettings__certificate__azureKeyvaultAdAppId={ActiveDirectoryAppId}
keyConnectorSettings__certificate__azureKeyvaultAdSecret={ActiveDirectorySecret}
```

**HashiCorp Vault**

```
keyConnectorSettings__certificate__provider=vault
keyConnectorSettings__certificate__vaultServerUri={ServerURI}
keyConnectorSettings__certificate__vaultToken={Token}
keyConnectorSettings__certificate__vaultSecretMountPoint={SecretMountPoint}
keyConnectorSettings__certificate__vaultSecretPath={SecretPath}
keyConnectorSettings__certificate__vaultSecretDataKey={SecretDataKey}
keyConnectorSettings__certificate__vaultSecretFilePassword={SecretFilePassword}
```

## Developer Documentation

Please refer to the [Key Connector section](https://contributing.bitwarden.com/getting-started/enterprise/key-connector) of the [Contributing Documentation](https://contributing.bitwarden.com/) for build instructions, recommended tooling, code style tips, and lots of other great information to get you started.
