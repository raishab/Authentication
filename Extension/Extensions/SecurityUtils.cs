using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography;
using Utility;

namespace Extension
{
    public static class SecurityUtils
    {
        public static AuthTokenOptions CreateAuthTokenOptions(IConfiguration configuration)
        {
            return new AuthTokenOptions
            {
                Audiences = configuration.GetSection(JwtSecurityOptions.Audiences).Get<List<string>>(),
                Issuer = configuration.GetValue<string>(JwtSecurityOptions.Issuer),
                SigningCredentials = new SigningCredentials(GetPrivateKey(configuration),
                SecurityAlgorithms.RsaSha256),
                RsaPublicSecurityKey = GetPublicKey(configuration)
            };
        }

        private static RsaSecurityKey GetPrivateKey(IConfiguration configuration)
        {
            RSA rsa = RSA.Create();
            string key = configuration.GetValue<string>(JwtSecurityOptions.PrivateKey);
            if(!string.IsNullOrEmpty(key))
            {
                rsa.ImportRSAPrivateKey(
                    source: Convert.FromBase64String(key),
                    bytesRead: out int _
                );
            }

            return new RsaSecurityKey(rsa);
        }

        public static RsaSecurityKey GetPublicKey(IConfiguration configuration)
        {
            RSA rsa = RSA.Create();
            string key = configuration.GetValue<string>(JwtSecurityOptions.PublicKey);

            if(!string.IsNullOrEmpty(key))
            {
                rsa.ImportRSAPublicKey(
                    source: Convert.FromBase64String(key),
                    bytesRead: out int _
                );
            }

            return new RsaSecurityKey(rsa);
        }
    }
}
