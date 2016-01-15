using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace PerplexMail
{
    public static class Sql
    {
        /// <summary>
        /// Determines whether a SQL stored procedure exists in the current database
        /// </summary>
        /// <param name="procedureName"></param>
        /// <returns></returns>
        public static Boolean DoesSQLStoredProcedureExist(String procedureName)
        {
            return "1" == ExecuteSql(Constants.SQL_QUERY_SP_EXISTS, CommandType.Text, new { spName = procedureName });
        }

        /// <summary>
        /// Determines whether a SQL table exists in the current database
        /// </summary>
        /// <param name="tableName">The table to search for</param>
        /// <returns>Existance of the table as a boolean</returns>
        public static Boolean DoesSQLTableExist(String tableName)
        {
            return "1" == ExecuteSql(Constants.SQL_QUERY_TABLE_EXISTS, CommandType.Text, new { tblName = tableName });
        }

        /// <summary>
        /// Provides direct access to the data records returned by the SqlDataReader.
        /// This method returns an enumerator, so you should iterate over the results of this function in a foreach loop.
        /// Example ==> foreach (IDataRecord r in createSqlDataEnumerator("sPMyStoredProcedure, CommandType.StoredProcedure))
        /// </summary>
        /// <param name="c">Explicit SQL connection to use</param>
        /// <param name="SQL">Either a stored procedure name or SQL query text</param>
        /// <param name="type">Stored procedure or text</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>The number of rows affeceted</returns>
        public static IEnumerable<IDataRecord> CreateSqlDataEnumerator(string SQL, CommandType type, object parameters = null)
        {
            var db = Umbraco.Core.ApplicationContext.Current.DatabaseContext.Database;
            db.OpenSharedConnection();
            using (var command = db.CreateCommand(db.Connection, SQL, parameters))
            {
                command.CommandType = type;
                var rdr = command.ExecuteReader();
                while (rdr.Read())
                    yield return rdr as IDataRecord;
            }
            db.CloseSharedConnection();
        }

        /// <summary>
        /// Executes any SQL query. The resulting records are mapped to the specified class through Dapper.
        /// </summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="sql">The SQL query or stored procedure name</param>
        /// <param name="commandType">The SQL command type (generally speaking, Text or StoredProcedure)</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>A list of the specified type T</returns>
        public static List<T> ExecuteSql<T>(string SQL, CommandType type, object parameters = null) where T: class, new()
        {
            var result = new List<T>();
            var t = typeof(T);

            var db = Umbraco.Core.ApplicationContext.Current.DatabaseContext.Database;
            db.EnableNamedParams = true;
            db.OpenSharedConnection();
            
            using (var command = db.CreateCommand(db.Connection, SQL, parameters))
            {
                command.CommandType = type;
                var data = command.ExecuteReader();
                if (data != null && data.FieldCount > 0)
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

            db.CloseSharedConnection();
            return result;
        }

        /// <summary>
        /// Executes any SQL query as a sacaler query, returning only the first row of the first record.
        /// </summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="c">Explicit SQL connection to use</param>
        /// <param name="sql">The SQL query or stored procedure name</param>
        /// <param name="commandType">The SQL command type (generally speaking, Text or StoredProcedure)</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>The results of the scalar query (first row of the first record) of the resulting response</returns>
        public static string ExecuteSql(string SQL, CommandType type, object parameters = null)
        {
            var db = Umbraco.Core.ApplicationContext.Current.DatabaseContext.Database;
            db.OpenSharedConnection();
            try
            {
                using (var command = db.CreateCommand(db.Connection, SQL, parameters))
                {
                    command.CommandType = type;
                    var data = command.ExecuteScalar();
                    if (data != null)
                        return data.ToString();
                    else
                        return null;
                }
            }
            finally
            {
                db.CloseSharedConnection();
            }
        }

        /// <summary>
        /// Executes an SQL query and returns the current identity 
        /// </summary>
        /// <typeparam name="T">Any class type</typeparam>
        /// <param name="c">Explicit SQL connection to use</param>
        /// <param name="sql">The SQL query or stored procedure name</param>
        /// <param name="commandType">The SQL command type (generally speaking, Text or StoredProcedure)</param>
        /// <param name="parameters">Query parameters</param>
        /// <returns>The results of the scalar query (first row of the first record) of the resulting response</returns>
        public static int ExecuteSqlWithIdentity(string SQL, CommandType type, object parameters = null)
        {
            var db = Umbraco.Core.ApplicationContext.Current.DatabaseContext.Database;
            db.OpenSharedConnection();
            try
            {
                using (var command = db.CreateCommand(db.Connection, SQL, parameters))
                {
                    command.CommandType = type;
                    command.ExecuteNonQuery();
                    command.CommandText = "SELECT @@IDENTITY";
                    command.CommandType = CommandType.Text;
                    int result = 0;
                    var id = command.ExecuteScalar();
                    if (id != null)
                        int.TryParse(id.ToString(), out result);
                    return result;
                }
            }
            finally
            {
                db.CloseSharedConnection();
            }
        }
    }
}