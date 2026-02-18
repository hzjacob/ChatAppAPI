namespace ChatAppTest.Controllers
{
    using chatAppTest.Models;
    using ChatAppTest.Models;
    using Microsoft.AspNetCore.Mvc;
    using Supabase;

    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly Supabase.Client _supabase;

        public UsersController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var result = await _supabase.From<User>().Get();
                var users = result.Models.Select(u => new UserDTO
                {
                    Id = u.Id,
                    Username = u.Username
                }).ToList();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching users: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateUser(string username)
        {
            try
            {
                var result = await _supabase.From<User>().Insert(new User
                {
                    Username = username
                });
                return Ok(result.Content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating user: {ex.Message}");
            }
        }
        [HttpGet("login")]
        public async Task<IActionResult> Login(string username, string password)
        {
            try
            {
                var result = await _supabase.From<User>().Where(u => u.Username == username).Get();
                var user = result.Models.FirstOrDefault();
                if (user == null)
                {
                    return NotFound("User not found");
                }
                return Ok(new UserDTO { Id = user.Id, Username = user.Username });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error logging in: {ex.Message}");
            }
        }
    }
}
