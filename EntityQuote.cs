using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClass
{
    /// <summary>
    /// 数据库字段的用途。
    /// </summary>
    public enum DBFieldUsage
    {
        /// <summary>
        /// 未定义。
        /// </summary>
        None = 0x00,
        /// <summary>
        /// 用于主键。
        /// </summary>
        PrimaryKey = 0x01,
        /// <summary>
        /// 用于唯一键。
        /// </summary>
        UniqueKey = 0x02,
        /// <summary>
        /// 由系统控制该字段的值。
        /// </summary>
        BySystem = 0x04,
        /// <summary>
        /// 不是本张表的
        /// </summary>
        NoField = 0X08
    }
    /// <summary>
    /// 数据字段说明
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public class DTFieldAttribute : Attribute
    {
        public DBFieldUsage Usage { get; set; }
        string m_strFieldName;
        object m_defaultValue;
        
        /// <summary>
        /// 是否自增
        /// </summary>
        public bool Identity { get; set; }


        // 获取该成员映射的数据库字段名称。
        public string FieldName
        {
            get
            {
                return m_strFieldName;
            }
            set
            {
                m_strFieldName = value;
            }
        }

        // 获取该字段的默认值
        public object DefaultValue
        {
            get
            {
                return m_defaultValue;
            }
            set
            {
                m_defaultValue = value;
            }
        }
    }
    /// <summary>
    /// 数据类型
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class DataTableAttribute : Attribute
    {
        public string TableName { get; set; }
        public DataTableAttribute()
        {

        }
    }
}
