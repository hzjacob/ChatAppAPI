using System.Text.Json.Serialization;
using Postgrest.Attributes;
using Postgrest.Models;
namespace chatAppTest.Models
{
    [Table("User")]
    public class User : BaseModel
    {
        [PrimaryKey("id", false)]
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [Column("username")]
        public string Username { get; set; } = string.Empty;

    }
}