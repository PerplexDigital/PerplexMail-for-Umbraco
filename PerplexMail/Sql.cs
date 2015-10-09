using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PerplexMail
{
    public static class Sql
    {
        const string WebConfigUmbracoCsAlias = "umbracoDbDSN";

        static SqlConnection CreateConnection()
        {
            // Attempt #1: Try to read the Umbraco connectionstring
            var csUmbraco = ConfigurationManager.ConnectionStrings[WebConfigUmbracoCsAlias];
            if (csUmbraco != null && !String.IsNullOrEmpty(csUmbraco.ConnectionString))
                return new SqlConnection(csUmbraco.ConnectionString);

            // Attempt #2: Try to read the Umbraco connectionstring as an application setting (legacy)
            string csAppsettingsUmbraco = ConfigurationManager.AppSettings[WebConfigUmbracoCsAlias];
            if (csAppsettingsUmbraco != null && !String.IsNullOrEmpty(csAppsettingsUmbraco))
                return new SqlConnection(csAppsettingsUmbraco);

            // Attempt 3: Unable to locate any Umbraco connectionstring. Try to simply load the first connectionstring connection in the list.
            if (ConfigurationManager.ConnectionStrings.Count > 0)
            {
                var csEerste = ConfigurationManager.ConnectionStrings[1];
                if (csEerste != null && !String.IsNullOrEmpty(csEerste.ConnectionString))
                    return new SqlConnection(csEerste.ConnectionString);
            }

            // Could not determine any database connection
            throw new Exception("Error executing private method 'PerplexMail.Sql.CreateConnection()': Could not determine the database connectionstring.");
        }

        /// <summary>
        /// Provides direct access to the data records returned by the SqlDataReader.
        /// This method returns an enumerator, so you should iterate over the results of this function in a foreach loop.
        /// Example ==> foreach (IDataRecord r in createSqlDataEnumerator("sPMyStoredProcedure, CommandType.StoredProcedure))
        /// </summary>
        /// <param name="SQL">Either a stored procedure name or SQL query text</param>
        /// <param name="type">Stored procedure or text</param>
        /// <param name="sqlParameters">(OPTIONEEL) Geef hier eventueel extra sql parameters mee. Vaak handig als je een stored procedure aanroept</param>
        /// <returns>The number of rows affeceted</returns>
        public static IEnumerable<IDataRecord> CreateSqlDataEnumerator(string SQL, CommandType type, params SqlParameter[] sqlParameters)
        {
            return CreateSqlDataEnumerator(CreateConnection(), SQL, type, sqlParameters);
        }

        /// <summary>
        /// Provides direct access to the data records returned by the SqlDataReader.
        /// This method returns an enumerator, so you should iterate over the results of this function in a foreach loop.
        /// Example ==> foreach (IDataRecord r in createSqlDataEnumerator("sPMyStoredProcedure, CommandType.StoredProcedure))
        /// </summary>
        /// <param name="c">Explicit SQL connection to use</param>
        /// <param name="SQL">Either a stored procedure name or SQL query text</param>
        /// <param name="type">Stored procedure or text</param>
        /// <param name="sqlParameters">(OPTIONEEL) Geef hier eventueel extra sql parameters mee. Vaak handig als je een stored procedure aanroept</param>
        /// <returns>The number of rows affeceted</returns>
        public static IEnumerable<IDataRecord> CreateSqlDataEnumerator(SqlConnection c, string SQL, CommandType type, params SqlParameter[] sqlParameters)
        {
            using (c)
            {
                c.Open();
                using (var command = new SqlCommand())
                {
                    command.CommandText = SQL;
                    command.CommandType = type;
                    command.Connection = c;
                    command.Parameters.AddRange(sqlParameters);
                    var rdr = command.ExecuteReader();
                    if (rdr.HasRows)
                        while (rdr.Read())
                            yield return rdr as IDataRecord;
                }
            }
        }

        /// <summary>
        /// Determines whether a SQL stored procedure exists in the current database
        /// </summary>
        /// <param name="procedureName"></param>
        /// <returns></returns>
        public static Boolean DoesSQLStoredProcedureExist(String procedureName)
        {
            return "1" == ExecuteSql("SELECT TOP 1 '1' FROM sys.procedures WHERE name = @spName", CommandType.Text, new SqlParameter("@spName", procedureName));
        }

        /// <summary>
        /// Determines whether a SQL table exists in the current database
        /// </summary>
        /// <param name="tableName">The table to search for</param>
        /// <returns>Existance of the table as a boolean</returns>
        public static Boolean DoesSQLTableExist(String tableName)
        {
            return "1" == ExecuteSql("SELECT TOP 1 '1' FROM INFORMATION_SCHEMA.TABLES  WHERE TABLE_SCHEMA = 'dbo' AND  TABLE_NAME = @tblName", CommandType.Text, new SqlParameter("@tblName", tableName));
        }

        /// <summary>
        /// Executes any SQL query. The resulting records are mapped to the specified class through Dapper.
        /// </summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="sql">The SQL query or stored procedure name</param>
        /// <param name="commandType">The SQL command type (generally speaking, Text or StoredProcedure)</param>
        /// <param name="sqlParameters">SQL Query parameters as SqlParameters</param>
        /// <returns>A list of the specified type T</returns>
        public static List<T> ExecuteSql<T>(string SQL, CommandType type, params SqlParameter[] sqlParameters) where T: class, new()
        {
            return ExecuteSql<T>(CreateConnection(), SQL, type, sqlParameters);
        }

        /// <summary>
        /// Executes any SQL query. The resulting records are mapped to the specified class through Dapper.
        /// </summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="c">Explicit SQL connection to use</param>
        /// <param name="sql">The SQL query or stored procedure name</param>
        /// <param name="commandType">The SQL command type (generally speaking, Text or StoredProcedure)</param>
        /// <param name="sqlParameters">SQL Query parameters as SqlParameters</param>
        /// <returns>A list of the specified type T</returns>
        public static List<T> ExecuteSql<T>(SqlConnection c, string SQL, CommandType type, params SqlParameter[] sqlParameters) where T: class, new()
        {
            // Transformeer even de sql parameters naar Dapper parameters (stiekem zijn dat eigenlijk ook gewoon sql parameters...)
            var result = new List<T>();
            var t = typeof(T);

            using (c)
            {
                c.Open();
                using (var command = new SqlCommand())
                {
                    command.CommandText = SQL;
                    command.CommandType = type;
                    command.Connection = c;
                    command.Parameters.AddRange(sqlParameters);

                    var data = command.ExecuteReader();
                    if (data.HasRows)
                    {
                        int numberOfColumns = data.FieldCount;
                        var properties = new List<PropertyInfo>();
                        for (int i = 0; i < numberOfColumns; i++)
                        {
                            var columnName = data.GetName(i);
                            var p = t.GetProperty(columnName);
                            if (p != null)
                                properties.Add(p);
                            else
                                properties.Add(null);
                        }

                        while (data.Read())
                        {
                            var resultRecord = new T();
                            var row = data as IDataRecord;
                            for (int i = 0; i < numberOfColumns; i++)
                            {
                                var p = properties[i];
                                if (p != null && row[i] != DBNull.Value)
                                    p.SetValue(resultRecord, row[i]);
                            }
                            result.Add(resultRecord);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Executes any SQL query as a sacaler query, returning only the first row of the first record.
        /// </summary>
        /// <param name="sql">The SQL query or stored procedure name</param>
        /// <param name="commandType">The SQL command type (generally speaking, Text or StoredProcedure)</param>
        /// <param name="sqlParameters">SQL Query parameters as SqlParameters</param>
        /// <returns>The results of the scalar query (first row of the first record) of the resulting response</returns>
        public static string ExecuteSql(string SQL, CommandType type, params SqlParameter[] sqlParameters)
        {
            return ExecuteSql(CreateConnection(), SQL, type, sqlParameters);
        }

        /// <summary>
        /// Executes any SQL query as a sacaler query, returning only the first row of the first record.
        /// </summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="c">Explicit SQL connection to use</param>
        /// <param name="sql">The SQL query or stored procedure name</param>
        /// <param name="commandType">The SQL command type (generally speaking, Text or StoredProcedure)</param>
        /// <param name="sqlParameters">SQL Query parameters as SqlParameters</param>
        /// <returns>The results of the scalar query (first row of the first record) of the resulting response</returns>
        public static string ExecuteSql(SqlConnection c, string SQL, CommandType type, params SqlParameter[] sqlParameters)
        {
            using (c)
            {
                c.Open();
                using (var command = new SqlCommand())
                {
                    command.CommandText = SQL;
                    command.CommandType = type;
                    command.Connection = c;
                    command.Parameters.AddRange(sqlParameters);

                    var result = command.ExecuteScalar();
                    if (result != null)
                        return result.ToString();
                    else
                        return null;
                }
            }
        }
    }
}