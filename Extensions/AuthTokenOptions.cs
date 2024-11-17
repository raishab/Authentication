using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
namespace Extensions
{
    public class AuthTokenOptions
    {
        public List<string>? Audiences { get; set; }

        public string? Issuer { get; set; }

        public RsaSecurityKey? RsaPublicSecurityKey { get; set; }

        public SigningCredentials? SigningCredentials { get; set; }
    }
}
