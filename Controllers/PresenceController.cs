using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Supabase;
using ChatAppTest.Models;
using System.Security.Claims;

namespace ChatAppTest.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PresenceController: ControllerBase
    {
        private readonly Supabase.Client _supabaseClient;
        public PresenceController(Supabase.Client supabaseClient)
        {
            _supabaseClient = supabaseClient;
        }
        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat()
        {
            try
            {
                var userID = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;

                if (userID == null || username == null)
                {
                    return BadRequest("User ID or username not found in claims.");
                }

                if (!int.TryParse(userID, out var parsedUserId))
                {
                    return BadRequest($"Invalid user ID format: {userID}");
                }

                var presence = new Presence
                {
                    UserId = parsedUserId,
                    Username = username,
                    LastSeen = DateTime.UtcNow
                };

                var response = await _supabaseClient.From<Presence>().Upsert(presence);
                if (!response.ResponseMessage.IsSuccessStatusCode)
                {
                    return StatusCode(500, $"Error updating presence: {response.ResponseMessage.ReasonPhrase}");
                }
                return Ok("Heartbeat successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, $"Error processing heartbeat: {ex.Message}");
            }
        }
        [HttpGet("online")]
        public async Task<IActionResult> GetOnlineUsers()
        {

            var response = await _supabaseClient.From<Presence>().Select("*").Get();
            if (!response.ResponseMessage.IsSuccessStatusCode)
            {
                return StatusCode(500, $"Error fetching online users: {response.ResponseMessage.ReasonPhrase}");
            }
            Console.WriteLine(response.Content);
            return Ok(response.Models.Select(p => new PresenceDTO
            {
                UserId = p.UserId,
                Username = p.Username,
                LastSeen = p.LastSeen
            }).ToList());
            
        }
    }
}