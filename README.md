<a href="https://hub.docker.com/r/bitwarden/crypto-agent" target="_blank">
  <img src="https://img.shields.io/docker/pulls/bitwarden/crypto-agent.svg" alt="DockerHub" />
</a>

# Bitwarden Crypto Agent

The Bitwarden Crypto Agent is a self-hosted web application that stores and provides cryptographic keys to Bitwarden
clients.

The Crypto Agent project is written in C# using .NET Core with ASP.NET Core. The codebase can be developed, built, run,
and deployed cross-platform on Windows, macOS, and Linux distributions.

## Deploy

The Bitwarden Crypto Agent can be deployed using the pre-built docker container available on
[DockerHub](https://hub.docker.com/r/bitwarden/crypto-agent).

## Configuration

A variety of configuration options are available for the Bitwarden Crypto Agent.

### Bitwarden Server

By default, the Bitwarden server configuration points to the Bitwarden Cloud endpoints. If you are using a
self-hosted Bitwarden installation, you will need to configure the web vault and identity server endpoints.

```
cryptoAgentSettings__webVaultUri=https://bitwarden.company.com
cryptoAgentSettings__identityServerUri=https://bitwarden.company.com/identity/
```

### Database

A database persists encrypted keys for your users. The following databases are supported to be configured. Migrating
from one database provider to another is not supported at this time.

**JSON File (default)**

```
cryptoAgentSettings__database__provider=json
cryptoAgentSettings__database__jsonFilePath=/etc/bitwarden/data.json
```

**Microsoft SQL Server**

```
cryptoAgentSettings__database__provider=sqlserver
cryptoAgentSettings__database__sqlServerConnectionString={ConnectionString}
```

**PostgreSQL**

```
cryptoAgentSettings__database__provider=postgresql
cryptoAgentSettings__database__postgreSqlConnectionString={ConnectionString}
```

**MySQL/MariaDB**

```
cryptoAgentSettings__database__provider=mysql
cryptoAgentSettings__database__mySqlConnectionString={ConnectionString}
```

**SQLite**

```
cryptoAgentSettings__database__provider=sqlite
cryptoAgentSettings__database__sqliteConnectionString={ConnectionString}
```

**MongoDB**

```
cryptoAgentSettings__database__provider=mongo
cryptoAgentSettings__database__mongoConnectionString={ConnectionString}
cryptoAgentSettings__database__mongoDatabaseName={DatabaseName}
```

### RSA Key

The Bitwarden Crypto Agent uses a RSA key pair to protect user keys at rest. The RSA key pair should be a minimum of
2048 bits in length.

You must configure how the Bitwarden Crypto Agent accesses and utilizes your RSA key pair.

**Certificate (default)**

An X509 certificate that contains the RSA key pair.

```
cryptoAgentSettings__rsaKey__provider=certificate
```

*See additional certificate configuration options below.*

**Azure Key Vault**

You will need to create an Azure Active Directory application that has access to read from the associated Key Vault.

```
cryptoAgentSettings__rsaKey__provider=azurekv
cryptoAgentSettings__rsaKey__azureKeyvaultUri={URI}
cryptoAgentSettings__rsaKey__azureKeyvaultKeyName={KeyName}
cryptoAgentSettings__rsaKey__azureKeyvaultAdTenantId={ActiveDirectoryTenantId}
cryptoAgentSettings__rsaKey__azureKeyvaultAdAppId={ActiveDirectoryAppId}
cryptoAgentSettings__rsaKey__azureKeyvaultAdSecret={ActiveDirectorySecret}
```

**Google Cloud Key Management**

```
cryptoAgentSettings__rsaKey__provider=gcpkms
cryptoAgentSettings__rsaKey__googleCloudProjectId={ProjectId}
cryptoAgentSettings__rsaKey__googleCloudLocationId={LocationId}
cryptoAgentSettings__rsaKey__googleCloudKeyringId={KeyringId}
cryptoAgentSettings__rsaKey__googleCloudKeyId={KeyId}
cryptoAgentSettings__rsaKey__googleCloudKeyVersionId={KeyVersionId}
```

**AWS Key Management Service**

```
cryptoAgentSettings__rsaKey__provider=awskms
cryptoAgentSettings__rsaKey__awsAccessKeyId={AccessKeyId}
cryptoAgentSettings__rsaKey__awsAccessKeySecret={AccessKeySecret}
cryptoAgentSettings__rsaKey__awsRegion={RegionName}
cryptoAgentSettings__rsaKey__awsKeyId={KeyId}
```

**PKCS11**

Use a physical HSM device with the PKCS11 provider.

```
cryptoAgentSettings__rsaKey__provider=pkcs11
# Available providers: yubihsm, opensc
cryptoAgentSettings__rsaKey__pkcs11Provider={Provider}
cryptoAgentSettings__rsaKey__pkcs11SlotTokenSerialNumber={TokenSerialNumber}
# Available user types: user, so, context_specific
cryptoAgentSettings__rsaKey__pkcs11LoginUserType={LoginUserType}
cryptoAgentSettings__rsaKey__pkcs11LoginPin={LoginPIN}

# Locate the private key on the device via label *or* ID.
cryptoAgentSettings__rsaKey__pkcs11PrivateKeyLabel={PrivateKeyLabel}
cryptoAgentSettings__rsaKey__pkcs11PrivateKeyId={PrivateKeyId}
```

*When using the PKCS11 provider to store your private key on an HSM device, the associated public key must be made
available and configured as a certificate (see below).*

### Certificate

The RSA key pair can be provided via certificate configuration. The certificate should be made available as a PKCS12
`.pfx` file. Example:

```
openssl req -x509 -newkey rsa:4096 -sha256 -nodes -keyout bwagent.key
  -out bwagent.crt -subj "/CN=Bitwarden Agent" -days 36500

openssl pkcs12 -export -out ./bwagent.pfx -inkey bwagent.key
  -in bwagent.crt -passout pass:{Password}
```

If using the PKCS11 RSA key provider, you will need to make a public key PKCS12 certificate available.

**Filesystem**

```
cryptoAgentSettings__certificate__provider=filesystem
cryptoAgentSettings__certificate__filesystemPath={Path}
cryptoAgentSettings__certificate__filesystemPassword={Password}
```

**OS Certificate Store**

```
cryptoAgentSettings__certificate__provider=store
cryptoAgentSettings__certificate__storeThumbprint={Thumbprint}
```

**Azure Blob Storage**

```
cryptoAgentSettings__certificate__provider=azurestorage
cryptoAgentSettings__certificate__azureStorageConnectionString={ConnectionString}
cryptoAgentSettings__certificate__azureStorageContainer={Container}
cryptoAgentSettings__certificate__azureStorageFileName={FileName}
cryptoAgentSettings__certificate__azureStorageFilePassword={FilePassword}
```

**Azure Key Vault**

You will need to create an Azure Active Directory application that has access to read from the associated Key Vault.

```
cryptoAgentSettings__certificate__provider=azurekv
cryptoAgentSettings__certificate__azureKeyvaultUri={URI}
cryptoAgentSettings__certificate__azureKeyvaultCertificateName={CertificateName}
cryptoAgentSettings__certificate__azureKeyvaultAdTenantId={ActiveDirectoryTenantId}
cryptoAgentSettings__certificate__azureKeyvaultAdAppId={ActiveDirectoryAppId}
cryptoAgentSettings__certificate__azureKeyvaultAdSecret={ActiveDirectorySecret}
```

**HashiCorp Vault**

```
cryptoAgentSettings__certificate__provider=vault
cryptoAgentSettings__certificate__vaultServerUri={ServerURI}
cryptoAgentSettings__certificate__vaultToken={Token}
cryptoAgentSettings__certificate__vaultSecretMountPoint={SecretMountPoint}
cryptoAgentSettings__certificate__vaultSecretPath={SecretPath}
cryptoAgentSettings__certificate__vaultSecretDataKey={SecretDataKey}
cryptoAgentSettings__certificate__vaultSecretFilePassword={SecretFilePassword}
```

## Build/Run

### Requirements

- [.NET Core 5.0 SDK](https://www.microsoft.com/net/download/core)

*These dependencies are free to use.*

### Recommended Development Tooling

- [Visual Studio](https://www.visualstudio.com/vs/) (Windows and macOS)
- [Visual Studio Code](https://code.visualstudio.com/) (other)

*These tools are free to use.*
