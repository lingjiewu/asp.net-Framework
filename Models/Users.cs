using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectClass.Models
{
    [DataTable(TableName = "Users")]
    public class Users : IEntity
    {
        [DTField(Usage = DBFieldUsage.PrimaryKey)]
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Mobile { get; set; }
        public string Account { get; set; }
        public string Password { get; set; }
        public DateTime CreateTime { get; set; }
    }
}
