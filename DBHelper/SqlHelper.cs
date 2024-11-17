using Microsoft.Data.SqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DBHelper
{
    public class SqlHelper
    {
        private const int _minParametersLength = 1;

        #region private utility methods & constructors

        internal static void CheckNullException<T>(T value)
        {
            if(value == null || value is string && string.IsNullOrEmpty(value.ToString()))
            {
                throw new ArgumentNullException(nameof(value));
            }
        }

        private static void CheckArgumentException(SqlConnection connection)
        {
            if(connection == null)
            {
                throw new ArgumentException("The transaction was rollbacked or commited, please provide an open transaction.");
            }
        }

        /// <summary>
        /// This method is used to attach array of SqlParameters to a SqlCommand.
        ///
        /// This method will assign a value of DbNull to any parameter with a direction of
        /// InputOutput and a value of null.
        ///
        /// This behavior will prevent default values from being used, but
        /// this will be the less common case than an intended pure output parameter (derived as InputOutput)
        /// where the user provided no input value.
        /// </summary>
        /// <param name="command">The command to which the parameters will be added</param>
        /// <param name="commandParameters">An array of SqlParameters to be added to command</param>
        private static void AttachParameters(SqlCommand command, SqlParameter[] commandParameters)
        {
            CheckNullException(command);
            if(commandParameters != null)
            {
                foreach(SqlParameter parameter in commandParameters)
                {
                    if(parameter != null)
                    {
                        // Check for derived output value with no value assigned
                        if((parameter.Direction == ParameterDirection.InputOutput ||
                            parameter.Direction == ParameterDirection.Input) &&
                            (parameter.Value == null))
                        {
                            parameter.Value = DBNull.Value;
                        }
                        command.Parameters.Add(parameter);
                    }
                }
            }
        }

        /// <summary>
        /// This method assigns dataRow column values to an array of SqlParameters
        /// </summary>
        /// <param name="commandParameters">Array of SqlParameters to be assigned values</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values</param>
        private static void AssignParameterValues(SqlParameter[] commandParameters, DataRow dataRow)
        {
            if((commandParameters == null) || (dataRow == null))
            {
                // Do nothing if we get no data
                return;
            }

            int? i = 0;
            // Set the parameters values
            foreach(SqlParameter commandParameter in commandParameters)
            {
                // Check the parameter name
                if(dataRow.Table.Columns.IndexOf(commandParameter.ParameterName.Substring(1)) != -1)
                    commandParameter.Value = dataRow[commandParameter.ParameterName.Substring(1)];
                i++;
            }
        }

        /// <summary>
        /// This method assigns an array of values to an array of SqlParameters
        /// </summary>
        /// <param name="commandParameters">Array of SqlParameters to be assigned values</param>
        /// <param name="parameterValues">Array of objects holding the values to be assigned</param>
        private static void AssignParameterValues(SqlParameter[] commandParameters, object[] parameterValues)
        {
            if((commandParameters == null) || (parameterValues == null))
            {
                // Do nothing if we get no data
                return;
            }

            // We must have the same number of values as we pave parameters to put them in
            if(commandParameters.Length != parameterValues.Length)
            {
                throw new ArgumentException("Parameter count does not match Parameter Value count.");
            }

            // Iterate through the SqlParameters, assigning the values from the corresponding position in the
            // value array
            for(int i = 0, j = commandParameters.Length; i < j; i++)
            {
                // If the current array value derives from IDbDataParameter, then assign its Value property
                IDbDataParameter parameterValuesRef = parameterValues[i] as IDbDataParameter;
                if(parameterValuesRef != null)
                {
                    IDbDataParameter paramInstance = (IDbDataParameter)parameterValues[i];
                    if(paramInstance.Value == null)
                    {
                        commandParameters[i].Value = DBNull.Value;
                    }
                    else
                    {
                        commandParameters[i].Value = paramInstance.Value;
                    }
                }
                else if(parameterValues[i] == null)
                {
                    commandParameters[i].Value = DBNull.Value;
                }
                else
                {
                    commandParameters[i].Value = parameterValues[i];
                }
            }
        }

        /// <summary>
        /// This method opens (if necessary) and assigns a connection, transaction, command type and parameters
        /// to the provided command
        /// </summary>
        /// <param name="command">The SqlCommand to be prepared</param>
        /// <param name="connection">A valid SqlConnection, on which to execute this command</param>
        /// <param name="transaction">A valid SqlTransaction, or 'null'</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParameters to be associated with the command or 'null' if no parameters are required</param>
        /// <param name="mustCloseConnection"><c>true</c> if the connection was opened by the method, otherwose is false.</param>
        private static async Task<bool> PrepareCommand(SqlCommand command, SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, SqlParameter[] commandParameters)
        {
            bool mustCloseConnection;
            CheckNullException(command);
            CheckNullException(commandText);

            // If the provided connection is not open, we will open it
            if(connection.State != ConnectionState.Open)
            {
                mustCloseConnection = true;
                await connection.OpenAsync();
            }
            else
            {
                mustCloseConnection = false;
            }

            // Associate the connection with the command
            command.Connection = connection;
            command.CommandTimeout = connection.ConnectionTimeout;
            command.CommandText = commandText;

            // If we were provided a transaction, assign it
            if(transaction != null)
            {
                CheckArgumentException(transaction.Connection);
                command.Transaction = transaction;
            }

            // Set the command type
            command.CommandType = commandType;

            // Attach the command parameters if they are provided
            if(commandParameters != null)
            {
                AttachParameters(command, commandParameters);
            }
            return mustCloseConnection;
        }

        #endregion private utility methods & constructors

        #region ExecuteNonQuery

        /// <summary>
        /// Execute a SqlCommand (that returns no resultset and takes no parameters) against the provided SqlConnection.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  int? result = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders");
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <returns>An int? representing the number of rows affected by the command</returns>
        internal static async Task<int?> ExecuteNonQuery(string connectionString, CommandType commandType, string commandText)
        {
            return await ExecuteNonQuery(connectionString, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns no resultset) against the specified SqlConnection
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  int? result = ExecuteNonQuery(conn, CommandType.StoredProcedure, "PublishOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>An int? representing the number of rows affected by the command</returns>
        public static async Task<int?> ExecuteNonQuery(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            CheckNullException(connectionString);

            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            // Create a command and prepare it for execution
            SqlCommand sqlCommand = new SqlCommand();
            await PrepareCommand(sqlCommand, connection, null, commandType, commandText, commandParameters);

            // Finally, execute the command
            int? retval = await sqlCommand.ExecuteNonQueryAsync();

            // Detach the SqlParameters from the command object, so they can be used again
            sqlCommand.Parameters.Clear();
            connection.Close();

            return retval;
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified SqlConnection
        /// using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  int? result = ExecuteNonQuery(conn, "PublishOrders", 24, 36);
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>An int? representing the number of rows affected by the command</returns>
        internal static async Task<int?> ExecuteNonQuery(string connectionString, string spName, params object[] parameterValues)
        {
            CheckNullException(connectionString);
            CheckNullException(spName);

            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connectionString, spName);
                AssignParameterValues(commandParameters, parameterValues);
                return await ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a SqlCommand (that returns no resultset and takes no parameters) against the provided SqlTransaction.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  int? result = ExecuteNonQuery(trans, CommandType.StoredProcedure, "PublishOrders");
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <returns>An int? representing the number of rows affected by the command</returns>
        internal static async Task<int?> ExecuteNonQuery(SqlTransaction transaction, CommandType commandType, string commandText)
        {
            return await ExecuteNonQuery(transaction, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns no resultset) against the specified SqlTransaction
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  int? result = ExecuteNonQuery(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>An int? representing the number of rows affected by the command</returns>
        internal static async Task<int?> ExecuteNonQuery(SqlTransaction transaction, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            CheckNullException(transaction);
            CheckNullException(transaction.Connection);

            SqlCommand cmd = new SqlCommand();
            _ = await PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters);

            int? retval = await cmd.ExecuteNonQueryAsync();
            cmd.Parameters.Clear();
            return retval;
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified
        /// SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  int? result = ExecuteNonQuery(conn, trans, "PublishOrders", 24, 36);
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>An int? representing the number of rows affected by the command</returns>
        internal static async Task<int?> ExecuteNonQuery(SqlTransaction transaction, string spName, params object[] parameterValues)
        {
            CheckNullException(transaction);
            CheckNullException(transaction.Connection);
            CheckNullException(spName);

            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(transaction.Connection, spName);

                AssignParameterValues(commandParameters, parameterValues);

                return await ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteNonQuery

        #region ExecuteDataset

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in
        /// the connection string using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  DataSet ds = ExecuteDataset(connString, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>A dataset containing the resultset generated by the command</returns>
        internal static async Task<DataSet> ExecuteDataset(string connectionString, string spName, bool isRetrun, params object[] parameterValues)
        {
            CheckNullException(connectionString);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                //SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connectionString, spName); -- Original code from sqlDAL
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connectionString, spName, isRetrun); // Added Parameter true to support ReturnValues

                // Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                // Call the overload that takes an array of SqlParameters
                return await ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                return await ExecuteDataset(connectionString, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  DataSet ds = ExecuteDataset(conn, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <returns>A dataset containing the resultset generated by the command</returns>
        internal static async Task<DataSet> ExecuteDataset(string connectionString, CommandType commandType, string commandText)
        {
            // Pass through the call providing null for the set of SqlParameters
            return await ExecuteDataset(connectionString, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the specified SqlConnection
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  DataSet ds = ExecuteDataset(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>A dataset containing the resultset generated by the command</returns>
        public static async Task<DataSet> ExecuteDataset(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            CheckNullException(connectionString);

            // Create & open a SqlConnection, and dispose of it after we are done
            using SqlConnection connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Create a command and prepare it for execution
            using SqlCommand sqlCommand = new SqlCommand();

            await PrepareCommand(sqlCommand, connection, (SqlTransaction)null, commandType, commandText, commandParameters);

            // Create the DataAdapter & DataSet
            using SqlDataAdapter sqlDataAdapter = new SqlDataAdapter(sqlCommand);
            DataSet ds = new DataSet();

            // Fill the DataSet using default values for DataTable names, etc

            sqlDataAdapter.Fill(ds);

            // Detach the SqlParameters from the command object, so they can be used again
            sqlCommand.Parameters.Clear();

            connection.Close();


            // Return the dataset
            return ds;
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection
        /// using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  DataSet ds = ExecuteDataset(conn, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>A dataset containing the resultset generated by the command</returns>
        internal static async Task<DataSet> ExecuteDataset(string connectionString, string spName, params object[] parameterValues)
        {
            CheckNullException(connectionString);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connectionString, spName);

                // Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                // Call the overload that takes an array of SqlParameters
                return await ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                return await ExecuteDataset(connectionString, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  DataSet ds = ExecuteDataset(trans, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <returns>A dataset containing the resultset generated by the command</returns>
        internal static async Task<DataSet> ExecuteDataset(SqlTransaction transaction, CommandType commandType, string commandText)
        {
            // Pass through the call providing null for the set of SqlParameters
            return await ExecuteDataset(transaction, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  DataSet ds = ExecuteDataset(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>A dataset containing the resultset generated by the command</returns>
        internal static async Task<DataSet> ExecuteDataset(SqlTransaction transaction, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);

            // Create a command and prepare it for execution
            SqlCommand cmd = new SqlCommand();
            _ = await PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters);

            // Create the DataAdapter & DataSet
            using SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataSet ds = new DataSet();

            // Fill the DataSet using default values for DataTable names, etc
            da.Fill(ds);

            // Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear();

            // Return the dataset
            return ds;
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified
        /// SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  DataSet ds = ExecuteDataset(trans, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>A dataset containing the resultset generated by the command</returns>
        internal static async Task<DataSet> ExecuteDataset(SqlTransaction transaction, string spName, params object[] parameterValues)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(transaction.Connection, spName);

                // Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                // Call the overload that takes an array of SqlParameters
                return await ExecuteDataset(transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                return await ExecuteDataset(transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteDataset

        #region ExecuteReader

        /// <summary>
        /// This enum is used to indicate whether the connection was provided by the caller, or created by sqlDAL, so that
        /// we can set the appropriate CommandBehavior when calling ExecuteReader()
        /// </summary>
        private enum SqlConnectionOwnership
        {
            /// <summary>Connection is owned and managed by sqlDAL</summary>
            Internal,

            /// <summary>Connection is owned and managed by the caller</summary>
            External
        }

        /// <summary>
        /// Create and prepare a SqlCommand, and call ExecuteReader with the appropriate CommandBehavior.
        /// </summary>
        /// <remarks>
        /// If we created and opened the connection, we want the connection to be closed when the DataReader is closed.
        ///
        /// If the caller provided the connection, we want to leave it to them to manage.
        /// </remarks>
        /// <param name="connection">A valid SqlConnection, on which to execute this command</param>
        /// <param name="transaction">A valid SqlTransaction, or 'null'</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParameters to be associated with the command or 'null' if no parameters are required</param>
        /// <param name="connectionOwnership">Indicates whether the connection parameter was provided by the caller, or created by sqlDAL</param>
        /// <returns>SqlDataReader containing the results of the command</returns>
        private static async Task<SqlDataReader> ExecuteReader(SqlConnection connection, SqlTransaction transaction, CommandType commandType, string commandText, SqlParameter[] commandParameters, SqlConnectionOwnership connectionOwnership)
        {
            CheckNullException(connection);

            bool mustCloseConnection = false;
            // Create a command and prepare it for execution
            SqlCommand cmd = new SqlCommand();
            try
            {
                mustCloseConnection = await PrepareCommand(cmd, connection, transaction, commandType, commandText, commandParameters);

                // Create a reader
                SqlDataReader dataReader;

                // Call ExecuteReader with the appropriate CommandBehavior
                if(connectionOwnership == SqlConnectionOwnership.External)
                {
                    dataReader = await cmd.ExecuteReaderAsync();
                }
                else
                {
                    dataReader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
                }

                // Detach the SqlParameters from the command object, so they can be used again.
                // HACK: There is a problem here, the output parameter values are fletched
                // when the reader is closed, so if the parameters are detached from the command
                // then the SqlReader can�t set its values.
                // When this happen, the parameters can�t be used again in other command.
                bool canClear = true;
                foreach(SqlParameter commandParameter in cmd.Parameters)
                {
                    if(commandParameter.Direction != ParameterDirection.Input)
                        canClear = false;
                }

                if(canClear)
                {
                    cmd.Parameters.Clear();
                }

                return dataReader;
            }
            catch
            {
                if(mustCloseConnection)
                    connection.Close();
                throw;
            }
            finally
            {
                if(mustCloseConnection && connection != null)
                    connection.Close();
            }
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset and takes no parameters) against the database specified in
        /// the connection string.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  SqlDataReader dr = ExecuteReader(connString, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReader(string connectionString, CommandType commandType, string commandText)
        {
            // Pass through the call providing null for the set of SqlParameters
            return await ExecuteReader(connectionString, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the database specified in the connection string
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  SqlDataReader dr = ExecuteReader(connString, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReader(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            CheckNullException(connectionString);
            SqlConnection connection = null;
            try
            {
                connection = new SqlConnection(connectionString);
                connection.Open();

                // Call the private overload that takes an internally owned connection in place of the connection string
                return await ExecuteReader(connection, null, commandType, commandText, commandParameters, SqlConnectionOwnership.Internal); //change From Internal to External
            }
            catch
            {
                // If we fail to return the SqlDatReader, we need to close the connection ourselves
                if(connection != null) connection.Close();
                throw;
            }
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in
        /// the connection string using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  SqlDataReader dr = ExecuteReader(connString, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReader(string connectionString, string spName, params object[] parameterValues)
        {
            CheckNullException(connectionString);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connectionString, spName, true);

                AssignParameterValues(commandParameters, parameterValues);

                return await ExecuteReader(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                return await ExecuteReader(connectionString, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  SqlDataReader dr = ExecuteReader(conn, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReader(SqlConnection connection, CommandType commandType, string commandText)
        {
            // Pass through the call providing null for the set of SqlParameters
            return await ExecuteReader(connection, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the specified SqlConnection
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  SqlDataReader dr = ExecuteReader(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReader(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            // Pass through the call to the private overload using a null transaction value and an externally owned connection
            return await ExecuteReader(connection, (SqlTransaction)null, commandType, commandText, commandParameters, SqlConnectionOwnership.External);
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection
        /// using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  SqlDataReader dr = ExecuteReader(conn, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReader(SqlConnection connection, string spName, params object[] parameterValues)
        {
            CheckNullException(connection);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connection, spName);

                AssignParameterValues(commandParameters, parameterValues);

                return await ExecuteReader(connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                return await ExecuteReader(connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  SqlDataReader dr = ExecuteReader(trans, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReader(SqlTransaction transaction, CommandType commandType, string commandText)
        {
            // Pass through the call providing null for the set of SqlParameters
            return await ExecuteReader(transaction, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///   SqlDataReader dr = ExecuteReader(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReader(SqlTransaction transaction, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);

            // Pass through to private overload, indicating that the connection is owned by the caller
            return await ExecuteReader(transaction.Connection, transaction, commandType, commandText, commandParameters, SqlConnectionOwnership.External);
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified
        /// SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  SqlDataReader dr = ExecuteReader(trans, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReader(SqlTransaction transaction, string spName, params object[] parameterValues)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(transaction.Connection, spName);

                AssignParameterValues(commandParameters, parameterValues);

                return await ExecuteReader(transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                return await ExecuteReader(transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteReader

        #region ExecuteScalar

        /// <summary>
        /// Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the database specified in
        /// the connection string.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  int? orderCount = (int)ExecuteScalar(connString, CommandType.StoredProcedure, "GetOrderCount");
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalar(string connectionString, CommandType commandType, string commandText)
        {
            // Pass through the call providing null for the set of SqlParameters
            return await ExecuteScalar(connectionString, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a 1x1 resultset) against the database specified in the connection string
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  int? orderCount = (int)ExecuteScalar(connString, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalar(string connectionString, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            CheckNullException(connectionString);
            // Create & open a SqlConnection, and dispose of it after we are done
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            // Call the overload that takes a connection in place of the connection string
            return await ExecuteScalar(connection, commandType, commandText, commandParameters);
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the database specified in
        /// the connection string using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  int? orderCount = (int)ExecuteScalar(connString, "GetOrderCount", 24, 36);
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalar(string connectionString, string spName, params object[] parameterValues)
        {
            CheckNullException(connectionString);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connectionString, spName);

                // Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                // Call the overload that takes an array of SqlParameters
                return await ExecuteScalar(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                return await ExecuteScalar(connectionString, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the provided SqlConnection.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  int? orderCount = (int)ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount");
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalar(SqlConnection connection, CommandType commandType, string commandText)
        {
            // Pass through the call providing null for the set of SqlParameters
            return await ExecuteScalar(connection, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  int? orderCount = (int)ExecuteScalar(conn, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalar(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            CheckNullException(connection);

            // Create a command and prepare it for execution
            SqlCommand cmd = new SqlCommand();
            bool mustCloseConnection = await PrepareCommand(cmd, connection, null, commandType, commandText, commandParameters);

            // Execute the command & return the results
            object retval = cmd.ExecuteScalar();

            // Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear();

            if(mustCloseConnection)
                connection.Close();

            return retval;
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection
        /// using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  int? orderCount = (int)ExecuteScalar(conn, "GetOrderCount", 24, 36);
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalar(SqlConnection connection, string spName, params object[] parameterValues)
        {
            CheckNullException(connection);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connection, spName);

                // Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                // Call the overload that takes an array of SqlParameters
                return await ExecuteScalar(connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                return await ExecuteScalar(connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a 1x1 resultset and takes no parameters) against the provided SqlTransaction.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  int? orderCount = (int)ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount");
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalar(SqlTransaction transaction, CommandType commandType, string commandText)
        {
            // Pass through the call providing null for the set of SqlParameters
            return await ExecuteScalar(transaction, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a 1x1 resultset) against the specified SqlTransaction
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  int? orderCount = (int)ExecuteScalar(trans, CommandType.StoredProcedure, "GetOrderCount", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalar(SqlTransaction transaction, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);

            // Create a command and prepare it for execution
            SqlCommand cmd = new SqlCommand();
            _ = await PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters);

            // Execute the command & return the results
            object retval = await cmd.ExecuteScalarAsync();

            // Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear();
            return retval;
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified
        /// SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  int? orderCount = (int)ExecuteScalar(trans, "GetOrderCount", 24, 36);
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalar(SqlTransaction transaction, string spName, params object[] parameterValues)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                // PPull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(transaction.Connection, spName);

                // Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                // Call the overload that takes an array of SqlParameters
                return await ExecuteScalar(transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                return await ExecuteScalar(transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteScalar

        #region ExecuteXmlReader

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  XmlReader r = ExecuteXmlReader(conn, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command using "FOR XML AUTO"</param>
        /// <returns>An XmlReader containing the resultset generated by the command</returns>
        internal static async Task<XmlReader> ExecuteXmlReader(SqlConnection connection, CommandType commandType, string commandText)
        {
            // Pass through the call providing null for the set of SqlParameters
            return await ExecuteXmlReader(connection, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the specified SqlConnection
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  XmlReader r = ExecuteXmlReader(conn, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command using "FOR XML AUTO"</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>An XmlReader containing the resultset generated by the command</returns>
        internal static async Task<XmlReader> ExecuteXmlReader(SqlConnection connection, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            CheckNullException(connection);

            bool mustCloseConnection = false;
            // Create a command and prepare it for execution
            SqlCommand cmd = new SqlCommand();
            try
            {
                mustCloseConnection = await PrepareCommand(cmd, connection, (SqlTransaction)null, commandType, commandText, commandParameters);

                // Create the DataAdapter & DataSet
                XmlReader retval = await cmd.ExecuteXmlReaderAsync();

                // Detach the SqlParameters from the command object, so they can be used again
                cmd.Parameters.Clear();

                return retval;
            }
            catch
            {
                if(mustCloseConnection)
                    connection.Close();
                throw;
            }
            finally
            {
                if(mustCloseConnection && connection != null)
                    connection.Close();
            }
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection
        /// using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  XmlReader r = ExecuteXmlReader(conn, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="spName">The name of the stored procedure using "FOR XML AUTO"</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>An XmlReader containing the resultset generated by the command</returns>
        internal static async Task<XmlReader> ExecuteXmlReader(SqlConnection connection, string spName, params object[] parameterValues)
        {
            CheckNullException(connection);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connection, spName);

                // Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                // Call the overload that takes an array of SqlParameters
                return await ExecuteXmlReader(connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                return await ExecuteXmlReader(connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  XmlReader r = ExecuteXmlReader(trans, CommandType.StoredProcedure, "GetOrders");
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command using "FOR XML AUTO"</param>
        /// <returns>An XmlReader containing the resultset generated by the command</returns>
        internal static async Task<XmlReader> ExecuteXmlReader(SqlTransaction transaction, CommandType commandType, string commandText)
        {
            // Pass through the call providing null for the set of SqlParameters
            return await ExecuteXmlReader(transaction, commandType, commandText, (SqlParameter[])null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  XmlReader r = ExecuteXmlReader(trans, CommandType.StoredProcedure, "GetOrders", new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command using "FOR XML AUTO"</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <returns>An XmlReader containing the resultset generated by the command</returns>
        internal static async Task<XmlReader> ExecuteXmlReader(SqlTransaction transaction, CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);

            // Create a command and prepare it for execution
            SqlCommand cmd = new SqlCommand();
            _ = await PrepareCommand(cmd, transaction.Connection, transaction, commandType, commandText, commandParameters);

            // Create the DataAdapter & DataSet
            XmlReader retval = await cmd.ExecuteXmlReaderAsync();

            // Detach the SqlParameters from the command object, so they can be used again
            cmd.Parameters.Clear();
            return retval;
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified
        /// SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  XmlReader r = ExecuteXmlReader(trans, "GetOrders", 24, 36);
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        /// <returns>A dataset containing the resultset generated by the command</returns>
        internal static async Task<XmlReader> ExecuteXmlReader(SqlTransaction transaction, string spName, params object[] parameterValues)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(transaction.Connection, spName);

                // Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                // Call the overload that takes an array of SqlParameters
                return await ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                return await ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteXmlReader

        #region FillDataset

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset and takes no parameters) against the database specified in
        /// the connection string.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  FillDataset(connString, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"});
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
        /// <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
        /// by a user defined name (probably the actual table name)</param>
        internal static async Task FillDataset(string connectionString, CommandType commandType, string commandText, DataSet dataSet, string[] tableNames)
        {
            CheckNullException(connectionString);
            CheckNullException(dataSet);

            // Create & open a SqlConnection, and dispose of it after we are done
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            // Call the overload that takes a connection in place of the connection string
            await FillDataset(connection, commandType, commandText, dataSet, tableNames);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the database specified in the connection string
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  FillDataset(connString, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        /// <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
        /// <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
        /// by a user defined name (probably the actual table name)
        /// </param>
        internal static async Task FillDataset(string connectionString, CommandType commandType,
            string commandText, DataSet dataSet, string[] tableNames,
            params SqlParameter[] commandParameters)
        {
            CheckNullException(connectionString);
            CheckNullException(dataSet);
            // Create & open a SqlConnection, and dispose of it after we are done
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();
            // Call the overload that takes a connection in place of the connection string
            await FillDataset(connection, commandType, commandText, dataSet, tableNames, commandParameters);
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in
        /// the connection string using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  FillDataset(connString, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, 24);
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
        /// <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
        /// by a user defined name (probably the actual table name)
        /// </param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        internal static async Task FillDataset(string connectionString, string spName,
            DataSet dataSet, string[] tableNames,
            params object[] parameterValues)
        {
            CheckNullException(connectionString);
            CheckNullException(dataSet);
            // Create & open a SqlConnection, and dispose of it after we are done
            using SqlConnection connection = new SqlConnection(connectionString);
            connection.Open();

            // Call the overload that takes a connection in place of the connection string
            await FillDataset(connection, spName, dataSet, tableNames, parameterValues);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlConnection.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  FillDataset(conn, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"});
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
        /// <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
        /// by a user defined name (probably the actual table name)
        /// </param>
        internal static async Task FillDataset(SqlConnection connection, CommandType commandType,
            string commandText, DataSet dataSet, string[] tableNames)
        {
            await FillDataset(connection, commandType, commandText, dataSet, tableNames, null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the specified SqlConnection
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  FillDataset(conn, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
        /// <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
        /// by a user defined name (probably the actual table name)
        /// </param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        internal static async Task FillDataset(SqlConnection connection, CommandType commandType,
            string commandText, DataSet dataSet, string[] tableNames,
            params SqlParameter[] commandParameters)
        {
            await FillDataset(connection, null, commandType, commandText, dataSet, tableNames, commandParameters);
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection
        /// using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  FillDataset(conn, "GetOrders", ds, new string[] {"orders"}, 24, 36);
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
        /// <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
        /// by a user defined name (probably the actual table name)
        /// </param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        internal static async Task FillDataset(SqlConnection connection, string spName,
            DataSet dataSet, string[] tableNames,
            params object[] parameterValues)
        {
            CheckNullException(connection);
            CheckNullException(dataSet);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connection, spName);

                // Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                // Call the overload that takes an array of SqlParameters
                await FillDataset(connection, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                await FillDataset(connection, CommandType.StoredProcedure, spName, dataSet, tableNames);
            }
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset and takes no parameters) against the provided SqlTransaction.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  FillDataset(trans, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"});
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
        /// <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
        /// by a user defined name (probably the actual table name)
        /// </param>
        internal static async Task FillDataset(SqlTransaction transaction, CommandType commandType,
            string commandText,
            DataSet dataSet, string[] tableNames)
        {
            await FillDataset(transaction, commandType, commandText, dataSet, tableNames, null);
        }

        /// <summary>
        /// Execute a SqlCommand (that returns a resultset) against the specified SqlTransaction
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  FillDataset(trans, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
        /// <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
        /// by a user defined name (probably the actual table name)
        /// </param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        internal static async Task FillDataset(SqlTransaction transaction, CommandType commandType,
            string commandText, DataSet dataSet, string[] tableNames,
            params SqlParameter[] commandParameters)
        {
            await FillDataset(transaction.Connection, transaction, commandType, commandText, dataSet, tableNames, commandParameters);
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified
        /// SqlTransaction using the provided parameter values.  This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <remarks>
        /// This method provides no access to output parameters or the stored procedure's return value parameter.
        ///
        /// e.g.:
        ///  FillDataset(trans, "GetOrders", ds, new string[]{"orders"}, 24, 36);
        /// </remarks>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
        /// <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
        /// by a user defined name (probably the actual table name)
        /// </param>
        /// <param name="parameterValues">An array of objects to be assigned as the input values of the stored procedure</param>
        internal static async Task FillDataset(SqlTransaction transaction, string spName,
            DataSet dataSet, string[] tableNames,
            params object[] parameterValues)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);
            CheckNullException(dataSet);
            CheckNullException(spName);

            // If we receive parameter values, we need to figure out where they go
            if((parameterValues != null) && (parameterValues.Length > 0))
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(transaction.Connection, spName);

                // Assign the provided values to these parameters based on parameter order
                AssignParameterValues(commandParameters, parameterValues);

                // Call the overload that takes an array of SqlParameters
                await FillDataset(transaction, CommandType.StoredProcedure, spName, dataSet, tableNames, commandParameters);
            }
            else
            {
                // Otherwise we can just call the SP without params
                await FillDataset(transaction, CommandType.StoredProcedure, spName, dataSet, tableNames);
            }
        }

        /// <summary>
        /// Private helper method that execute a SqlCommand (that returns a resultset) against the specified SqlTransaction and SqlConnection
        /// using the provided parameters.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  FillDataset(conn, trans, CommandType.StoredProcedure, "GetOrders", ds, new string[] {"orders"}, new SqlParameter("@prodid", 24));
        /// </remarks>
        /// <param name="connection">A valid SqlConnection</param>
        /// <param name="transaction">A valid SqlTransaction</param>
        /// <param name="commandType">The CommandType (stored procedure, text, etc.)</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="dataSet">A dataset wich will contain the resultset generated by the command</param>
        /// <param name="tableNames">This array will be used to create table mappings allowing the DataTables to be referenced
        /// by a user defined name (probably the actual table name)
        /// </param>
        /// <param name="commandParameters">An array of SqlParamters used to execute the command</param>
        private static async Task FillDataset(SqlConnection connection, SqlTransaction transaction, CommandType commandType,
            string commandText, DataSet dataSet, string[] tableNames,
            params SqlParameter[] commandParameters)
        {
            CheckNullException(connection);
            CheckNullException(dataSet);

            // Create a command and prepare it for execution
            SqlCommand command = new SqlCommand();
            bool mustCloseConnection = await PrepareCommand(command, connection, transaction, commandType, commandText, commandParameters);

            // Create the DataAdapter & DataSet
            using(SqlDataAdapter dataAdapter = new SqlDataAdapter(command))
            {
                // Add the table mappings specified by the user
                if(tableNames != null && tableNames.Length > 0)
                {
                    //Changes added for string and string builder
                    StringBuilder tableName = new StringBuilder("Table");
                    for(int index = 0; index < tableNames.Length; index++)
                    {
                        if(tableNames[index] == null || tableNames[index].Length == 0) throw new ArgumentException("The tableNames parameter must contain a list of tables, a value was provided as null or empty string.", nameof(tableNames));
                        dataAdapter.TableMappings.Add(tableName.ToString(), tableNames[index]);
                        tableName.Append(index + 1);
                    }
                }

                // Fill the DataSet using default values for DataTable names, etc
                dataAdapter.Fill(dataSet);

                // Detach the SqlParameters from the command object, so they can be used again
                command.Parameters.Clear();
            }

            if(mustCloseConnection)
                connection.Close();
        }

        #endregion FillDataset

        #region UpdateDataset

        /// <summary>
        /// Executes the respective command for each inserted, updated, or deleted row in the DataSet.
        /// </summary>
        /// <remarks>
        /// e.g.:
        ///  UpdateDataset(conn, insertCommand, deleteCommand, updateCommand, dataSet, "Order");
        /// </remarks>
        /// <param name="insertCommand">A valid transact-SQL statement or stored procedure to insert new records into the data source</param>
        /// <param name="deleteCommand">A valid transact-SQL statement or stored procedure to delete records from the data source</param>
        /// <param name="updateCommand">A valid transact-SQL statement or stored procedure used to update records in the data source</param>
        /// <param name="dataSet">The DataSet used to update the data source</param>
        /// <param name="tableName">The DataTable used to update the data source.</param>
        internal static void UpdateDataset(SqlCommand insertCommand, SqlCommand deleteCommand, SqlCommand updateCommand, DataSet dataSet, string tableName)
        {
            CheckNullException(tableName);

            // Create a SqlDataAdapter, and dispose of it after we are done
            using SqlDataAdapter dataAdapter = new SqlDataAdapter();
            // Set the data adapter commands
            dataAdapter.UpdateCommand = updateCommand ?? throw new ArgumentNullException(nameof(updateCommand));
            dataAdapter.InsertCommand = insertCommand ?? throw new ArgumentNullException(nameof(insertCommand));
            dataAdapter.DeleteCommand = deleteCommand ?? throw new ArgumentNullException(nameof(deleteCommand));

            // Update the dataset changes in the data source
            dataAdapter.Update(dataSet, tableName);

            // Commit all the changes made to the DataSet
            dataSet.AcceptChanges();
        }

        #endregion UpdateDataset        

        #region ExecuteNonQueryTypedParams

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns no resultset) against the database specified in
        /// the connection string using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on row values.
        /// </summary>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>An int? representing the number of rows affected by the command</returns>
        internal static async Task<int?> ExecuteNonQueryTypedParams(String connectionString, String spName, DataRow dataRow)
        {
            CheckNullException(connectionString);
            CheckNullException(spName);

            // If the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connectionString, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteNonQuery(connectionString, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns no resultset) against the specified
        /// SqlTransaction using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on row values.
        /// </summary>
        /// <param name="transaction">A valid SqlTransaction object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>An int? representing the number of rows affected by the command</returns>
        internal static async Task<int?> ExecuteNonQueryTypedParams(SqlTransaction transaction, String spName, DataRow dataRow)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);
            CheckNullException(spName);

            // Sf the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(transaction.Connection, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteNonQuery(transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteNonQueryTypedParams

        #region ExecuteDatasetTypedParams

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in
        /// the connection string using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on row values.
        /// </summary>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>A dataset containing the resultset generated by the command</returns>
        internal static async Task<DataSet> ExecuteDatasetTypedParams(string connectionString, String spName, DataRow dataRow)
        {
            CheckNullException(connectionString);
            CheckNullException(spName);

            //If the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connectionString, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteDataset(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteDataset(connectionString, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction
        /// using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on row values.
        /// </summary>
        /// <param name="transaction">A valid SqlTransaction object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>A dataset containing the resultset generated by the command</returns>
        internal static async Task<DataSet> ExecuteDatasetTypedParams(SqlTransaction transaction, String spName, DataRow dataRow)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);
            CheckNullException(spName);

            // If the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(transaction.Connection, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteDataset(transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteDataset(transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteDatasetTypedParams

        #region ExecuteReaderTypedParams

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the database specified in
        /// the connection string using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReaderTypedParams(String connectionString, String spName, DataRow dataRow)
        {
            CheckNullException(connectionString);
            CheckNullException(spName);

            // If the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connectionString, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteReader(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteReader(connectionString, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection
        /// using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <param name="connection">A valid SqlConnection object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReaderTypedParams(SqlConnection connection, String spName, DataRow dataRow)
        {
            CheckNullException(connection);
            CheckNullException(spName);

            // If the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connection, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteReader(connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteReader(connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction
        /// using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <param name="transaction">A valid SqlTransaction object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>A SqlDataReader containing the resultset generated by the command</returns>
        internal static async Task<SqlDataReader> ExecuteReaderTypedParams(SqlTransaction transaction, String spName, DataRow dataRow)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);
            CheckNullException(spName);

            // If the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(transaction.Connection, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteReader(transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteReader(transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteReaderTypedParams

        #region ExecuteScalarTypedParams

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the database specified in
        /// the connection string using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalarTypedParams(String connectionString, String spName, DataRow dataRow)
        {
            CheckNullException(connectionString);
            CheckNullException(spName);

            // If the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connectionString, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteScalar(connectionString, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteScalar(connectionString, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlConnection
        /// using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <param name="connection">A valid SqlConnection object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalarTypedParams(SqlConnection connection, String spName, DataRow dataRow)
        {
            CheckNullException(connection);
            CheckNullException(spName);

            // If the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connection, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteScalar(connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteScalar(connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a 1x1 resultset) against the specified SqlTransaction
        /// using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <param name="transaction">A valid SqlTransaction object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>An object containing the value in the 1x1 resultset generated by the command</returns>
        internal static async Task<object> ExecuteScalarTypedParams(SqlTransaction transaction, String spName, DataRow dataRow)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);
            CheckNullException(spName);

            // If the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(transaction.Connection, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteScalar(transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteScalar(transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteScalarTypedParams

        #region ExecuteXmlReaderTypedParams

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlConnection
        /// using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <param name="connection">A valid SqlConnection object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>An XmlReader containing the resultset generated by the command</returns>
        internal static async Task<XmlReader> ExecuteXmlReaderTypedParams(SqlConnection connection, String spName, DataRow dataRow)
        {
            CheckNullException(connection);
            CheckNullException(spName);

            // If the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(connection, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteXmlReader(connection, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteXmlReader(connection, CommandType.StoredProcedure, spName);
            }
        }

        /// <summary>
        /// Execute a stored procedure via a SqlCommand (that returns a resultset) against the specified SqlTransaction
        /// using the dataRow column values as the stored procedure's parameters values.
        /// This method will query the database to discover the parameters for the
        /// stored procedure (the first time each stored procedure is called), and assign the values based on parameter order.
        /// </summary>
        /// <param name="transaction">A valid SqlTransaction object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="dataRow">The dataRow used to hold the stored procedure's parameter values.</param>
        /// <returns>An XmlReader containing the resultset generated by the command</returns>
        internal static async Task<XmlReader> ExecuteXmlReaderTypedParams(SqlTransaction transaction, String spName, DataRow dataRow)
        {
            CheckNullException(transaction);
            CheckArgumentException(transaction.Connection);
            CheckNullException(spName);

            // If the row has values, the store procedure parameters must be initialized
            if(dataRow != null && dataRow.ItemArray.Length > 0)
            {
                // Pull the parameters for this stored procedure from the parameter cache (or discover them & populate the cache)
                SqlParameter[] commandParameters = SqlDalParameterCache.GetSpParameterSet(transaction.Connection, spName);

                // Set the parameters values
                AssignParameterValues(commandParameters, dataRow);

                return await ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName, commandParameters);
            }
            else
            {
                return await ExecuteXmlReader(transaction, CommandType.StoredProcedure, spName);
            }
        }

        #endregion ExecuteXmlReaderTypedParams

        #region Public Methods

        public string ExecuteBulkInsert(string connectionString, string routineName, DataTable dt)
        {
            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            using(SqlBulkCopy sqlBulkCopy = new SqlBulkCopy(sqlConnection))
            {
                sqlBulkCopy.DestinationTableName = routineName;
                foreach(var column in dt.Columns)
                    sqlBulkCopy.ColumnMappings.Add(column.ToString(), column.ToString());
                sqlBulkCopy.WriteToServer(dt);
            }

            return "";
        }

        public static async Task<DataTable> ExecuteQuery(string connectionString, string routineName)
        {
            using SqlConnection sqlConnection = new SqlConnection(connectionString);

            await sqlConnection.OpenAsync();

            using SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = routineName;
            sqlCommand.CommandType = CommandType.StoredProcedure;

            using DbDataReader sqlReader = sqlCommand.ExecuteReader();

            DataTable dataTable = new DataTable();
            while(!sqlReader.IsClosed)
            {
                dataTable = new DataTable();
                dataTable.Load(sqlReader);
            }
            sqlConnection.Close();
            return dataTable;
        }

        public static async Task<DataTable> ExecuteQuery(string connectionString, string routineName, params object[] parameters)
        {
            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            await sqlConnection.OpenAsync();

            using SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = routineName;
            sqlCommand.CommandType = CommandType.StoredProcedure;
            sqlCommand.Parameters.AddRange(parameters);

            using DbDataReader sqlReader = sqlCommand.ExecuteReader();

            DataTable dataTable = new DataTable();
            while(!sqlReader.IsClosed)
            {
                dataTable = new DataTable();
                dataTable.Load(sqlReader);
            }
            sqlConnection.Close();
            return dataTable;
        }

        public static DataSet ExecuteQuery(string connectionString, string routineName, string datasetName, params object[] parameters)
        {
            DataSet dataSet = new DataSet(datasetName);
            using SqlConnection sqlConnection = new SqlConnection(connectionString);
            SqlDataAdapter sqlDataAdapter = new SqlDataAdapter();
            sqlConnection.Open();

            using SqlCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = routineName;
            sqlCommand.CommandType = CommandType.StoredProcedure;
            if(parameters != null)
            {
                sqlCommand.Parameters.AddRange(parameters);
            }
            sqlDataAdapter.SelectCommand = sqlCommand;

            sqlDataAdapter.Fill(dataSet);
            return dataSet;
        }

        public static IEnumerable<T> ExecuteRoutine<T>(string routineName, IDictionary<string, object> parameters)
        {
            var tList = default(IEnumerable<T>);

            if(!string.IsNullOrEmpty(routineName))
            {
                var sqlParameters = new List<SqlParameter>();

                if(parameters != default(IDictionary<string, object>) &&
                    parameters.Count >= _minParametersLength)
                {
                    foreach(var key in parameters.Keys)
                    {
                        if(String.IsNullOrEmpty(Convert.ToString(parameters[key])))
                        {
                            sqlParameters.Add(new SqlParameter(key, DBNull.Value));
                        }
                        else
                            sqlParameters.Add(new SqlParameter(key, parameters[key]));
                    }
                }
            }

            return tList;
        }

        public static async Task<IEnumerable<T>> AsyncExecuteRoutine<T>(string routineName, IDictionary<string, object> parameters)
        {
            var tList = default(IEnumerable<T>);

            if(!string.IsNullOrEmpty(routineName))
            {
                var sqlParameters = new List<SqlParameter>();

                if(parameters != default(IDictionary<string, object>) &&
                    parameters.Count >= _minParametersLength)
                {
                    foreach(var key in parameters.Keys)
                        sqlParameters.Add(new SqlParameter(key, parameters[key]));
                }
            }

            return await Task.FromResult(tList);
        }

        public static IEnumerable<T> ExecuteRoutinebydt<T>(string routineName, ref SqlParameter[] parameters)
        {
            var tList = default(IEnumerable<T>);

            if(!string.IsNullOrEmpty(routineName))
            {
                var sqlParameters = new List<SqlParameter>();

                if(parameters != default(SqlParameter[]) &&
                    parameters.Length >= _minParametersLength)
                {
                    foreach(SqlParameter parameter in parameters)
                    {
                        //check for derived output value with no value assigned
                        if((parameter.Direction == ParameterDirection.InputOutput) && (parameter.Value == null))
                        {
                            parameter.Value = DBNull.Value;
                        }
                        sqlParameters.Add(parameter);
                    }
                }
            }

            return tList;
        }

        #endregion Public Methods

        #region Protected Methods

        protected virtual T ExecuteReader<T>(string connection, Func<DbDataReader, T> mapEntities, string exec, params object[] parameters)
        {
            using var sqlConnection = new SqlConnection(connection);
            using var command = new SqlCommand(exec, sqlConnection);
            sqlConnection.Open();
            command.Parameters.AddRange(parameters);
            command.CommandType = CommandType.StoredProcedure;
            try
            {
                using var reader = command.ExecuteReader();
                T data = mapEntities(reader);
                return data;
            }
            finally
            {
                sqlConnection.Close();
            }
        }

        public static int ExecuteScriptNonQuery(string connectionString, string routineName, string script)
        {
            SqlConnection sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            using SqlCommand cmd = sqlConnection.CreateCommand();
            cmd.CommandText = script;
            return cmd.ExecuteNonQuery();
        }

        public static object ExecuteScriptScaler(string connectionString, string script)
        {
            SqlConnection sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            using SqlCommand cmd = sqlConnection.CreateCommand();
            cmd.CommandText = script;
            return cmd.ExecuteScalar();
        }

        public void BulkUpload(string connectionString, string procedureName, DataTable dtTable, string tableVariable)
        {
            using SqlConnection connection = new SqlConnection(connectionString);
            using SqlCommand sqlCommand = new SqlCommand(procedureName);
            sqlCommand.CommandType = CommandType.StoredProcedure;
            sqlCommand.Connection = connection;
            sqlCommand.Parameters.AddWithValue(tableVariable, dtTable);
            connection.Open();
            sqlCommand.ExecuteNonQuery();
            connection.Close();
        }

        #endregion Protected Methods
    }

    internal sealed class SqlDalParameterCache
    {
        #region private methods, variables, and constructors

        //Since this class provides only static methods, make the default constructor private to prevent
        //instances from being created with "new SqlDalParameterCache()"
        private SqlDalParameterCache()
        { }

        private static readonly Hashtable paramCache = Hashtable.Synchronized(new Hashtable());

        /// <summary>
        /// Resolve at run time the appropriate set of SqlParameters for a stored procedure
        /// </summary>
        /// <param name="connection">A valid SqlConnection object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="includeReturnValueParameter">Whether or not to include their return value parameter</param>
        /// <returns>The parameter array discovered.</returns>
        private static SqlParameter[] DiscoverSpParameterSet(SqlConnection connection, string spName, bool includeReturnValueParameter)
        {
            SqlHelper.CheckNullException(connection);
            SqlHelper.CheckNullException(spName);

            using SqlCommand sqlCommand = new SqlCommand(spName, connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            connection.Open();
            SqlCommandBuilder.DeriveParameters(sqlCommand);
            connection.Close();

            if(!includeReturnValueParameter)
            {
                sqlCommand.Parameters.RemoveAt(0);
            }

            SqlParameter[] discoveredParameters = new SqlParameter[sqlCommand.Parameters.Count];

            sqlCommand.Parameters.CopyTo(discoveredParameters, 0);

            // Init the parameters with a DBNull value
            foreach(SqlParameter discoveredParameter in discoveredParameters)
            {
                discoveredParameter.Value = DBNull.Value;
            }
            return discoveredParameters;
        }

        /// <summary>
        /// Deep copy of cached SqlParameter array
        /// </summary>
        /// <param name="originalParameters"></param>
        /// <returns></returns>
        private static SqlParameter[] CloneParameters(SqlParameter[] originalParameters)
        {
            SqlParameter[] clonedParameters = new SqlParameter[originalParameters.Length];

            for(int i = 0, j = originalParameters.Length; i < j; i++)
            {
                clonedParameters[i] = (SqlParameter)((ICloneable)originalParameters[i]).Clone();
            }

            return clonedParameters;
        }

        #endregion private methods, variables, and constructors

        #region caching functions

        /// <summary>
        /// Add parameter array to the cache
        /// </summary>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <param name="commandParameters">An array of SqlParamters to be cached</param>
        internal static void CacheParameterSet(string connectionString, string commandText, params SqlParameter[] commandParameters)
        {
            SqlHelper.CheckNullException(connectionString);
            SqlHelper.CheckNullException(commandText);

            string hashKey = connectionString + ":" + commandText;

            paramCache[hashKey] = commandParameters;
        }

        /// <summary>
        /// Retrieve a parameter array from the cache
        /// </summary>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="commandText">The stored procedure name or T-SQL command</param>
        /// <returns>An array of SqlParamters</returns>
        internal static SqlParameter[] GetCachedParameterSet(string connectionString, string commandText)
        {
            SqlHelper.CheckNullException(connectionString);
            SqlHelper.CheckNullException(commandText);
            string hashKey = connectionString + ":" + commandText;

            if(!(paramCache[hashKey] is SqlParameter[] cachedParameters))
            {
                return default;
            }
            else
            {
                return CloneParameters(cachedParameters);
            }
        }

        #endregion caching functions

        #region Parameter Discovery Functions

        /// <summary>
        /// Retrieves the set of SqlParameters appropriate for the stored procedure
        /// </summary>
        /// <remarks>
        /// This method will query the database for this information, and then store it in a cache for future requests.
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <returns>An array of SqlParameters</returns>
        internal static SqlParameter[] GetSpParameterSet(string connectionString, string spName)
        {
            return GetSpParameterSet(connectionString, spName, false);
        }

        /// <summary>
        /// Retrieves the set of SqlParameters appropriate for the stored procedure
        /// </summary>
        /// <remarks>
        /// This method will query the database for this information, and then store it in a cache for future requests.
        /// </remarks>
        /// <param name="connectionString">A valid connection string for a SqlConnection</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="includeReturnValueParameter">A bool value indicating whether the return value parameter should be included in the results</param>
        /// <returns>An array of SqlParameters</returns>
        internal static SqlParameter[] GetSpParameterSet(string connectionString, string spName, bool includeReturnValueParameter)
        {
            SqlHelper.CheckNullException(connectionString);
            SqlHelper.CheckNullException(spName);

            using SqlConnection connection = new SqlConnection(connectionString);
            return GetSpParameterSetInternal(connection, spName, includeReturnValueParameter);
        }

        /// <summary>
        /// Retrieves the set of SqlParameters appropriate for the stored procedure
        /// </summary>
        /// <remarks>
        /// This method will query the database for this information, and then store it in a cache for future requests.
        /// </remarks>
        /// <param name="connection">A valid SqlConnection object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <returns>An array of SqlParameters</returns>
        internal static SqlParameter[] GetSpParameterSet(SqlConnection connection, string spName)
        {
            return GetSpParameterSet(connection, spName, false);
        }

        /// <summary>
        /// Retrieves the set of SqlParameters appropriate for the stored procedure
        /// </summary>
        /// <remarks>
        /// This method will query the database for this information, and then store it in a cache for future requests.
        /// </remarks>
        /// <param name="connection">A valid SqlConnection object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="includeReturnValueParameter">A bool value indicating whether the return value parameter should be included in the results</param>
        /// <returns>An array of SqlParameters</returns>
        internal static SqlParameter[] GetSpParameterSet(SqlConnection connection, string spName, bool includeReturnValueParameter)
        {
            SqlHelper.CheckNullException(connection);
            using SqlConnection clonedConnection = (SqlConnection)((ICloneable)connection).Clone();
            return GetSpParameterSetInternal(clonedConnection, spName, includeReturnValueParameter);
        }

        /// <summary>
        /// Retrieves the set of SqlParameters appropriate for the stored procedure
        /// </summary>
        /// <param name="connection">A valid SqlConnection object</param>
        /// <param name="spName">The name of the stored procedure</param>
        /// <param name="includeReturnValueParameter">A bool value indicating whether the return value parameter should be included in the results</param>
        /// <returns>An array of SqlParameters</returns>
        private static SqlParameter[] GetSpParameterSetInternal(SqlConnection connection, string spName, bool includeReturnValueParameter)
        {
            SqlHelper.CheckNullException(connection);
            SqlHelper.CheckNullException(spName);

            string hashKey = connection.ConnectionString + ":" + spName + (includeReturnValueParameter ? ":include ReturnValue Parameter" : "");

            SqlParameter[] cachedParameters;

            cachedParameters = paramCache[hashKey] as SqlParameter[];
            if(cachedParameters == null)
            {
                SqlParameter[] spParameters = DiscoverSpParameterSet(connection, spName, includeReturnValueParameter);
                paramCache[hashKey] = spParameters;
                cachedParameters = spParameters;
            }

            return CloneParameters(cachedParameters);
        }

        #endregion Parameter Discovery Functions
    }
}
