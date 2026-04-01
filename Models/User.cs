using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Postgrest.Attributes;
using Postgrest.Models;
using ColumnAttribute = Postgrest.Attributes.ColumnAttribute;
namespace ChatAppTest.Models
{
    [Postgrest.Attributes.Table("User")]
    public class User : BaseModel
    {
        [PrimaryKey("id", false)]
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [Column("username")]
        public string Username { get; set; } = string.Empty;
        [Column("user_email")]
        public string User_email { get; set; } = string.Empty;
        [Column("password")]
        public string Password { get; set;} = string.Empty;
        [Column("refresh_token")]
        public string RefreshToken { get; set; } = string.Empty;
        [Column("refresh_token_expiry")]
        public DateTime RefreshTokenExpiry { get; set; }
        

    }
}