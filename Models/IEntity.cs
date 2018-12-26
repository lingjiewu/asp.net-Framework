using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClass.Models
{
    public class IEntity
    {
        /// <summary>
        /// 代表int空值，当数据库该字段可为空，或者需要序列化为null传给前端时，需要添加标记
        /// </summary> 
        public const int DefaultNullValue = -999999;
        /// <summary>
        /// 属性值是否为空
        /// </summary>
        /// <param name="proInfo">属性</param>        
        public bool ValueIsNull(PropertyInfo proInfo)
        {
            object obj = proInfo.GetValue(this, null);
            if (obj == null) return true;
            object[] DefaultValueAtts = proInfo.GetCustomAttributes(typeof(DefaultValueAttribute), true);
            if (DefaultValueAtts != null && DefaultValueAtts.Length > 0)
            {
                DefaultValueAttribute attr = (DefaultValueAttribute)DefaultValueAtts[0];
                if (obj.Equals(Convert.ChangeType(attr.Value, proInfo.PropertyType))) return true;
            }
            else
            {
                switch (proInfo.PropertyType.Name)
                {
                    case "Int32":
                        return obj.Equals(DefaultNullValue);
                    case "Int64":
                        return obj.Equals((Int64)DefaultNullValue);
                    case "Single":
                        return obj.Equals(Convert.ToSingle(DefaultNullValue));
                    case "Double":
                        return obj.Equals(Convert.ToDouble(DefaultNullValue));
                    case "Decimal":
                        return obj.Equals(Convert.ToDecimal(DefaultNullValue));
                    case "DateTime":
                        return obj.Equals(DateTime.MinValue);
                    default:
                        return false;
                }
            }
            return false;
        }
    }
}
