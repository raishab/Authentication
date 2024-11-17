
using Microsoft.Extensions.Options;
using APIModels.Common;
using System.Data;
using Microsoft.Data.SqlClient;
using DBHelper;
namespace DataService
{
    public class DataServices : IDataServices
    {
        private readonly string? _connString;
        public DataServices(IOptions<ConnectionStringModel> app)
        {
            _connString = app.Value.DefaultConnection;
        }
        public async Task<DataTable> ExecuteInsertQuery<T>(T model, string storedProcedure)
        {
            return await ExecuteQuery(model, storedProcedure);
        }

        public async Task<DataTable> ExecuteUpdateQuery<T>(T model, string storedProcedure)
        {
            return await ExecuteQuery(model, storedProcedure);
        }

        public async Task<int?> ExecuteDeleteQuery<T>(T model, string storedProcedure)
        {
            return await ExecuteNonQuery(model, storedProcedure);
        }

        public async Task<int?> ExecuteUpdateNonQuery<T>(T model, string storedProcedure)
        {
            return await ExecuteNonQuery(model, storedProcedure);
        }

        internal async Task<int?> ExecuteNonQuery<T>(T model, string storedProcedure)
        {
            SqlParameter[] param = SqlParameterHelper.GetSqlParameter(model);

            return await SqlHelper.ExecuteNonQuery(_connString == null ? "" : _connString, CommandType.StoredProcedure,
                storedProcedure, param);
        }

        internal async Task<DataTable> ExecuteQuery<T>(T model, string storedProcedure)
        {
            SqlParameter[] param = SqlParameterHelper.GetSqlParameter(model);

            return await SqlHelper.ExecuteQuery(_connString == null ? "" : _connString, storedProcedure, param);
        }
    }
}
