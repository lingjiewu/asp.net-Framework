using ProjectClass.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClass.Data
{
    public class DBContext : IDisposable
    {
        private static DBContextConfig GlobalDBContextConfig;
        /// <summary>
        /// 日志数据库的配置，空则与默认的相同
        /// </summary>
        public static DBContextConfig LogDBConfig { get; set; }
        /// <summary>
        /// 载入配置
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="config"></param>
        public static void LoadConfigFromFile(string filePath, DBContextConfig config)
        {
            string json = ReadFile(filePath);
            GlobalDBContextConfig = config;
            if (LogDBConfig == null) LogDBConfig = config;
            //if (string.IsNullOrEmpty(json)) queryMapping = new Dictionary<string, QueryEntityConfig>();
            //else queryMapping = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, QueryEntityConfig>>(json);
        }
        public DBContext()
        {
            InitDBContext(false, null, null, null);
        }
        /// <summary>
        /// 创建实体
        /// </summary>
        /// <param name="dbType">数据库类型(sqlserver,mysql,oracle)</param>
        /// <param name="conn">连接串</param>
        /// <param name="formater">实体类全名的格式化，空则使用全局的</param>
        public DBContext(string dbType, string conn, string formater)
        {
            InitDBContext(false, dbType, conn, formater);
        }
        private void InitDBContext(bool useTrans, string dbType, string conn, string formater)
        {
            if (dbType != null || conn != null || formater != null)
            {
                currentDBContextConfig = new DBContextConfig
                {
                    ConnectionString = conn == null ? GlobalDBContextConfig.ConnectionString : conn,
                    DatabaseType = dbType == null ? GlobalDBContextConfig.DatabaseType : dbType,
                    EntityTypeFormat = formater == null ? GlobalDBContextConfig.EntityTypeFormat : formater
                };
            }
            if (GlobalDBContextConfig == null) throw new Exception("在创建实例前,请先使用LoadConfigFromFile初始化DBContext");
            LogSql = GlobalDBContextConfig.LogSql;
            this.useTran = useTrans;
            //if (defaultMembership != null || membership != null) Membership.OnDBContextInit(this);
        }
        public static string ReadFile(string file)
        {

            try
            {
                System.IO.StreamReader reader = new System.IO.StreamReader(file, Encoding.UTF8);
                string str = reader.ReadToEnd();
                reader.Close();
                return str;
            }
            catch (Exception ex)
            {
                throw new ClientException("读取页面权限配置文件出错" + ex.Message);
            }

        }

        /// <summary>
        /// 记录sql语句的方式，0：不记录，1：记录除select之外的，2：全部，实例化的时候会使用全局配置
        /// </summary>
        public SqlLogMode LogSql { get; set; }
        /// <summary>
        /// 记录sql到日志
        /// <param name="isQuery">是否为查询语句</param>
        /// </summary>
        public void LogSqlMessage(string sql, bool isQuery)
        {
            bool isLog = false;
            switch (LogSql)
            {
                case SqlLogMode.All:
                    isLog = true;
                    break;
                case SqlLogMode.ExceptSelect:
                    isLog = !isQuery;
                    break;
            }
            if (isLog) LogMessage(sql, "SQL");
            //else System.Diagnostics.Debug.WriteLine(sql);
        }
        public void LogMessage(string message, string moduleName = "系统")
        {
            //先添加到队列，后台去保存日志
            LogEvent log = new LogEvent() { Message = message, ModuleName = moduleName, CreateTime = DateTime.Now };
            //if (LoginUser != null) log.UserId = LoginUser.UserId;
            lock (MessageQueue)
            {
                MessageQueue.Enqueue(log);
            }
#if DEBUG
            System.Diagnostics.Debug.WriteLine(message);
#endif
            triggerLogHandle();
        }
        public static Queue<LogEvent> MessageQueue = new Queue<LogEvent>();
        static System.Threading.Timer timer;
        public void triggerLogHandle()
        {
            if (timer == null)
            {
                timer = new System.Threading.Timer(new System.Threading.TimerCallback(SaveEventLogHandle), null, 10000, 0);
            }
        }
        public void SaveEventLogHandle(object o)
        {
            //if (timer == null || MessageQueue.Count == 0) return;
            if (timer == null) return;
            List<LogEvent> lst;
            lock (MessageQueue)
            {
                lst = new List<LogEvent>();
                foreach (LogEvent e in MessageQueue) lst.Add(e);
                MessageQueue.Clear();
            }
            foreach (LogEvent log in lst)
            {
                log4net.ILog loger = log4net.LogManager.GetLogger(log.ModuleName == null ? "系统" : log.ModuleName);
                if (log.ModuleName == null) loger.Info(log.Message);
                else loger.Info("[" + log.ModuleName + "] " + log.Message);
            }

            if (DBContext.LogDBConfig != null)
            {
                using (DBContext db = new DBContext(DBContext.LogDBConfig.DatabaseType, DBContext.LogDBConfig.ConnectionString, null))
                {
                    db.LogSql = SqlLogMode.NoLog;
                    foreach (LogEvent log in lst)
                    {
                        if (log == null) continue;
                        try
                        {
                            db.SaveEntity(log);
                        }
                        catch (Exception ex)
                        {
                            db.LogException(ex, "日志", false);
                        }

                    }
                }
            }

            timer = null;
        }
        /// <summary>
        /// 记录异常Log4Net
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="appendMsg"></param>
        public void LogException(Exception ex, string appendMsg = null, bool logDb = true)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(appendMsg == null ? "system" : appendMsg);
            if (ex != null)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                log.Error(ex.Message, ex);
            }
            else log.Error(appendMsg);
            if (logDb) LogMessage(ex.Message, appendMsg == null ? "异常" : appendMsg);
        }
        public DBContextConfig currentDBContextConfig;
        public DBContextConfig GetConfig()
        {
            DBContextConfig config = currentDBContextConfig == null ? GlobalDBContextConfig : currentDBContextConfig;
            if (config == null) throw new ClientException("没有相关配置，全局请使用DBContext.LoadConfig");
            return config;
        }
        DataBase m_dbAccess;
        private bool useTran = false;
        DataBase GetDBAccess()
        {
            if (m_dbAccess == null)
            {
                switch (GetConfig().DatabaseType.ToLower())
                {
                    case "mysql":
                    case "mysql.data.mysqlclient":
                        //m_dbAccess = new Sql.MySqlAccess(this);
                        break;
                    case "oracle":
                        //m_dbAccess = new SqlClient.OracleAccess(this);
                        break;
                    default:
                        m_dbAccess = new sql.SqlAccess(this);
                        break;
                }
                m_dbAccess.Create(GetConfig().ConnectionString, this.useTran);
            }
            return m_dbAccess;
        }


        #region DBACCESS



        /// <summary>
        /// 手动开启事务，用于有事务和没事务混合的过程，如果整个访问过程都在一个事务，请使用DBContext(True)
        /// 如果前面有事务，会提交
        /// </summary>
        public void BeginTransaction()
        {
            useTran = true;
            GetDBAccess().BeginTransaction();
        }
        public DataTable QueryTable(string text, CommandType cmdType, params IDbDataParameter[] parameters)
        {
            return GetDBAccess().QueryTable(text, cmdType, parameters);
        }
        public DataTable QueryTable(string sql)
        {
            return GetDBAccess().QueryTable(sql);
        }

        public IDataReader QueryReader(string sql)
        {
            return GetDBAccess().QueryReader(sql);
        }

        /// <summary>
        /// 分页查询
        /// </summary>
        /// <param name="select"></param>
        /// <param name="table"></param>
        /// <param name="where"></param>
        /// <param name="orderby"></param>
        /// <param name="start">起始位置，从1开始</param>
        /// <param name="end">结束位置</param>
        /// <returns></returns>
        public DataTable PartQuery(string select, string table, string where, string orderby, int start, int end)
        {
            return GetDBAccess().PartQuery(select, table, where, orderby, start, end);
        }

        public int ExecuteCommand(string text, CommandType cmdType, params IDbDataParameter[] parameters)
        {
            return GetDBAccess().ExecuteCommand(text, cmdType, parameters);
        }
        public int ExecuteCommand(string sql)
        {
            return GetDBAccess().ExecuteCommand(sql);
        }

        public object ExecScalar(string text, CommandType cmdType, params IDbDataParameter[] parameters)
        {
            return GetDBAccess().ExecScalar(text, cmdType, parameters);
        }

        /// <summary>
        /// 执行语句返回第一行第一列，如果是空则为DBNull.Value(注意：不是null)
        /// </summary>
        /// <param name="sql"></param>
        /// <returns>第一行第一列值</returns>
        public object ExecScalar(string sql)
        {
            return GetDBAccess().ExecScalar(sql);
        }

        /// <summary>
        /// 查询某个字段的值
        /// </summary>        
        /// <param name="fieldName">字段名</param>
        /// <param name="primaryKey">主键值</param>
        /// <returns>如果字段为空则返回null(注意：不是DBNull.Value)</returns>
        public object ExecScalar<T>(string fieldName, int primaryKey)
        {
            Type entityType = typeof(T);
            //QueryParam param = new QueryParam() { entityType = entityType, permission = null, queryField = fieldName };
            //CustomerProperty attr = GetPrimary(param.entityType);
            //param.whereJson = "{" + attr.Info.Name + ":'" + primaryKey + "'}";
            //SetQueryEntityConfig(param);
            string sql = "";
            //ParseQuery(queryInstance, param.entityType, out sql, false);
            object scalar = GetDBAccess().ExecScalar(sql);
            if (scalar == DBNull.Value) return null;
            return scalar;
        }

        /// <summary>
        /// 查询某个字段的值，如果字段为空则返回null(注意：不是DBNull.Value)
        /// </summary>        
        /// <param name="fieldName">字段名</param>
        /// <param name="primaryKey">主键值</param>
        public object ExecScalar<T>(string fieldName, object where, string throwNull)
        {
            return ExecScalar<T>(null, fieldName, where, throwNull);
        }

        /// <summary>
        /// 查询某个字段的值,如果字段为空则返回null(注意：不是DBNull.Value)
        /// </summary>   
        /// <param name="fieldName">字段名</param>
        /// <param name="where">查询条件动态类</param>
        /// <param name="throwNull">如果空，抛出的异常信息</param>
        public object ExecScalar<T>(string permission, string fieldName, object where, string throwNull)
        {
            Type entityType = typeof(T);
            //QueryParam param = new QueryParam() { entityType = entityType, permission = permission, queryField = fieldName };
            //CustomerProperty attr = GetPrimary(param.entityType);
            //param.SetWhere(where);
            //SetQueryEntityConfig(param);
            string sql = "";
            //ParseQuery(queryInstance, param.entityType, out sql, false);
            object scalar = GetDBAccess().ExecScalar(sql);
            if (scalar == null || scalar == DBNull.Value)
            {
                if (throwNull != null) throw new ClientException(throwNull);
                return null;
            }
            return scalar;
        }

        /// <summary>
        /// 创建参数
        /// </summary>
        /// <returns></returns>
        public IDbDataParameter CreateParameter(string name, object value)
        {
            return GetDBAccess().CreateParameter(name, value);
        }

        /// <summary>
        /// 关闭连接，如果有事务则提交
        /// </summary>
        /// <returns></returns>
        public void Close()
        {
            GetDBAccess().Close();
        }

        /// <summary>
        /// 是否要在事务中
        /// </summary>
        public void RequireTransact(bool require = true)
        {
            if (require != useTran)
            {
                throw new ClientException("该过程" + (require ? "必需" : "不允许") + "在事务中执行");
            }
        }

        public void Rollback()
        {
            useTran = false;
            GetDBAccess().Rollback();
        }
        public void Commit()
        {
            useTran = false;
            GetDBAccess().Commit();
        }

        #endregion
        
        /// <summary>
        /// 保存实体到数据库操作
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="rootParseInfo">主表则为空，子表则执行下带入</param>
        /// <param name="filterPropertys">过滤的(需要保存和更新的)属性列表</param>
        private int SaveEntity(IEntity obj, string[] filterPropertyArr = null)
        {
            
            Type entityType = obj.GetType();
            ParseInfo parseInfo = ParseCustomerAttr(entityType, obj);
            //if (parseInfo.proListExcludePrimary.Count == 0 ) throw new ClientException("没有要操作的列");
            if (parseInfo.editFlag == 0) return -1;
            int effectCount = -1;
            if (string.IsNullOrEmpty(parseInfo.primaryKey) || parseInfo.primaryCusPro == null) throw new ClientException(obj.GetType().Name + "的DBTableAttribute属性没有定义主键");
            Dictionary<string, ParseInfo.ValueCustomerProperty> filterPropertys;
            if (filterPropertyArr == null) filterPropertys = parseInfo.proListExcludePrimary;
            else
            {
                filterPropertys = new Dictionary<string, ParseInfo.ValueCustomerProperty>();
                foreach (string proName in filterPropertyArr)
                {
                    if (parseInfo.proListExcludePrimary.ContainsKey(proName))
                    {
                        filterPropertys.Add(proName, parseInfo.proListExcludePrimary[proName]);
                    }

                }
            }
            string sql = "";
            if (filterPropertys.Count > 0)
            {
                bool isNew = obj.ValueIsNull(parseInfo.primaryCusPro.Info) || parseInfo.primaryKeyVal.Equals(0);
                List<IDbDataParameter> parameters = new List<IDbDataParameter>();
                if (isNew)
                {
                    sql = "insert into " + parseInfo.tableAttr.TableName + " (";
                    var customIncream = !GetDBAccess().AotoIdentity || !parseInfo.primaryCusPro.Attr.Identity;
                    //如果不支持自动增长，或者主键没显示定义自动增长
                    int idetity;
                    if (customIncream)
                    {
                        //if (DBContext.UserRedis)
                        //{
                        //    //如果使用redis缓存 ，则使得缓存来实现自增
                        //    string key = parseInfo.tableAttr.TableName + "_identity";
                        //    //idetity = (int)IncrementRedisItem(key);
                        //    if (idetity < 2)
                        //    {
                        object scalar = ExecScalar("select max(" + parseInfo.primaryKey + ") from " + parseInfo.tableAttr.TableName);
                        if (scalar == null || scalar == DBNull.Value) idetity = 1;
                        else idetity = (int)scalar;
                               idetity++;
                        //        SetRedisItem<int>(key, idetity);
                        //    }
                        //}
                        //else
                        //{
                        //if (GetDBAccess().AotoIdentity)
                        //{
                        //    throw new ClientException(GetDBAccess().GetType().Name + "没有实现非redis的自增");
                        //}
                        //使用数据库的自增（GetIdentitySql）
                        //sql += parseInfo.primaryKey + ",";
                        //object scalar = GetDBAccess().ExecScalar(GetDBAccess().GetIdentitySql(parseInfo.tableAttr.TableName), CommandType.Text);
                        //if (scalar == null || scalar == DBNull.Value) throw new ClientException("自动获取主键失败");
                        //idetity = Convert.ToInt32(scalar);
                        //}
                        parseInfo.primaryKeyVal = idetity;
                        parseInfo.primaryCusPro.Info.SetValue(obj, idetity, null);
                        sql += parseInfo.primaryKey + ",";
                    }

                    foreach (KeyValuePair<string, ParseInfo.ValueCustomerProperty> kv in filterPropertys) sql += kv.Value.CusProperty.GetDTFieldName() + ",";
                    sql = sql.Substring(0, sql.Length - 1);
                    sql += ") values(";
                    if (customIncream)
                    {
                        IDbDataParameter parameter = GetDBAccess().CreateParameter(parseInfo.primaryCusPro.GetDTFieldName(), parseInfo.primaryKeyVal);
                        sql += parameter.ParameterName + ",";
                        parameters.Add(parameter);
                    }
                    foreach (KeyValuePair<string, ParseInfo.ValueCustomerProperty> kv in filterPropertys)
                    {

                        // FormatValueForDB(kv.Key.Info, kv.Value)
                        IDbDataParameter parameter = GetDBAccess().CreateParameter(kv.Value.CusProperty.GetDTFieldName(), kv.Value.PropertyValue);
                        sql += parameter.ParameterName + ",";
                        parameters.Add(parameter);
                    }
                    sql = sql.Substring(0, sql.Length - 1);
                    sql += ")";
                    if (!customIncream && parseInfo.primaryCusPro.Info.PropertyType == typeof(int))
                    {
                        sql += ";" + GetDBAccess().GetIdentitySql(parseInfo.tableAttr.TableName);
                        object scalar = GetDBAccess().ExecScalar(sql, CommandType.Text, parameters.ToArray());
                        if (scalar == null || scalar == DBNull.Value) throw new ClientException("自动获取主键失败");
                        parseInfo.primaryKeyVal = Convert.ToInt32(scalar);
                        parseInfo.primaryCusPro.Info.SetValue(obj, Convert.ToInt32(scalar), null);
                    }
                    else
                    {
                        effectCount = GetDBAccess().ExecuteCommand(sql, CommandType.Text, parameters.ToArray());
                    }
                }
            }
            object mainPrimaryVal = parseInfo.primaryCusPro.Info.GetValue(obj, null);
            return effectCount;
        }
        public class ParseInfo
        {
            public DataTableAttribute tableAttr;
            public AttributeInfor primaryCusPro;
            public string primaryKey;
            public object primaryKeyVal;
            public int editFlag = 1;
            public Dictionary<string, ValueCustomerProperty> proListExcludePrimary = new Dictionary<string, ValueCustomerProperty>();
            public Dictionary<string, object> proItemsList = new Dictionary<string, object>();
            /// <summary>
            /// 子元素（List）的集合
            /// </summary>
            public Dictionary<string, Type> proItemsDefList = new Dictionary<string, Type>();
            /// <summary>
            /// 带属性Property值的CustomerProperty
            /// </summary>
            public class ValueCustomerProperty
            {
                public AttributeInfor CusProperty { get; set; }
                public object PropertyValue { get; set; }
            }
        }
        /// <summary>
        /// 获取实体的表定义
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static DataTableAttribute GetTableAttr(Type t, bool useEntityNameWhenNull = true)
        {
            if (t == null) throw new ClientException("找不到该类型的实体");
            object[] attrs = t.GetCustomAttributes(typeof(DataTableAttribute), true);
            if (attrs.Length < 1)
            {
                return new DataTableAttribute() { TableName = useEntityNameWhenNull ? t.Name : null };
            }
            DataTableAttribute attr = (DataTableAttribute)attrs[0];
            if (attr.TableName == null && useEntityNameWhenNull) attr.TableName = t.Name;
            return attr;
        }
        private AttributeInfor getProCustomerAttr(Type entityType, PropertyInfo proInfo)
        {
            AttributeInfor cus = GetCustomerProperty(entityType, proInfo.Name);
            if (cus == null)
            {
                //获取自定义属性
                object[] fieldAttrs = proInfo.GetCustomAttributes(typeof(DTFieldAttribute), true);
                DTFieldAttribute attrField = null;
                if (fieldAttrs.Length > 0)//有自定
                {
                    attrField = fieldAttrs[0] as DTFieldAttribute;
                }
                return new AttributeInfor() { Info = proInfo, Attr = attrField };
            }
            else return cus;
        }
        /// <summary>
        /// 获取类型的某个属性及自定义属性
        /// </summary>
        /// <param name="t">类型</param>
        /// <param name="name">属性名</param>
        /// <returns></returns>
        public static AttributeInfor GetCustomerProperty(Type t, string name)
        {
            foreach (AttributeInfor per in GetTypeCustomerPropertys(t))
            {
                if (per.Info.Name == name) return per;
            }
            return null;
        }
        static Dictionary<Type, List<AttributeInfor>> typeCustomerPropertyDic = new Dictionary<Type, List<AttributeInfor>>();
        /// <summary>
        /// 获取实体类型的自定义属性集合
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static List<AttributeInfor> GetTypeCustomerPropertys(Type t)
        {
            List<AttributeInfor> lst;
            if (!typeCustomerPropertyDic.TryGetValue(t, out lst))
            {
                lst = new List<AttributeInfor>();
                foreach (PropertyInfo proInfo in t.GetProperties())
                {
                    object[] fieldAttrs = proInfo.GetCustomAttributes(typeof(DTFieldAttribute), true);
                    if (fieldAttrs.Length > 0) lst.Add(new AttributeInfor() { Info = proInfo, Attr = fieldAttrs[0] as DTFieldAttribute });
                    else lst.Add(new AttributeInfor() { Info = proInfo });
                }
                if (lst.Count == 0) throw new ClientException(t.Name + "没有定义任何属性");
                typeCustomerPropertyDic.Add(t, lst);
            }
            return lst;
        }
        private static Type GetIDBEntityListItemType(Type proinfoType)
        {
            Type[] typeArr = proinfoType.GetGenericArguments();
            if (typeArr.Length > 0 && typeArr[0].IsSubclassOf(typeof(IEntity)))
                return typeArr[0];
            else return null;
        }
        private ParseInfo ParseCustomerAttr(Type entityType, IEntity entityObj)
        {
            ParseInfo parseInfo = new ParseInfo();
            parseInfo.tableAttr = GetTableAttr(entityType);
            foreach (PropertyInfo proInfo in entityType.GetProperties())
            {
                if (proInfo.GetGetMethod() == null) continue;
                AttributeInfor cusPro = getProCustomerAttr(entityType, proInfo);
                if (cusPro.Attr != null && cusPro.Attr.Usage == DBFieldUsage.NoField) continue;
                object proVal = entityObj == null ? null : proInfo.GetValue(entityObj, null);
                string fieldName = cusPro.GetDTFieldName();
                if (cusPro.IsPrimaryKey())
                {
                    parseInfo.primaryCusPro = cusPro;
                    parseInfo.primaryKey = fieldName;
                    //parseInfo.primaryKeyPro = proInfo;
                    parseInfo.primaryKeyVal = proVal;
                    continue;
                }
                Type proinfoType = proInfo.PropertyType;
                if (proinfoType.BaseType.Name.IndexOf("List") > -1) proinfoType = proinfoType.BaseType;//对自定义集合类的支持，需要继承List<T>
                switch (proinfoType.Namespace)
                {
                    case "System"://简单类型                        
                        if (proVal != null && !entityObj.ValueIsNull(proInfo))
                        {
                            if (proInfo.Name.ToLower() == "editflag")
                            {
                                if (proinfoType.Name != "Int32") throw new ClientException("EditFlag属性必需为int值");
                                parseInfo.editFlag = Convert.ToInt32(proVal);
                            }
                            else
                            {

                                parseInfo.editFlag = 1;
                                parseInfo.proListExcludePrimary.Add(cusPro.Info.Name, new ParseInfo.ValueCustomerProperty() { CusProperty = cusPro, PropertyValue = proVal });
                            }
                        }
                        else
                        {
                            //值为空，看是否开启为空也保存
                            if (cusPro.Attr != null && cusPro.Attr.Usage == DBFieldUsage.NoField) break;
                            //if (queryInstance.Config.IgnoreNull != null)
                            //{
                            //    foreach (string ignoreField in queryInstance.Config.IgnoreNull)
                            //    {
                            //        if (ignoreField != null && ignoreField.Equals(proInfo.Name))
                            //        {
                            //            parseInfo.proListExcludePrimary.Add(cusPro.Info.Name, new ParseInfo.ValueCustomerProperty() { CusProperty = cusPro, PropertyValue = proVal });
                            //            break;
                            //        }
                            //    }
                            //}
                        }
                        break;
                    case "System.Collections.Generic":
                        Type type = GetIDBEntityListItemType(proinfoType);
                        if (type != null)
                        {
                            if (proVal != null) parseInfo.proItemsList.Add(proInfo.Name, proVal);
                            parseInfo.proItemsDefList.Add(proInfo.Name, type);
                        }
                        break;
                    default:
                        if (proinfoType.IsSubclassOf(typeof(IEntity))) parseInfo.proItemsDefList.Add(proInfo.Name, proinfoType);
                        break;
                }
            }
            return parseInfo;
        }
        /// <summary>
        /// 释放，如果有未提交的事务，则rollback，有未关闭的连接则close
        /// </summary>
        public void Dispose()
        {
            GetDBAccess().Dispose();
        }
    }
}
