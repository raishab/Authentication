using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataService
{
    public interface IDataServices
    {
        Task<DataTable> ExecuteInsertQuery<T>(T model, string storedProcedure);

        Task<DataTable> ExecuteUpdateQuery<T>(T model, string storedProcedure);

        Task<int?> ExecuteUpdateNonQuery<T>(T model, string storedProcedure);

        Task<int?> ExecuteDeleteQuery<T>(T model, string storedProcedure);
    }
}
