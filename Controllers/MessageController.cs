using System.Security.Claims;
using ChatAppTest.Models;
using static ChatAppTest.Services.RoomAccess;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Postgrest;
using Postgrest.Interfaces;
using static Postgrest.Constants;
using ChatAppTest.Services;
namespace ChatAppTest.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private const int MaxPageSize = 100;

        private readonly Supabase.Client _supabase;
        private readonly WebhookQueue _webhookQueue;

        public MessageController(Supabase.Client supabase, WebhookQueue webhookQueue)
        {
            _supabase = supabase;
            _webhookQueue = webhookQueue;
        }

        // The user ID and username come from the signed JWT, never from the request,
        // so a client cannot pretend to be someone else.
        private bool TryGetCurrentUser(out int userId, out string username)
        {
            username = User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out userId) && username.Length > 0;
        }

        // Filter for every room the user may read: the public room plus their own private rooms.
        private static List<IPostgrestQueryFilter> VisibleRoomsFilter(int userId) => new()
        {
            new QueryFilter("room_id", Operator.Equals, PublicRoomId),
            new QueryFilter("room_id", Operator.Like, $"private-{userId}-%"),
            new QueryFilter("room_id", Operator.Like, $"private-%-{userId}")
        };

        private static MessageDTO ToDto(Message m) => new()
        {
            Id = m.Id,
            Username = m.Username,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            SendTo = m.SendTo,
            RoomId = m.RoomId,
            SenderId = m.SenderId
        };

        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            if (!TryGetCurrentUser(out var userId, out _))
                return Unauthorized();

            try
            {
                var result = await _supabase.From<Message>()
                    .Or(VisibleRoomsFilter(userId))
                    .Get();
                return Ok(result.Models.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching messages: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] MessageDTO message)
        {
            if (!TryGetCurrentUser(out var userId, out var username))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(message.Content))
                return BadRequest("Message content is required.");

            if (!CanAccessRoom(message.RoomId, userId, out var otherUserId))
                return Forbid();

            try
            {
                var insertMessage = new Message
                {
                    Username = username,          // from JWT
                    SenderId = userId,            // from JWT
                    SendTo = otherUserId,         // derived from the room, not trusted from the client
                    RoomId = message.RoomId,
                    Content = message.Content,
                    CreatedAt = DateTime.UtcNow
                };
                var inserted = await _supabase.From<Message>().Insert(insertMessage);
                var saved = inserted.Models.FirstOrDefault() ?? insertMessage;

                _webhookQueue.TryEnqueue(new OutgoingEvent(saved.Id, saved.RoomId!, saved.Username, saved.SenderId, saved.Content, saved.CreatedAt));


                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error sending message: {ex.Message}");
            }
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages(string query)
        {
            if (!TryGetCurrentUser(out var userId, out _))
                return Unauthorized();

            try
            {
                var result = await _supabase.From<Message>()
                    .Filter("content", Operator.ILike, $"%{query}%")
                    .Or(VisibleRoomsFilter(userId))
                    .Get();
                return Ok(result.Models.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error searching messages: {ex.Message}");
            }
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteMessage(long id)
        {
            if (!TryGetCurrentUser(out var userId, out _))
                return Unauthorized();

            try
            {
                var existing = await _supabase.From<Message>()
                    .Filter("id", Operator.Equals, id.ToString())
                    .Single();

                if (existing == null)
                    return NotFound();

                // Only the sender may delete their own message.
                if (existing.SenderId != userId)
                    return Forbid();

                await _supabase.From<Message>()
                    .Filter("id", Operator.Equals, id.ToString())
                    .Filter("sender_id", Operator.Equals, userId.ToString())
                    .Delete();
                return Ok($"Message with ID {id} deleted.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting message: {ex.Message}");
            }
        }

        [HttpGet("paged")]
        public async Task<IActionResult> GetPagedMessages([FromQuery] int currentOffset, [FromQuery] int messagePageSize, [FromQuery] string roomId)
        {
            if (!TryGetCurrentUser(out var userId, out _))
                return Unauthorized();

            if (!CanAccessRoom(roomId, userId, out _))
                return Forbid();

            if (currentOffset < 0 || messagePageSize < 1 || messagePageSize > MaxPageSize)
                return BadRequest($"Offset must be >= 0 and page size between 1 and {MaxPageSize}.");

            try
            {
                var response = await _supabase
                    .From<Message>()
                    .Order("created_at", Ordering.Descending)
                    .Filter("room_id", Operator.Equals, roomId)
                    .Range(currentOffset, currentOffset + messagePageSize - 1)
                    .Get();

                if (response.ResponseMessage?.IsSuccessStatusCode == true)
                    return Ok(response.Models.Select(ToDto).ToList());

                return BadRequest("Could not fetch messages from Supabase.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetMessagesByRoomId(string roomId)
        {
            if (!TryGetCurrentUser(out var userId, out _))
                return Unauthorized();

            if (!CanAccessRoom(roomId, userId, out _))
                return Forbid();

            try
            {
                var result = await _supabase.From<Message>()
                    .Filter("room_id", Operator.Equals, roomId)
                    .Get();
                return Ok(result.Models.Select(ToDto).ToList());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching messages by room ID: {ex.Message}");
            }
        }
    }
}
