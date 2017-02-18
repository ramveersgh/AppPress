using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Xml;
using System.Data.Common;
using MySql.Data.MySqlClient;
using MySql.Data;
using System.Data;
using System.Configuration;
using System.Data.SqlClient;

namespace AppPressFramework
{
    public enum DatabaseType
    {
        SqlServer = 1,
        MySql = 2,
    }
    public class AppPressKeys
    {
        public const string ErrorFormException = "ErrorFormException";
    }
    public class DAOBasic
    {
        public string dbName;
        /// <summary>
        /// Type of Database. MYSQL and MSSQL are supported
        /// </summary>
        public DatabaseType databaseType = DatabaseType.SqlServer;

        internal static string SchemaColumnName = null;

        internal DbConnection conn = null;
        internal DbTransaction trans = null;
        private string conStr = null;
        /// <summary>
        /// Format to use for converting DateTime for purpose of SQL Query
        /// </summary>
        public static string DBDateTimeFormat = "yyyy'-'MM'-'dd HH':'mm':'ss";
        /// <summary>
        /// Format to use for converting DateTime for purpose of SQL Query
        /// </summary>
        public static string DBDateFormat = "yyyy'-'MM'-'dd";
        /// <summary>
        /// Quote Character for Table Name and Column Names. It is ` for MySQL and " for MSSQL
        /// </summary>
        public string SQLQuote;

        /// <summary>
        /// Constructor for DAOBasic
        /// use Try block around code using the DAOBasic object. In finally call Close
        /// </summary>
        public DAOBasic()
        {
            databaseType = AppPress.Settings.databaseType;
            if (databaseType == DatabaseType.MySql)
            {
                this.SQLQuote = "`";
                conn = new MySqlConnection(AppPress.Settings.ConnectionString);
            }
            else if (databaseType == DatabaseType.SqlServer)
            {
                this.SQLQuote = "\"";
                conn = new SqlConnection(AppPress.Settings.ConnectionString);
            }
            conn.Open();
            dbName = conn.Database;
            if (databaseType == DatabaseType.MySql)
                ExecuteNonQuery("SET SESSION group_concat_max_len = 10000000");

        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="connnectionStr"></param>
        public DAOBasic(string connnectionStr, DatabaseType dbType)
        {
            databaseType = dbType;
            this.conStr = connnectionStr;
            if (dbType == DatabaseType.MySql)
            {
                this.SQLQuote = "`";
                conn = new MySqlConnection(connnectionStr);
            }
            else
            {
                this.SQLQuote = "\"";
                conn = new SqlConnection(connnectionStr);
            }
            dbName = conn.Database;
            conn.Open();
        }

        internal static DAOBasic _DAOBasic(DAOBasic dAOBasic)
        {
            // TODO: Complete member initialization
            if (dAOBasic.conStr != null)
                return new DAOBasic(dAOBasic.conStr, dAOBasic.databaseType);
            else
                return new DAOBasic();
        }
        public string QuoteDBName(string dbName)
        {
            if (databaseType == DatabaseType.MySql)
                return SQLQuote + dbName + SQLQuote;
            else if (databaseType == DatabaseType.SqlServer)
                return dbName + ".dbo";
            throw new NotImplementedException();
        }
        internal void ReOpen()
        {
            this.conn.Open();
        }
        /// <summary>
        /// Database Level Begin Transaction. Used when doing Database changes when do not have AppPress object. For example updating database from a Thread.
        /// </summary>
        /// <param name="isolationLevel">Refer to help on .net IsolationLevel</param>
        public void BeginTrans(IsolationLevel isolationLevel = IsolationLevel.RepeatableRead)
        {
            if (trans != null)
                throw new Exception("Internal Error in Begin Transaction");
            trans = conn.BeginTransaction(isolationLevel);
        }
        /// <summary>
        /// Database Level Commit Transaction. Used when doing Database changes when do not have AppPress object. For example updating database from a Thread.
        /// </summary>
        public void Commit()
        {
            trans.Commit();
            trans.Dispose();
            trans = null;
        }
        /// <summary>
        /// Database Level Rollback Transaction. Used when doing Database changes when do not have AppPress object. For example updating database from a Thread.
        /// </summary>
        public void RollBack()
        {
            trans.Rollback();
            trans.Dispose();
            trans = null;
        }
        /// <summary>
        /// Returns DataSet for the Query
        /// </summary>
        /// <param name="query">Query to execute</param>
        /// <returns></returns>
        public DataSet ExecuteToGetDataSet(string query)
        {
            try
            {
                DbCommand cmd = GetDBCommand();
                cmd.Connection = conn;
                cmd.Transaction = trans;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;

                DbDataAdapter adapter = GetDBDataAdapter();
                adapter.SelectCommand = cmd;
                DataSet ds = new DataSet();
                adapter.Fill(ds);
                adapter.Dispose();
                cmd.Dispose();
                return ds;
            }
            catch (MySqlException ex)
            {
                if (ex.ErrorCode == -2147467259)
                {
                    throw new ForeignKeyConstraintException(ex.Message);
                }
                var message = "SQL Error: " + ex.Message;
                if (AppPress.Settings.developer)
                    message += "\nQuery:\n" + query;
                throw new Exception(message);
            }
            catch (Exception ex)
            {
                var message = "SQL Error: " + ex.Message;
                if (AppPress.Settings.developer)
                    message += "\nQuery:\n" + query;
                throw new Exception(message);
            }
        }
        /// <summary>
        /// Executes a Insert, Update or Delete SQL Statement
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>Number of Records affected</returns>
        public int ExecuteNonQuery(string query)
        {
            try
            {
                DbCommand cmd = GetDBCommand();
                cmd.Connection = conn;
                cmd.Transaction = trans;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;

                int recordsAffected = cmd.ExecuteNonQuery();
                cmd.Dispose();
                return recordsAffected;
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(MySqlException))
                {
                    var mex = (MySqlException)ex;
                    if (mex.Number == 1062) // Unique
                        throw ex;
                    if (mex.Number == 1451) // ForeignKey
                        throw ex;
                    if (mex.Number == 1406) // Data too long
                        throw ex;
                }
                var message = "SQL Error: " + ex.Message;
                if (AppPress.Settings == null || AppPress.Settings.developer)
                    message += "\nQuery:\n" + query;
                throw new Exception(message);
            }
        }
        /// <summary>
        /// Executes the Query and returns the first column of first row
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>null if no row returned from query execution. Otherwise value for first column</returns>
        public object ExecuteScalar(string query)
        {
            try
            {
                DbCommand cmd = GetDBCommand();
                cmd.Connection = conn;
                cmd.Transaction = trans;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;
                object o = cmd.ExecuteScalar();
                cmd.Dispose();
                return o == DBNull.Value ? null : o;
            }
            catch (Exception ex)
            {
                var message = "SQL Error: " + ex.Message;
                if (AppPress.Settings.developer)
                    message += "\nQuery:\n" + query;
                throw new Exception(message);
            }
        }
        /// <summary>
        /// Executes the Query and returns the first column of first row as int value
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>null if no row returned from query execution. Otherwise int value for first column</returns>
        public int? ExecuteInt(string query)
        {
            var o = ExecuteScalar(query);
            return o == null || o == DBNull.Value ? (int?)null : Convert.ToInt32(o);
        }

        private DbDataAdapter GetDBDataAdapter()
        {
            if (databaseType == DatabaseType.MySql)
                return new MySqlDataAdapter();
            if (databaseType == DatabaseType.SqlServer)
                return new SqlDataAdapter();
            throw new NotImplementedException();
        }
        private DbCommand GetDBCommand()
        {
            if (databaseType == DatabaseType.MySql)
                return new MySqlCommand();
            if (databaseType == DatabaseType.SqlServer)
                return new SqlCommand();
            throw new NotImplementedException();
        }

        public bool DBTableExists(string tableName)
        {
            var q = "";
            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                case DatabaseType.MySql:
                    q = @"SELECT Count(1)
                        FROM  Information_schema.Tables
                        WHERE table_schema = '" + dbName + @"' and table_name='" + tableName + @"'";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return ExecuteInt(q) == 1;
        }

        public bool DBRoutineExists(string routineName)
        {
            var q = "";
            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                case DatabaseType.MySql:
                    q = @"SELECT Count(1)
                        FROM  Information_schema.Routines
                        WHERE routine_schema = '" + dbName + @"' AND specific_name = '" + routineName + @"' AND Routine_Type = 'FUNCTION'";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return ExecuteInt(q) == 1;
        }

        public bool DBColumnExists(string tableName, string columName)
        {
            var q = "";
            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                case DatabaseType.MySql:
                    q = @"SELECT Count(1)
                        FROM  Information_schema.Columns
                        WHERE table_schema = '" + dbName + @"' and table_name='" + tableName + @"' and column_name='" + columName + @"'";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return ExecuteInt(q) == 1;
        }

        public bool DBViewExists(string viewName)
        {
            var q = "";
            switch (databaseType)
            {
                case DatabaseType.SqlServer:
                case DatabaseType.MySql:
                    q = @"SELECT Count(1)
                        FROM  INFORMATION_SCHEMA.VIEWS
                        WHERE table_schema = '" + dbName + @"' AND table_Name = '" + viewName + @"'";
                    break;
                default:
                    throw new NotImplementedException();
            }
            return ExecuteInt(q) == 1;
        }

        /// <summary>
        /// Executes the Query and returns the first column of first row as decimal value
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>null if no row returned from query execution. Otherwise decimal value for first column</returns>
        public decimal? ExecuteDecimal(string query)
        {
            var o = ExecuteScalar(query);
            return o == null || o == DBNull.Value ? (decimal?)null : Convert.ToDecimal(o);
        }
        /// <summary>
        /// Executes the Query and returns the first column of first row as string value
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>null if no row returned from query execution. Otherwise string value for first column</returns>
        public string ExecuteString(string query)
        {
            var o = ExecuteScalar(query);
            return o == null || o == DBNull.Value ? (string)null : Convert.ToString(o);
        }
        /// <summary>
        /// Executes the Query and returns the first column of first row as DateTime value
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>null if no row returned from query execution. Otherwise DateTime value for first column</returns>
        public DateTime? ExecuteDateTime(string query)
        {
            var o = ExecuteScalar(query);
            return o == null || o == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(o);
        }
        /// <summary>
        /// Executes the Query and returns the first column of returned rows as List of Int64 values
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>List of Int64 values</returns>
        public List<Int64> ExecuteInt64List(string query)
        {
            List<Int64> iList = new List<Int64>();
            IDataReader dr = ExecuteQuery(query);
            try
            {
                while (dr.Read())
                    iList.Add(dr.GetInt64(0));
            }
            catch (Exception ex)
            {
                var message = "SQL Error: " + ex.Message;
                if (AppPress.Settings.developer)
                    message += "\nQuery:\n" + query;
                throw new Exception(message);
            }
            finally
            {
                dr.Close();
            }
            return iList;
        }
        /// <summary>
        /// Executes the Query and returns the first column of returned rows as List of int values
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>List of int values</returns>
        public List<int> ExecuteIntList(string query)
        {
            List<int> iList = new List<int>();
            IDataReader dr = ExecuteQuery(query);
            try
            {
                while (dr.Read())
                    iList.Add(dr.GetInt32(0));
            }
            catch (Exception ex)
            {
                var message = "SQL Error: " + ex.Message;
                if (AppPress.Settings.developer)
                    message += "\nQuery:\n" + query;
                throw new Exception(message);
            }
            finally
            {
                dr.Close();
            }
            return iList;
        }
        /// <summary>
        /// Executes the Query and returns the first column of returned rows as List of int values
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>List of int values</returns>
        public List<DateTime> ExecuteDateTimeList(string query)
        {
            var iList = new List<DateTime>();
            var dr = ExecuteQuery(query);
            try
            {
                while (dr.Read())
                    iList.Add(dr.GetDateTime(0));
            }
            catch (Exception ex)
            {
                var message = "SQL Error: " + ex.Message;
                if (AppPress.Settings.developer)
                    message += "\nQuery:\n" + query;
                throw new Exception(message);
            }
            finally
            {
                dr.Close();
            }
            return iList;
        }
        /// <summary>
        /// Executes the Query and returns the first column of returned rows as List of string values
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>List of string values</returns>
        public List<string> ExecuteStringList(string query)
        {
            List<string> iList = new List<string>();
            IDataReader dr = ExecuteQuery(query);
            try
            {
                while (dr.Read())
                    iList.Add(Convert.ToString(dr[0]));
            }
            catch (Exception ex)
            {
                var message = "SQL Error: " + ex.Message;
                if (AppPress.Settings.developer)
                    message += "\nQuery:\n" + query;
                throw new Exception(message);
            }
            finally
            {
                dr.Close();
            }
            return iList;
        }
        /// <summary>
        /// Executes the Query and returns all columns of first row as List of object values
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>List of Object values, null if no row if returned</returns>
        public List<Object> ExecuteScalarList(string query)
        {
            var dr = ExecuteQuery(query);
            try
            {
                if (dr.Read())
                {
                    var iList = new List<Object>();
                    for (int i = 0; i < dr.FieldCount; ++i)
                        iList.Add(dr.IsDBNull(i) ? null : dr[i]);
                    return iList;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                var message = "SQL Error: " + ex.Message;
                if (AppPress.Settings.developer)
                    message += "\nQuery:\n" + query;
                throw new Exception(message);
            }
            finally
            {
                dr.Close();
            }
        }
        /// <summary>
        /// Executes the Query and returns all data as Array
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>List of Object values, null if no row if returned</returns>
        public List<List<Object>> ExecuteArrayList(string query)
        {
            var iiList = new List<List<Object>>();
            var dr = ExecuteQuery(query);
            try
            {
                while (dr.Read())
                {
                    var iList = new List<Object>();
                    for (int i = 0; i < dr.FieldCount; ++i)
                        iList.Add(dr.IsDBNull(i) ? null : dr[i]);
                    iiList.Add(iList);
                }
                return iiList;
            }
            catch (Exception ex)
            {
                var message = "SQL Error: " + ex.Message;
                if (AppPress.Settings.developer)
                    message += "\nQuery:\n" + query;
                throw new Exception(message);
            }
            finally
            {
                dr.Close();
            }
        }
        /// <summary>
        /// Executes a Query
        /// </summary>
        /// <param name="query">Query to Execute</param>
        /// <returns>data reader</returns>
        public IDataReader ExecuteQuery(string query)
        {

            try
            {
                IDataReader dr;
                DbCommand cmd;
                if (databaseType == DatabaseType.MySql)
                    cmd = new MySqlCommand(query, (MySqlConnection)conn, (MySqlTransaction)trans);
                else if (databaseType == DatabaseType.SqlServer)
                    cmd = new SqlCommand(query, (SqlConnection)conn, (SqlTransaction)trans);
                else
                    throw new NotImplementedException();
                dr = cmd.ExecuteReader();
                cmd.Dispose();
                return dr;
            }
            catch (Exception ex)
            {
                var message = "SQL Error: " + ex.Message;
                if (AppPress.Settings == null || AppPress.Settings.developer)
                    message += "<br/><b>Query:</b><br/>" + query;
                throw new Exception(message);
            }
        }


        /// <summary>
        /// Executes the Insert query and returns the primary key value for the new row which is inserted
        /// </summary>
        /// <param name="query">Insert query to execute</param>
        /// <param name="tableName">name of the table where row is being inserted</param>
        /// <returns>value of primary indentity column</returns>
        public Int64 ExecuteIdentityInsert(string query, string tableName)
        {
            try
            {
                DbCommand cmd = conn.CreateCommand();
                cmd.Transaction = trans;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;
                cmd.ExecuteNonQuery();
                cmd.CommandText = databaseType == DatabaseType.MySql ? "Select LAST_INSERT_ID()" : "Select SCOPE_IDENTITY()";
                // Int64 id = (Int64)cmd.ExecuteScalar();
                long id = Convert.ToInt64(cmd.ExecuteScalar());
                cmd.Dispose();
                return id;
            }
            catch (Exception ex)
            {
                var message = "SQL Error: " + ex.Message;
                if (ex.GetType() == typeof(MySqlException))
                {
                    var mex = (MySqlException)ex;
                    if (mex.Number == 1062) // Unique
                        throw ex;
                    if (mex.Number == 1451) // ForeignKey
                        throw ex;
                }
                else if (ex.GetType() == typeof(SqlException))
                {
                    var mex = (SqlException)ex;
                    if (mex.Number == 2601) // Unique
                        throw ex;
                    if (mex.Number == 547) // ForeignKey Failure
                        message += "\nThis maybe because the containing form is missing TableName property";
                }
                if (AppPress.Settings.developer)
                    message += "\nQuery:\n" + query;
                throw new Exception(message);
            }
        }

        internal bool CheckForeignKey(string FKTable, string FKColumn, string PKTable, string PKColumn)
        {
            if (!DBColumnExists(FKTable, FKColumn))
                return true;
            var query = GetForeignKeysQuery(PKTable, PKColumn);
            var dr = ExecuteQuery(query);
            try
            {
                while (dr.Read())
                    if (dr.GetString(0).Equals(FKTable, StringComparison.OrdinalIgnoreCase) && dr.GetString(1).Equals(FKColumn, StringComparison.OrdinalIgnoreCase))
                        return true;
                return false;
            }
            finally
            {
                dr.Close();
            }
        }

        internal string GetForeignKeysQuery(string TableName, string PrimaryKeyColumn)
        {
            string query;
            if (databaseType == DatabaseType.MySql)
                query = @"SELECT TABLE_NAME,COLUMN_NAME
                                FROM
                                    INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                                WHERE
                                    REFERENCED_TABLE_NAME = '" + TableName + @"' and REFERENCED_COLUMN_NAME = '" + PrimaryKeyColumn + "' and " + DAOBasic.SchemaColumnName + @"='" + dbName + "'";

            else
                query = @"
                                SELECT 
                                    tab1.name AS [table_name],
                                    col1.name AS [column_Name]
                                FROM sys.foreign_key_columns fkc
                                INNER JOIN sys.objects obj
                                    ON obj.object_id = fkc.constraint_object_id
                                INNER JOIN sys.tables tab1
                                    ON tab1.object_id = fkc.parent_object_id
                                INNER JOIN sys.schemas sch
                                    ON tab1.schema_id = sch.schema_id
                                INNER JOIN sys.columns col1
                                    ON col1.column_id = parent_column_id AND col1.object_id = tab1.object_id
                                INNER JOIN sys.tables tab2
                                    ON tab2.object_id = fkc.referenced_object_id
                                INNER JOIN sys.columns col2
                                    ON col2.column_id = referenced_column_id AND col2.object_id = tab2.object_id
                                WHERE tab2.name = '" + TableName + @"'
                                AND col2.name = '" + PrimaryKeyColumn + "'";
            return query;
        }

        #region private utility methods


        private static void AttachParameters(DbCommand command, DbParameter[] commandParameters)
        {
            foreach (DbParameter p in commandParameters)
            {
                //check for derived output value with no value assigned
                if ((p.Direction == ParameterDirection.InputOutput) && (p.Value == null))
                {
                    p.Value = DBNull.Value;
                }

                command.Parameters.Add(p);
            }
        }


        private static void AssignParameterValues(MySqlParameter[] commandParameters, object[] parameterValues)
        {
            if ((commandParameters == null) || (parameterValues == null))
            {
                //do nothing if we get no data
                return;
            }

            // we must have the same number of values as we pave parameters to put them in
            if (commandParameters.Length != parameterValues.Length)
            {
                throw new ArgumentException("Parameter count does not match Parameter Value count.");
            }

            //iterate through the MySqlParameters, assigning the values from the corresponding position in the 
            //value array
            for (int i = 0, j = commandParameters.Length; i < j; i++)
            {
                commandParameters[i].Value = parameterValues[i];
            }
        }
        private static void PrepareCommand(DbCommand command, DbConnection connection, DbTransaction transaction, CommandType commandType, string commandText, DbParameter[] commandParameters)
        {
            //if the provided connection is not open, we will open it
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            //associate the connection with the command
            command.Connection = connection;

            //set the command text (stored procedure name or SQL statement)
            command.CommandText = commandText;

            //if we were provided a transaction, assign it.
            if (transaction != null)
            {
                command.Transaction = transaction;
            }

            //set the command type
            command.CommandType = commandType;

            //attach the command parameters if they are provided
            if (commandParameters != null)
            {
                AttachParameters(command, commandParameters);
            }

            return;
        }


        #endregion private utility methods & constructors


        #region ExecuteScalar

        public object ExecuteScalar(CommandType commandType, string commandText, params MySqlParameter[] commandParameters)
        {
            //create & open a MySqlConnection, and dispose of it after we are done.
            using (MySqlConnection cn = new MySqlConnection(AppPress.Settings.ConnectionString))
            {
                cn.Open();

                //call the overload that takes a connection in place of the connection string
                return ExecuteScalar(cn, commandType, commandText, commandParameters);
            }
        }
        internal object ExecuteScalar(CommandType commandType, string commandText, params SqlParameter[] commandParameters)
        {
            //create & open a MySqlConnection, and dispose of it after we are done.
            using (SqlConnection cn = new SqlConnection(AppPress.Settings.ConnectionString))
            {
                cn.Open();

                //call the overload that takes a connection in place of the connection string
                return ExecuteScalar(cn, commandType, commandText, commandParameters);
            }
        }
        internal object ExecuteScalar(MySqlConnection connection, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            //create a command and prepare it for execution
            DbCommand cmd = GetDBCommand();
            PrepareCommand(cmd, connection, (DbTransaction)null, commandType, commandText, commandParameters);

            //execute the command & return the results
            object retval = cmd.ExecuteScalar();

            // detach the MySqlParameters from the command object, so they can be used again.
            cmd.Parameters.Clear();
            return retval;

        }
        internal object ExecuteScalar(SqlConnection connection, CommandType commandType, string commandText, params DbParameter[] commandParameters)
        {
            //create a command and prepare it for execution
            DbCommand cmd = GetDBCommand();
            PrepareCommand(cmd, connection, (DbTransaction)null, commandType, commandText, commandParameters);

            //execute the command & return the results
            object retval = cmd.ExecuteScalar();

            // detach the MySqlParameters from the command object, so they can be used again.
            cmd.Parameters.Clear();
            return retval;

        }
        #endregion ExecuteScalar

        /// <summary>
        /// Close the DAOBasic object. Should be called always and commended to call it in finally clause of try catch
        /// </summary>
        public void Close()
        {
            if (trans != null)
                throw new Exception("DAOBasic Closed without committing or rollback transaction");
            // trans.Dispose();

            if (conn != null)
            {
                conn.Close();
                conn.Dispose();
            }

        }

        internal static int TryGetOrdinal(IDataReader dr, string p)
        {
            for (int i = 0; i < dr.FieldCount; ++i)
                if (dr.GetName(i).Equals(p, StringComparison.OrdinalIgnoreCase))
                    return i;
            return -1;
        }
        public string EscapeSQLString(string value)
        {
            if (value == null)
                return null;
#if DB2
            return value.Replace("'", "''");
#else
            switch (databaseType)
            {
                case DatabaseType.MySql:
                    value = value.Replace("\\", "\\\\").Replace("'", "\\'");
                    break;
                case DatabaseType.SqlServer:
                    value = value.Replace("'", "''");
                    break;
                default:
                    throw new NotImplementedException("Database not supported");
            }
#endif
            return value;
        }
        internal string GetPrimaryKey(string tableName)
        {
            string q;
            if (databaseType == DatabaseType.SqlServer)
                q = @"SELECT column_name as primarykeycolumn
                        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC
                        INNER JOIN
                        INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                        ON TC.CONSTRAINT_TYPE = 'PRIMARY KEY' AND
                        TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME
                        and ku.table_name='" + tableName + @"'
                        ORDER BY KU.TABLE_NAME, KU.ORDINAL_POSITION";
            else
                q = @"SELECT `COLUMN_NAME`
                        FROM `information_schema`.`COLUMNS`
                        WHERE (`TABLE_SCHEMA` = '" + dbName + @"')
                          AND (`TABLE_NAME` = '" + tableName + @"')
                          AND (`COLUMN_KEY` = 'PRI');";
            return ExecuteString(q);
        }
    }

    public sealed class SqlHelperParameterCache
    {
        #region private methods, variables, and constructors

        //Since this class provides only static methods, make the default constructor private to prevent 
        //instances from being created with "new SqlHelperParameterCache()".
        private SqlHelperParameterCache() { }

        private static Hashtable paramCache = Hashtable.Synchronized(new Hashtable());

        private static MySqlParameter[] DiscoverSpParameterSet(string connectionString, string spName, bool includeReturnValueParameter)
        {
            using (MySqlConnection cn = new MySqlConnection(connectionString))
            using (MySqlCommand cmd = new MySqlCommand(spName, cn))
            {
                cn.Open();
                cmd.CommandType = CommandType.StoredProcedure;

                MySqlCommandBuilder.DeriveParameters(cmd);

                if (!includeReturnValueParameter)
                {
                    cmd.Parameters.RemoveAt(0);
                }

                MySqlParameter[] discoveredParameters = new MySqlParameter[cmd.Parameters.Count]; ;

                cmd.Parameters.CopyTo(discoveredParameters, 0);

                return discoveredParameters;
            }
        }
        //deep copy of cached MySqlParameter array
        private static MySqlParameter[] CloneParameters(MySqlParameter[] originalParameters)
        {
            MySqlParameter[] clonedParameters = new MySqlParameter[originalParameters.Length];

            for (int i = 0, j = originalParameters.Length; i < j; i++)
            {
                clonedParameters[i] = (MySqlParameter)((ICloneable)originalParameters[i]).Clone();
            }

            return clonedParameters;
        }
        #endregion private methods, variables, and constructors

        #region caching functions
        public static void CacheParameterSet(string connectionString, string commandText, params MySqlParameter[] commandParameters)
        {
            string hashKey = connectionString + ":" + commandText;

            paramCache[hashKey] = commandParameters;
        }

        public static MySqlParameter[] GetCachedParameterSet(string connectionString, string commandText)
        {
            string hashKey = connectionString + ":" + commandText;

            MySqlParameter[] cachedParameters = (MySqlParameter[])paramCache[hashKey];

            if (cachedParameters == null)
            {
                return null;
            }
            else
            {
                return CloneParameters(cachedParameters);
            }
        }
        #endregion caching functions

        #region Parameter Discovery Functions


        public static MySqlParameter[] GetSpParameterSet(string connectionString, string spName)
        {
            return GetSpParameterSet(connectionString, spName, false);
        }

        public static MySqlParameter[] GetSpParameterSet(string connectionString, string spName, bool includeReturnValueParameter)
        {
            string hashKey = connectionString + ":" + spName + (includeReturnValueParameter ? ":include ReturnValue Parameter" : "");

            MySqlParameter[] cachedParameters;

            cachedParameters = (MySqlParameter[])paramCache[hashKey];

            if (cachedParameters == null)
            {
                cachedParameters = (MySqlParameter[])(paramCache[hashKey] = DiscoverSpParameterSet(connectionString, spName, includeReturnValueParameter));
            }

            return CloneParameters(cachedParameters);
        }
        #endregion Parameter Discovery Functions

    }
}
