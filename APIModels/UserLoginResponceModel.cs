using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace APIModels
{
    public class UserLoginResponceModel : TokenModel
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        public int? Role { get; set; }

        public string? Email { get; set; }

        [JsonIgnore]
        public string? OtpErrorMessage { get; set; }
    }
}
