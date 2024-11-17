
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace ADL.AirportSampling.DBHelper
{
    public static class DataTableHelper
    {
        public static T ToObject<T>(this DataRow row) where T : class, new()
        {
            T obj = new T();

            foreach (var prop in obj.GetType().GetProperties().Where(x => x.CanWrite))
            {
                try
                {
                    if (prop.PropertyType.IsGenericType && prop.PropertyType.Name.Contains("Nullable") && prop.CanWrite)
                    {
                        if (!string.IsNullOrEmpty(row[prop.Name].ToString()))
                            prop.SetValue(obj, Convert.ChangeType(row[prop.Name],
                            Nullable.GetUnderlyingType(prop.PropertyType), null));

                    }
                    else
                    {
                        prop.SetValue(obj, Convert.ChangeType(row[prop.Name], prop.PropertyType), null);
                    }
                }
                catch (Exception)
                {
                    prop.SetValue(obj, default);
                }
            }
            return obj;
        }
        /// <summary>
        /// Converts a DataTable to a list with generic objects
        /// </summary>
        /// <typeparam name="T">Generic object</typeparam>
        /// <param name="table">DataTable</param>
        /// <returns>List with generic objects</returns>
        public static List<T> DataTableToList<T>(this DataTable table) where T : class, new()
        {
            List<T> list = new List<T>();
            try
            {
                

                foreach (var row in table.AsEnumerable())//System.Data.DataSetExtensions
                {
                    var obj = row.ToObject<T>();

                    list.Add(obj);
                }

                return list;
            }
            catch
            {
                return list;
            }
        }
        public static T DataTableToObject<T>(this DataTable table) where T : class, new()
        {
            try
            {
                T response = new T();
                if (table != null && table.Rows.Count > 0)
                {
                    response = table.Rows[0].ToObject<T>();
                }
                return response;
            }
            catch
            {
                return null;
            }
        }
    }
}

