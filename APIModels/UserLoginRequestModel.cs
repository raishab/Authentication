using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIModels
{
    public class UserLoginRequestModel
    {
        [Required]
        [MaxLength(50)]
        public string? EmailId { get; set; }

        [Required]
        [MaxLength(100)]
        public string? Password { get; set; }
    }
}
