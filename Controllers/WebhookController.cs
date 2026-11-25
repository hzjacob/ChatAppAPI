using System.Security.Claims;
using System.Security.Cryptography;
using ChatAppTest.Models;
using ChatAppTest.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static Postgrest.Constants;

namespace ChatAppTest.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private const string PublicBaseUrl = "https://chatapptest.fly.dev";

        private readonly Supabase.Client _supabase;

        public WebhookController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        // POST api/webhook  { roomId, name }
        // Creates a webhook for a room the caller can access and returns its secret URL.
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateWebhookRequest request)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Unauthorized();

            if (string.IsNullOrWhiteSpace(request.Name))
                return BadRequest("Name is required.");

            if (!RoomAccess.CanAccessRoom(request.RoomId, userId, out _))
                return Forbid();

            var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            try
            {
                await _supabase.From<IncomingWebhook>().Insert(new IncomingWebhook
                {
                    RoomId = request.RoomId,
                    Name = request.Name.Trim(),
                    Token = token,
                    CreatedBy = userId.ToString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating webhook: {ex.Message}");
            }

            return Ok(new { url = $"{PublicBaseUrl}/api/webhook/incoming/{token}" });
        }

        // POST api/webhook/incoming/{token}
        [HttpPost("incoming/{token}")]
        public async Task<IActionResult> Recieve(string token, [FromBody] IncomingWebhookPayload p)
        {
            if (string.IsNullOrEmpty(token))
            {
                return BadRequest("Token is required.");
            }
            var hook = (await _supabase.From<IncomingWebhook>().Filter("token", Operator.Equals, token).Get()).Models.FirstOrDefault();
            if (hook == null)
            {
                return BadRequest("Invalid token.");
            }

            await _supabase.From<Message>().Insert(new Message
            {
                RoomId = hook.RoomId,
                Username = p.UserName ?? hook.Name,
                Content = p.Text,
                SenderId = null,
                CreatedAt = DateTime.UtcNow
            });
            return Ok();
        }

        [HttpPost("outgoing")]
        public async Task<IActionResult> CreateOutgoing([FromBody] CreateOutgoingWebhookRequest request)
        {
                if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                    return Unauthorized();

                if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                    return BadRequest("A valid https URL is required.");

                if (!RoomAccess.CanAccessRoom(request.RoomId, userId, out _))
                    return Forbid();

                var secret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

                await _supabase.From<OutgoingWebhook>().Insert(new OutgoingWebhook
                {
                    RoomId = request.RoomId,
                    Name = request.Name.Trim(),
                    Url = uri.ToString(),
                    Secret = secret,
                    CreatedBy = userId.ToString()
                });

                // Show the secret once; the receiver needs it to verify signatures.
                return Ok(new { secret });
        }
    }
}
