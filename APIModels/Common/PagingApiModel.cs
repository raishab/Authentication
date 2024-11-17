using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APIModels.Common
{
    public class PagingApiModel
    {
        [DefaultValue(1)]
        public int CurrentPage { get; set; }

        [DefaultValue(10)]
        public int PageSize { get; set; }

        public string? SortField { get; set; }

        [DefaultValue(null)]
        public bool? IsSortOrderAscending { get; set; }
    }
}
