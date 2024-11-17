using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIModels.Common
{
    public class JwtConfiguration
    {
        public List<string>? Audiences { get; set; }
        public string? AuthAudience { get; set; }

        public string? Issuer { get; set; }

        public double? TokenValidityInHours { get; set; }
    }
}
