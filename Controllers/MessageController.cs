using ChatAppTest.Models;
using Microsoft.AspNetCore.Mvc;
using static Postgrest.Constants;
namespace ChatAppTest.Controllers
{

    

    [ApiController]
    [Route("api/[controller]")]
    public class MessageController : ControllerBase
    {
        private readonly Supabase.Client _supabase;

        public MessageController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages()
        {
            try
            {
                var result = await _supabase.From<Message>().Get();
                var message = result.Models.Select(m => new MessageDTO
                {
                    Id = m.Id,
                    Username = m.Username,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    SendTo = m.SendTo
                }).ToList();
                return Ok(message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching messages: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> SendMessage()
        {
            try
            {
                var result = await _supabase.From<Message>().Insert(new Message
                {
                    Username = "anaratten",
                    Content = "bu mesaj api vasitesile gonderilmishdir, Anar bayramova tten",
                    CreatedAt = DateTime.UtcNow,
                    SendTo = null
                });
                return Ok(result.Content);
            }
            catch(Exception ex)
            {
                return StatusCode(500, $"Error sending message: {ex.Message}");
            }
        }
        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages(string query)
        {
            try
            {
                var result = await _supabase.From<Message>()
                    .Filter("content", Postgrest.Constants.Operator.ILike, $"%{query}%")
                    .Get();
                var messages = result.Models.Select(m => new MessageDTO
                {
                    Id = m.Id,
                    Username = m.Username,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt,
                    SendTo = m.SendTo
                }).ToList();
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error searching messages: {ex.Message}");
            }
        }
        [HttpDelete]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            try
            {
                await _supabase.From<Message>()
                    .Filter("id", Postgrest.Constants.Operator.Equals, id.ToString())
                    .Delete();
                return Ok($"Message with ID {id} deleted.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error deleting message: {ex.Message}");
            }
        }
        [HttpGet("paged")]
        public async Task<IActionResult> GetPagedMessages([FromQuery] int currentOffset, [FromQuery] int messagePageSize)
        {
            try
            {
                var response = await _supabase
                    .From<Message>()
                    .Order("created_at", Postgrest.Constants.Ordering.Descending)
                    .Range(currentOffset, currentOffset + messagePageSize - 1)
                    .Get();

                // 1. Check if we actually got a successful response from Supabase
                if (response.ResponseMessage?.IsSuccessStatusCode == true)
                {
                    // 2. Map the complex Supabase models to your simple MessageDTOs
                    var messages = response.Models.Select(m => new MessageDTO
                    {
                        Id = m.Id,
                        Username = m.Username,
                        Content = m.Content,
                        CreatedAt = m.CreatedAt,
                        SendTo = m.SendTo
                    }).ToList();

                    // 3. ONLY return the list of DTOs. 
                    // This is what System.Text.Json can handle perfectly.
                    return Ok(messages); 
                }

                return BadRequest("Could not fetch messages from Supabase.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }
    }
}