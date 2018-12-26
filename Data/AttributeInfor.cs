using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClass.Data
{
    /// <summary>
    /// 属性
    /// </summary>
    public class AttributeInfor
    {
        public PropertyInfo Info { get; set; }
        public DTFieldAttribute Attr { get; set; }
        public string GetDTFieldName()
        {
            if (Attr == null || string.IsNullOrEmpty(Attr.FieldName)) return Info.Name;
            else return Attr.FieldName;
        }
        public bool IsPrimaryKey()
        {
            return Attr != null && Attr.Usage == DBFieldUsage.PrimaryKey;
        }
    }
}
