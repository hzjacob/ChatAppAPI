using Postgrest.Attributes;
using Postgrest.Models;

namespace ChatAppTest.Models;

[Table("incoming_webhooks")]
public class IncomingWebhook : BaseModel
{
    [PrimaryKey("id", false)]
    public int Id { get; set; }
    [Column("room_id")]
    public string RoomId { get; set; } = string.Empty;
    [Column("name")]
    public string Name { get; set; } = string.Empty;
    [Column("token")]
    public string Token { get; set; } = string.Empty;
    [Column("created_by")]
    public string CreatedBy { get; set; } = string.Empty;
}

public record IncomingWebhookPayload(string Text, string? UserName);

public record CreateWebhookRequest(string RoomId, string Name);
