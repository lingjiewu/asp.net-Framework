using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClass.Data
{
    /// <summary>
    /// 数据库连接的抽象类
    /// </summary>
    public abstract class DataBase : IDisposable
    {
        protected DBContext mContext;
        public DataBase(DBContext context)
        {
            mContext = context;
        }
        public abstract void Create(string connectionString, bool useTrans = false);

        /// <summary>
        /// 手动开始事务
        /// </summary>
        public abstract void BeginTransaction();

        public abstract DataTable QueryTable(string text, CommandType cmdType, params System.Data.IDbDataParameter[] parameters);
        public abstract DataTable QueryTable(string sql);
        public abstract IDataReader QueryReader(string sql);
        public abstract DataTable PartQuery(string select, string table, string where, string orderby, int start, int end);
        /// <summary>
        /// 是否有定义使用事务（只是申明使用，不一定正在使用）
        /// </summary>        
        public abstract bool UseTransaction();

        /// <summary>
        /// 是否正在事务中
        /// </summary>        
        public abstract bool InTransaction();

        public abstract int ExecuteCommand(string text, CommandType cmdType, params System.Data.IDbDataParameter[] parameters);
        public abstract int ExecuteCommand(string sql);

        public abstract IDbDataParameter CreateParameter(string name, object value);

        public abstract object ExecScalar(string text, CommandType cmdType, params System.Data.IDbDataParameter[] parameters);
        public abstract object ExecScalar(string sql);

        /// <summary>
        /// 自增标识
        /// </summary>
        public abstract bool AotoIdentity { get; }

        /// <summary>
        /// 自增语句，如果AotoIdentity=false则在insert的时候赋值给主键（比如oracle），否则是在插入语句执行后再查询（比如sqlserver）
        /// </summary>
        public abstract string GetIdentitySql(string tableName);

        public abstract void Commit();

        public abstract void Rollback();
        public abstract void Close();

        public abstract string GetLastSql();

        /// <summary>
        /// 获取转日期的语句表达式
        /// </summary>
        /// <returns></returns>
        public abstract string GetDateConverter(int length);

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }
    }
    public enum SqlLogMode
    {
        /// <summary>
        /// 不记录
        /// </summary>
        NoLog = 0,
        /// <summary>
        /// 记录除select之外的
        /// </summary>
        ExceptSelect = 1,
        /// <summary>
        /// 全部
        /// </summary>
        All = 2
    }
}
