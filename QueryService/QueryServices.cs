using Microsoft.Extensions.Options;
using APIModels.Common;
using DBHelper;
using System.Data;
using Microsoft.Data.SqlClient;
namespace QueryService
{
    public class QueryServices : IQueryServices
    {
        private readonly string? _connString;
        public QueryServices(IOptions<ConnectionStringModel> app)
        {
            _connString = app.Value.DefaultConnection;
        }

        public async Task<DataTable> ExecuteQuery(string storedProcedure)
        {
            return await SqlHelper.ExecuteQuery(_connString == null ? "" : _connString, storedProcedure);
        }

        public async Task<DataTable> ExecuteQuery<T>(T model, string storedProcedure)
        {
            SqlParameter[] param = SqlParameterHelper.GetSqlParameter(model);

            return await SqlHelper.ExecuteQuery(_connString == null ? "" : _connString, storedProcedure, param);
        }

        public async Task<int?> ExecuteNonQuery<T>(T model, string storedProcedure)
        {
            SqlParameter[] param = SqlParameterHelper.GetSqlParameter(model);

            return await SqlHelper.ExecuteNonQuery(_connString == null ? "" : _connString, CommandType.StoredProcedure,
                storedProcedure, param);
        }

        public async Task<DataSet> ExecuteDataset<T>(T model, string storedProcedure)
        {
            SqlParameter[] param = SqlParameterHelper.GetSqlParameter(model);

            return await SqlHelper.ExecuteDataset(_connString == null ? "" : _connString, CommandType.StoredProcedure,
                storedProcedure, param);
        }

    }
}
