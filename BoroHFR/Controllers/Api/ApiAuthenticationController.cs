using BoroHFR.Data;
using BoroHFR.Models;
using BoroHFR.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BoroHFR.ViewModels.Authentication;
using BoroHFR.Controllers.Helpers;

namespace BoroHFR.Controllers.Api
{
    [ApiController]
    [Route("api/auth")]
    [AllowAnonymous]
    public class ApiAuthenticationController : ControllerBase
    {

        private readonly BoroHfrDbContext _dbContext;
        private readonly EmailService _emailService;
        private readonly ILogger<ApiAuthenticationController> _logger;
        private readonly IConfiguration _config;

        public ApiAuthenticationController(BoroHfrDbContext dbContext, IConfiguration config, ILogger<ApiAuthenticationController> logger, EmailService emailService)
        {
            _dbContext = dbContext;
            _config = config;
            _logger = logger;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginData data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var user = await _dbContext.Users.FirstOrDefaultAsync(x=>x.Username == data.Username);

            if (user is null || !BCrypt.Net.BCrypt.EnhancedVerify(data.Password, user.PasswordHash))
            {
                return Unauthorized("A megadott adatokkal nem található felhasználó.");
            }

            if (!user.EMailConfirmed)
            {

                if (!await _dbContext.EmailConfirmationTokens
                        .Where(x=>x.ValidUntil > DateTimeOffset.UtcNow && !x.Used && x.User == user)
                        .AnyAsync())
                {
                    await this.SendVerificationEmailAsync(_dbContext, _emailService, user);
                }
                return Unauthorized("Erősítsd meg az e-mail fiókod!");
            }

            Response.Headers.Authorization = "Bearer "+GenerateJwtToken(user,data.Persistent);

            return Ok("Bearer "+GenerateJwtToken(user,data.Persistent));

        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterData data)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new RegisterResult() {RequiredFieldsNotComplete=true });
            }

            //privacy policy check
            if (!data.AcceptPrivacyPolicy)
            {
                return BadRequest(new RegisterResult() { DidNotAcceptPrivacyPolicy=true});
            }

            //check username availability
            if (await _dbContext.Users.AnyAsync(x=>x.Username == data.Username))
            {
                return Conflict(new RegisterResult() { UsernameTaken=true});
            }
            //check email availability
            if (await _dbContext.Users.AnyAsync(x => x.EMail == data.Email))
            {
                return Conflict(new RegisterResult() { EMailTaken = true });
            }

            
            //check token validity
            var invToken = await _dbContext.InviteTokens
                .Include(t=>t.Class)
                .FirstOrDefaultAsync(x=>x.Token==data.Token);
            if (invToken is null || invToken.Usages <= 0 || DateTime.UtcNow > invToken.ValidUntil)
            {
                return Conflict(new RegisterResult() { InvalidToken = true });
            }

            //store new user in database and set token as used
            var user = new User()
            {
                Class = invToken.Class,
                EMail = data.Email,
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(data.Password),
                Username = data.Username,
                Role = invToken.Role,
                RegisterTime = DateTimeOffset.UtcNow,
            };

            _dbContext.Users.Add(user);

            invToken.Usages--;


            //email verification
            await this.SendVerificationEmailAsync(_dbContext, _emailService, user);

            await _dbContext.SaveChangesAsync();
            return Ok("Validate e-mail.");
        }


        public record PasswordResetData(string EMail);

        [HttpPost("resetpassword")]
        public async Task<IActionResult> ResetPassword(PasswordResetData data)
        {
            User? user = await _dbContext.Users.FirstOrDefaultAsync(x=>x.EMail == data.EMail);
            if (user is null)
            {
                return NotFound("Nem található felhasználó a megadott e-mail címmel.");
            }

            try
            {
                await this.SendPasswordResetEmailAsync(_dbContext, _emailService, user);
            }
            catch (Exception e)
            {
                return StatusCode(500, "Email error - consult operators if persists");
            }
            
            return Ok();
        }

        private string GenerateJwtToken(User user, bool persistent)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.EMail),
                new Claim(ClaimTypes.Role,user.Role.ToString()),
                new Claim("Id",user.Id.value.ToString())
            };
            var token = new JwtSecurityToken(_config["Jwt:Issuer"],
                _config["Jwt:Audience"],
                claims,
                expires: persistent ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(1),
                signingCredentials: credentials);


            return new JwtSecurityTokenHandler().WriteToken(token);

        }
    }

    public class RegisterData
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required string PasswordAgain { get; set; }
        public bool AcceptPrivacyPolicy { get; set; }
        public required string Email { get; set; }
        public required string Token { get; set; }
    }

    public class RegisterResult
    {
        public bool RequiredFieldsNotComplete { get; set; }
        public bool UsernameTaken { get; set; }
        public bool EMailTaken { get; set; }
        public bool InvalidToken { get; set; }
        public bool DidNotAcceptPrivacyPolicy { get; set; }
    }

    public class LoginData
    {
        [Required]
        public required string Username { get; set; }
        [Required]
        public required string Password { get; set; }
        public bool Persistent { get; set; }
    }

}
