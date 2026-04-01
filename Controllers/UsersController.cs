namespace ChatAppTest.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Supabase;
    using ChatAppTest.Models;
    using DbUser = ChatAppTest.Models.User;
    // We use an alias for BCrypt to prevent naming conflicts
    using BC = BCrypt.Net.BCrypt;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using Microsoft.IdentityModel.Tokens;
    using System.Text.Unicode;
    using System.Text;
    using System.IdentityModel.Tokens.Jwt;

    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly Supabase.Client _supabase;
        private readonly IConfiguration? _config;

        public UsersController(Supabase.Client supabase, IConfiguration config)
        {
            _supabase = supabase;
            _config = config;
        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.User_email),
                new Claim("User_id", user.Id.ToString()),
                new Claim("role", "authenticated")
            };
            var keyStr = _config["Jwt:Key"];
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
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
                    var token = GenerateJwtToken(dbUser);
                    var refreshToken = GenerateRefreshToken();

                    dbUser.RefreshToken = refreshToken;
                    dbUser.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7); 
                    await _supabase.From<DbUser>()
                    .Where(u=> u.Id == dbUser.Id)
                    .Set(u => u.RefreshToken, dbUser.RefreshToken)
                    .Set(u => u.RefreshTokenExpiry, dbUser.RefreshTokenExpiry)
                    .Update();
                    return Ok(new UserDTO 
                    { 
                        Id = dbUser.Id, 
                        Username = dbUser.Username,
                        Token = token,
                        RefreshToken = refreshToken


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