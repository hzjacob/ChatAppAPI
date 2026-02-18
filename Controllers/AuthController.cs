using Supabase;
using Postgrest;
using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;
using ChatAppTest.Models;

namespace ChatAppTest.Controllers
{
[Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly Supabase.Client _supabase;

        public AuthController(Supabase.Client supabase)
        {
            _supabase = supabase;
        }

        [HttpPost("request-otp")]
        public async Task<IActionResult> RequestOtp([FromBody] string email)
        {
            try 
            {
                // This triggers the Supabase internal email sender
                await _supabase.Auth.SignIn(email);
                return Ok(new { message = "Check your inbox for the 6-digit code." });
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] OtpVerificationRequest request)
        {
            try
            {
#pragma warning disable CS8604 // Possible null reference argument.
                var session = await _supabase.Auth.VerifyOTP(request.Email, request.Token, Supabase.Gotrue.Constants.EmailOtpType.MagicLink);
#pragma warning restore CS8604 // Possible null reference argument.

                if (session != null)
                {
                    return Ok(new { 
                        token = session.AccessToken, 
                        expires = session.ExpiresIn,
                        user = session.User?.Email 
                    });
                }
                return Unauthorized("Invalid code.");
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("request-sms-otp")]
        public async Task<IActionResult> RequestSmsOtp([FromBody] string phoneNumber)
        {
            try
            {
                await _supabase.Auth.SignIn(Supabase.Gotrue.Constants.SignInType.Phone, phoneNumber);
                return Ok(new{ message = "OTP sent" });
            }
            catch (Exception ex)
            {
                return BadRequest(new{ error = ex.Message});
            }
        }
        [HttpPost("verify-sms-otp")]
        public async Task<IActionResult> VerifySmsOtp ([FromBody] SmsVerificationRequest request)
        {
            try
            {
#pragma warning disable CS8604 // Possible null reference argument.
                var session = await _supabase.Auth.VerifyOTP(
                    request.PhoneNumber,
                    request.Token,
                    Supabase.Gotrue.Constants.MobileOtpType.SMS
                );
#pragma warning restore CS8604 // Possible null reference argument.
                if (session?.AccessToken != null)
                {
                    return Ok(new { 
                        token = session.AccessToken, 
                        user = session.User?.Phone,
                        message = "Login successful!" 
                    });
                }
                return Unauthorized("Invalid Token Or Expired Token");
            }
            catch (Exception ex)
            {
                return BadRequest(new{error = ex.Message});
            }
        }
    }
}
