using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClass.Data.sql
{
    class SqlAccess : DataBase
    {
        public SqlAccess(DBContext context)
            : base(context)
        {

        }
        public override void Create(string connectionString, bool useTrans = false)
        {
            conStr = connectionString;
            this.useTrans = useTrans;
        }
        public override void BeginTransaction()
        {
            if (this.useTrans && trans != null) { trans.Commit(); }
            if (conn != null && conn.State == ConnectionState.Open) conn.Close();
            this.useTrans = false;
            OpenSql();
            this.useTrans = true;
            trans = conn.BeginTransaction();
            mContext.LogSqlMessage("DataBase BeginTransaction", true);
        }

        /// <summary>
        /// 是否有定义使用事务（只是申明使用，不一定正在使用）
        /// </summary>  
        public override bool UseTransaction()
        {
            return useTrans;
        }
        /// <summary>
        /// 是否正在事务中
        /// </summary>   
        public override bool InTransaction()
        {
            return useTrans && trans != null;
        }
        public override DataTable QueryTable(string sql)
        {
            return QueryTable(sql, CommandType.Text);
        }
        public override IDataReader QueryReader(string sql)
        {
            cmd = null;
            if (reader != null && !reader.IsClosed) throw new ClientException("有已打开的Reader，请先调用reader.close方法关闭");
            setCommand(sql, null, true);
            reader = cmd.ExecuteReader();
            cmd = null;
            return reader;
        }
        string lastSql;
        bool useTrans = false;
        SqlConnection conn;
        SqlTransaction trans;
        SqlCommand cmd;
        SqlDataReader reader;
        string conStr;

        private void OpenSql()
        {
            if (conn == null) conn = new SqlConnection(conStr);
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
                mContext.LogSqlMessage("**********************DataBase Open**********************", true);
                if (useTrans)
                {
                    BeginTransaction();
                }
            }
        }

        private void setCommand(string sql, System.Data.IDbDataParameter[] parameters, bool query)
        {
            if (conn == null) OpenSql();
            if (cmd == null) cmd = new SqlCommand(sql, conn);
            else
            {
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;
            }
            cmd.Parameters.Clear();
            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
                sql += "(";
                foreach (SqlParameter parameter in parameters)
                {
                    sql += parameter.ParameterName + "=";
                    if (parameter.Value != null && parameter.Value.ToString().Length > 255) sql += "长内容";
                    else sql += parameter.Value;
                    sql += ",";
                }
                sql = sql.Substring(0, sql.Length - 1) + ")";
            }
            if (useTrans) cmd.Transaction = trans;
            lastSql = sql;
            mContext.LogSqlMessage(sql, query);
        }

        public override int ExecuteCommand(string sql)
        {
            // 枚举 CommandType.text =1 , （sql语句） CommandType.StoredProcedure = 4,    （存储过程的名称）   CommandType.TableDirect = 512（表名）,
            return ExecuteCommand(sql, CommandType.Text);
        }

        public override object ExecScalar(string sql)
        {
            return ExecScalar(sql, CommandType.Text);
        }

        public override IDbDataParameter CreateParameter(string name, object value)
        {
            return new SqlParameter("@" + name, value);
        }

        public override bool AotoIdentity
        {
            get { return true; }
        }

        public override string GetIdentitySql(string tableName)
        {
            return "select @@identity";
        }

        public override void Dispose()
        {
            if (conn != null)
            {
                if (conn.State == ConnectionState.Open)
                {
                    if (trans != null)
                    {
                        trans.Rollback();
                        mContext.LogSqlMessage("Rollback", true);
                    }
                    if (reader != null && !reader.IsClosed) reader.Close();
                    conn.Close();
                    mContext.LogSqlMessage("-----------------------DataBase Close-----------------------", true);
                }
                if (cmd != null) cmd.Dispose();
                conn.Dispose();
                conn = null;
                cmd = null;
            }
        }


        public override string GetLastSql()
        {
            return lastSql;
        }

        public override void Commit()
        {
            if (conn != null && conn.State == ConnectionState.Open)
            {
                if (trans != null && useTrans)
                {
                    trans.Commit(); trans = null;
                    mContext.LogSqlMessage("DataBase Commit", true);
                }
            }
        }
        public override void Rollback()
        {
            if (conn != null && conn.State == ConnectionState.Open)
            {
                if (trans != null && useTrans)
                {
                    trans.Rollback(); trans = null;
                    mContext.LogSqlMessage("DataBase Rollback", true);
                }
            }
        }
        public override void Close()
        {
            if (conn != null && conn.State == ConnectionState.Open)
            {
                if (reader != null && !reader.IsClosed) reader.Close();
                if (trans != null && useTrans)
                {
                    trans.Commit(); trans = null;
                    mContext.LogSqlMessage("DataBase Commit", true);
                }
            }
            Dispose();
        }


        public override DataTable PartQuery(string select, string table, string where, string orderby, int start, int end)
        {
            if (orderby == null) throw new ArgumentNullException("pagger query order by");
            string sql;
            if (start == 1 && false)
            {
                //第一页的查询使用top (这里不应用，因为row_number跟order排序出来不一样，会照成第一页的数据不准)
                sql = "select top " + (end - start + 1) + "  " + select + " from " + table;
                if (!string.IsNullOrEmpty(where)) sql += " where " + where;
                sql += " order by " + orderby;
            }
            else
            {
                sql = "select * from (select row_number() over(order by " + orderby + ") rownum, " + select + " from " + table;
                if (!string.IsNullOrEmpty(where)) sql += " where " + where;
                sql += ") t where rownum between " + start + " and " + end;
            }
            return QueryTable(sql);
        }


        public override DataTable QueryTable(string text, CommandType cmdType, params System.Data.IDbDataParameter[] parameters)
        {
            setCommand(text, parameters, true);
            cmd.CommandType = cmdType;
            System.Data.DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public override int ExecuteCommand(string text, CommandType cmdType, params IDbDataParameter[] parameters)
        {
            setCommand(text, parameters, false);
            cmd.CommandType = cmdType;
            return cmd.ExecuteNonQuery();
        }

        public override object ExecScalar(string text, CommandType cmdType, params System.Data.IDbDataParameter[] parameters)
        {
            setCommand(text, parameters, text.IndexOf("insert", StringComparison.OrdinalIgnoreCase) == -1);
            return cmd.ExecuteScalar();
        }

        public override string GetDateConverter(int length)
        {
            return "Convert(varchar(" + length + "),{0},120)";
        }
    }
}
