using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIModels.Common
{
    public class PaginationApiResponse<T> : ApiResponse<T>
    {
        public int? Count { get; set; }
    }
}
