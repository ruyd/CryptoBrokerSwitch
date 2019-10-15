using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Piggy
{
    public static class SqlFunctions
    {
        public static string Sql(this string s, bool textNull = true)
        {
            if (s == null)
                return textNull ? "NULL" : null;

            s = s.Trim();
            return $"'{s}'";
        }
        public static string Sql(this Guid? s, bool textNull = true)
        {
            if (s == null)
                return textNull ? "NULL" : null;
            return $"'{s}'";
        }
        public static string Sql(this DateTime? s, bool textNull = true)
        {
            if (s == null)
                return textNull ? "NULL" : null;
            return $"'{s}'";
        }
        public static string Sql(this bool? s, bool textNull = true)
        {
            if (s == null)
                return textNull ? "NULL" : null;
            return $"'{s}'";
        }
        public static string Sql(this int? s, bool textNull = true)
        {
            if (s == null)
                return textNull ? "NULL" : null;
            return $"'{s}'";
        }
        public static string ToSafeSqlLiteral(this string s, bool doUpper = true)
        {
            if (string.IsNullOrWhiteSpace(s))
                return s; 

            if (doUpper)
                s = s.ToUpper();

            s = s.Replace("'", "''");
            s = s.Replace("[", "[[]");
            s = s.Replace("%", "[%]");
            s = s.Replace("_", "[_]");
            s = s.Replace("execute", "BLOCKED");
            s = s.Replace("exec", "BLOCKED");
            s = s.Replace("EXECUTE", "BLOCKED");
            s = s.Replace("EXEC", "BLOCKED");
            s = s.Replace("xp_", "BLOCKED");
            s = s.Replace("?", "BLOCKED");
            s = s.Replace("--", "COMMENT");
            s = s.Replace("/*", "COMMENT");
            s = s.Replace("*/", "COMMENT");
            
            return s; 
        }

        public static int GetCount(string query, SqlConnection conn)
        {
            var raw = GetScalar(query, conn) as Int32?;
            if (raw == null)
                raw = 0;

            return raw.Value;
        }
        public static object GetScalar(string query, SqlConnection conn)
        {
            object result = null;
            var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandTimeout = 90000;

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                result = cmd.ExecuteScalar();

                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();
            }
            catch (Exception ex)
            {

            }

            return result;
        }


        public static int SprocExecNoParams(string spName, System.Data.Common.DbConnection conn)
        {
            var affected = -1;
            var cmd = conn.CreateCommand();
            cmd.CommandText = spName;
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandTimeout = 90000;

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                affected = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {

            }

            if (conn.State != System.Data.ConnectionState.Closed)
                conn.Close();

            return affected;
        }

        public static int ExecNoParams(string query, System.Data.Common.DbConnection conn)
        {
            var affected = -1;
            var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandTimeout = 90000;

            try
            {
                if (conn.State != System.Data.ConnectionState.Open)
                    conn.Open();

                affected = cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {

            }

            if (conn.State != System.Data.ConnectionState.Closed)
                conn.Close();

            return affected;
        }

        public static DataTable GetTable(string sql, SqlConnection conn)
        {
            var ds = new DataSet();

            try
            {
                var adp = new SqlDataAdapter(sql, conn);

                conn.Open();
                adp.Fill(ds);

                if (conn.State != System.Data.ConnectionState.Closed)
                    conn.Close();

            }
            catch (Exception ex)
            {

            }

            return ds.Tables[0];
        }

        public static void BulkInsertReplace(DataTable dt, SqlConnection conn, bool deleteExisting = true)
        {
            if (deleteExisting)
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM " + dt.TableName;
                cmd.Connection.Open();
                cmd.ExecuteNonQuery();
            }

            var bulkCopy = new SqlBulkCopy(conn);
            bulkCopy.BatchSize = 10000;
            bulkCopy.BulkCopyTimeout = 300;
            bulkCopy.DestinationTableName = dt.TableName;
            try
            {
                bulkCopy.WriteToServer(dt);
            }
            catch (Exception ex)
            {

            }

            if (conn.State == ConnectionState.Open)
                conn.Close();
        }

        public static bool RunScript(SqlConnection conn, string text)
        {
            var cmd = conn.CreateCommand();
            cmd.CommandTimeout = 655534;
            cmd.CommandText = text;
            cmd.CommandType = CommandType.Text; 

            try
            {
                var changed = cmd.ExecuteNonQuery();
                return true;
            }
            catch (Exception ex)
            {

                return false;
            }
        }
    }
}