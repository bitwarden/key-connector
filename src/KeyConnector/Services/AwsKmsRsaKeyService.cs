using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Amazon;
using Amazon.KeyManagementService;
using Amazon.KeyManagementService.Model;
using Bit.KeyConnector.Exceptions;

namespace Bit.KeyConnector.Services
{
    public class AwsKmsRsaKeyService : IRsaKeyService
    {
        private readonly KeyConnectorSettings _settings;
        private readonly AmazonKeyManagementServiceClient _kmsClient;

        public AwsKmsRsaKeyService(
            KeyConnectorSettings settings)
        {
            _settings = settings;
            if(UseInstanceMetadataForCredentials())
            {
                _kmsClient = new AmazonKeyManagementServiceClient(RegionEndpoint.GetBySystemName(settings.RsaKey.AwsRegion));
            } else {
                _kmsClient = new AmazonKeyManagementServiceClient(settings.RsaKey.AwsAccessKeyId, settings.RsaKey.AwsAccessKeySecret, RegionEndpoint.GetBySystemName(settings.RsaKey.AwsRegion));
            }
        }

        /// <summary>
        /// AWS will default to use the instance metadata for credentials if we initialize the client without credentials, per their documentation here: https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/creds-assign.html.
        /// We will infer that we should use the instance metadata if the AwsAccessKeyId and AwsAccessKeySecret are not set in the Key Connector configuration.
        /// </summary>
        private bool UseInstanceMetadataForCredentials()
        {
            return string.IsNullOrWhiteSpace(_settings.RsaKey.AwsAccessKeyId) && string.IsNullOrWhiteSpace(_settings.RsaKey.AwsAccessKeySecret);
        }

        public async Task<byte[]> EncryptAsync(byte[] data)
        {
            using var dataStream = new MemoryStream(data);
            var request = new EncryptRequest
            {
                KeyId = _settings.RsaKey.AwsKeyId,
                Plaintext = dataStream,
                EncryptionAlgorithm = _settings.RsaKey.AwsUseSymmetricEncryption
                    ? EncryptionAlgorithmSpec.SYMMETRIC_DEFAULT
                    : EncryptionAlgorithmSpec.RSAES_OAEP_SHA_256
            };
            var response = await _kmsClient.EncryptAsync(request);
            return response.CiphertextBlob.ToArray();
        }

        public async Task<byte[]> DecryptAsync(byte[] data)
        {
            using var dataStream = new MemoryStream(data);
            var request = new DecryptRequest
            {
                KeyId = _settings.RsaKey.AwsKeyId,
                CiphertextBlob = dataStream,
                EncryptionAlgorithm = _settings.RsaKey.AwsUseSymmetricEncryption
                    ? EncryptionAlgorithmSpec.SYMMETRIC_DEFAULT
                    : EncryptionAlgorithmSpec.RSAES_OAEP_SHA_256
            };
            var response = await _kmsClient.DecryptAsync(request);
            return response.Plaintext.ToArray();
        }

        public async Task<byte[]> SignAsync(byte[] data)
        {
            if (_settings.RsaKey.AwsUseSymmetricEncryption)
            {
                throw new InvalidKeyTypeException("Cannot sign using symmetric key");
            }
            using var dataStream = new MemoryStream(data);
            var request = new SignRequest
            {
                KeyId = _settings.RsaKey.AwsKeyId,
                SigningAlgorithm = SigningAlgorithmSpec.RSASSA_PKCS1_V1_5_SHA_256,
                Message = dataStream,
                MessageType = MessageType.RAW
            };
            var response = await _kmsClient.SignAsync(request);
            return response.Signature.ToArray();
        }

        public async Task<bool> VerifyAsync(byte[] data, byte[] signature)
        {
            if (_settings.RsaKey.AwsUseSymmetricEncryption)
            {
                throw new InvalidKeyTypeException("Cannot sign using symmetric key");
            }
            using var dataStream = new MemoryStream(data);
            using var signatureStream = new MemoryStream(data);
            var request = new VerifyRequest
            {
                KeyId = _settings.RsaKey.AwsKeyId,
                SigningAlgorithm = SigningAlgorithmSpec.RSASSA_PKCS1_V1_5_SHA_256,
                Message = dataStream,
                MessageType = MessageType.RAW,
                Signature = signatureStream
            };
            var response = await _kmsClient.VerifyAsync(request);
            return response.SignatureValid;
        }

        public async Task<byte[]> GetPublicKeyAsync()
        {
            if (_settings.RsaKey.AwsUseSymmetricEncryption)
            {
                throw new InvalidKeyTypeException("Cannot retrieve public key as symmetric keys do not have public keys");
            }
            var request = new GetPublicKeyRequest
            {
                KeyId = _settings.RsaKey.AwsKeyId
            };
            var response = await _kmsClient.GetPublicKeyAsync(request);
            var rsa = RSA.Create();
            rsa.ImportSubjectPublicKeyInfo(response.PublicKey.ToArray(), out _);
            return rsa.ExportSubjectPublicKeyInfo();
        }
    }
}
