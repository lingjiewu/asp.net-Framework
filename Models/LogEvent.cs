using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClass.Models
{
    /// <summary>
    /// 日志内容
    /// </summary>
    [DataTable(TableName = "LogEvents")]
    public class LogEvent: IEntity
    {
        [DTField(Usage = DBFieldUsage.PrimaryKey)]
        public int Id { get; set; }
        public string SessionId { get; set; }

        public int UserId { get; set; }

        public string Message { get; set; }
        public string ModuleName { get; set; }
        public DateTime CreateTime { get; set; }

    }
}
