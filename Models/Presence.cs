using System.Text.Json.Serialization;
using Postgrest.Attributes;
using Postgrest.Models;
using ColumnAttribute = Postgrest.Attributes.ColumnAttribute;
namespace ChatAppTest.Controllers
{
    [Table("online_users")]
    public class Presence : BaseModel
    {

        [PrimaryKey("id", false)]
        [Column("user_id")]
        public long UserId { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Column("last_seen")]
        public DateTime LastSeen { get; set; }
    }
}