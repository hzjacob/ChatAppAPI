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
                    Username = u.Username,
                    Password = u.Password,
                    User_email = u.User_email

                }).ToList();
                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching users: {ex.Message}");
            }
        }
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] User newUser)
        {
            try
            {
                var result = await _supabase.From<User>().Insert(new User
                {
                    Username = newUser.Username,
                    User_email = newUser.User_email,
                    Password = newUser.Password
                });
                return Ok(result.Content);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error creating user: {ex.Message}");
            }
        }
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserDTO userDTO)
        {
            try
            {
                var result = await _supabase.From<User>().Where(u => u.User_email == userDTO.User_email)
                .Where(u => u.Password == userDTO.Password)
                .Get();
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
