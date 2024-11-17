using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIModels
{
    public class TokenModel
    {
        public string? AuthTokenValue { get; set; }

        [DefaultValue(null)]
        public DateTime? Expiration { get; set; }
    }
}
