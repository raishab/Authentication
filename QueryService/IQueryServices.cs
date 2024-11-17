using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QueryService
{
    public interface IQueryServices
    {
        Task<DataTable> ExecuteQuery(string storedProcedure);
        Task<DataTable> ExecuteQuery<T>(T model, string storedProcedure);

        Task<int?> ExecuteNonQuery<T>(T model, string storedProcedure);

        Task<DataSet> ExecuteDataset<T>(T model, string storedProcedure);
    }
}
