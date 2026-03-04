namespace ChatAppTest.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Supabase;
    using ChatAppTest.Models;
    using DbUser = ChatAppTest.Models.User;
    // We use an alias for BCrypt to prevent naming conflicts
    using BC = BCrypt.Net.BCrypt; 

    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly Supabase.Client _supabase;

        public UsersController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserDTO dto)
        {
            try
            {
                // Using the BC alias makes the code clean and avoids "Net" errors
                string passwordHash = BC.HashPassword(dto.Password);
                
                var newUser = new DbUser
                {
                    Username = dto.Username,
                    User_email = dto.User_email,
                    Password = passwordHash 
                };

                await _supabase.From<DbUser>().Insert(newUser);
                
                return Ok(new { Message = "User registered successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest($"Registration failed: {ex.Message}");
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserDTO userDTO)
        {
            try 
            {
                var result = await _supabase.From<DbUser>()
                    .Where(u => u.User_email == userDTO.User_email)
                    .Get();

                var dbUser = result.Models.FirstOrDefault();

                // Null check 'dbUser != null' fixes the CS8602 warning
                if (dbUser != null && BC.Verify(userDTO.Password, dbUser.Password))
                {
                    return Ok(new UserDTO 
                    { 
                        Id = dbUser.Id, 
                        Username = dbUser.Username 
                    });
                }

                return Unauthorized("Invalid email or password");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Login error: {ex.Message}");
            }
        }
    }
}