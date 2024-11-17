
using Microsoft.Data.SqlClient;
namespace DBHelper
{
    public static class SqlParameterHelper
    {
        public static SqlParameter[] GetSqlParameter<T>(T model)
        {
            SqlParameter[] param = new SqlParameter[0];
            if(model != null)
            {
                var properties = model.GetType().GetProperties();

                param = new SqlParameter[properties.Length];

                for(int index = 0; index < properties.Length; index++)
                {
                    var value = properties[index].GetValue(model);

                    param[index] = new SqlParameter($"@{properties[index].Name}", value);
                }


            }
            return param;
        }
    }
}
