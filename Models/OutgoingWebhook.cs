using Postgrest.Attributes;
using Postgrest.Models;

namespace ChatAppTest.Models;

[Table("outgoing_webhooks")]
public class OutgoingWebhook : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id {get; set;}
    [Column("room_id")]
    public string? RoomId {get; set;}
    [Column("name")]
    public string? Name {get; set;}
    [Column("url")]
    public string? Url {get; set;}
    [Column("secret")]
    public string? Secret {get; set;}
    [Column("created_by")]
    public string? CreatedBy {get; set;}
}
public record CreateOutgoingWebhookRequest (string RoomId, string Name, string Url);

