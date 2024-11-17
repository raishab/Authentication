using Microsoft.IdentityModel.Tokens;

namespace Extension
{
    public class AuthTokenOptions
    {
        public List<string>? Audiences { get; set; }

        public string? Issuer { get; set; }

        public RsaSecurityKey? RsaPublicSecurityKey { get; set; }

        public SigningCredentials? SigningCredentials { get; set; }
    }
}
