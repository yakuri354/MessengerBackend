using System.IO;
using System.Security.Cryptography;
using JWT.Algorithms;
using JWT.Builder;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace MessengerBackend.Services
{
    public class CryptoService
    {
        public readonly SHA256 Sha256;
        private readonly IConfiguration _configuration;
        
        public readonly JwtBuilder JwtBuilder;

        public readonly RSACryptoServiceProvider PublicKey;
        public readonly RSACryptoServiceProvider PrivateKey;

        public CryptoService(IConfiguration configuration)
        {
            _configuration = configuration;
            Sha256 = SHA256.Create();
            using (var fs = File.OpenText(_configuration["Secrets:JWT:RSAPublicKeyPath"]))
            {
                var pemReader = new PemReader(fs);
                var keyPair = (AsymmetricCipherKeyPair) pemReader.ReadObject();
                PublicKey = new RSACryptoServiceProvider();
                PublicKey.ImportParameters(
                    DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters) keyPair.Private));
            }

            using (var fs = File.OpenText(_configuration["Secrets:JWT:RSAPrivateKeyPath"]))
            {
                var pemReader = new PemReader(fs);
                var keyPair = (AsymmetricCipherKeyPair) pemReader.ReadObject();
                PrivateKey = new RSACryptoServiceProvider();
                PrivateKey.ImportParameters(
                    DotNetUtilities.ToRSAParameters((RsaPrivateCrtKeyParameters) keyPair.Private));
            }
            JwtBuilder = new JwtBuilder()
                .WithAlgorithm(new RS256Algorithm(PublicKey, PrivateKey))
                .Issuer(AuthService.AuthOptions.ISSUER)
                .Audience(AuthService.AuthOptions.AUDIENCE);
        }
    }
}