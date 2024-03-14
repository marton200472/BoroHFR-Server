using System.Security.Claims;
using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using BoroHFR.Models;
using BoroHFR.Services;
using BoroHFR.ViewModels.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Controllers;

[AllowAnonymous]
[Route("authentication")]
public class AuthenticationController : Controller
{
    private readonly BoroHfrDbContext _dbContext;
    private readonly EmailService _emailService;
    private readonly ILogger<AuthenticationController> _logger;
    private readonly IConfiguration _configuration;

    public AuthenticationController(BoroHfrDbContext dbContext,
        EmailService emailService,
        ILogger<AuthenticationController> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Login");
    }

    [HttpGet("contributors")]
    public IActionResult Contributors()
    {
        return View();
    }

    [HttpGet("login")]
    public IActionResult Login()
    {
        return User?.Identity?.IsAuthenticated ?? false ? RedirectToAction("Index","Home") : View();
    }

    [HttpPost("login/{returnUrl?}")]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = await _dbContext.Users.FirstOrDefaultAsync(x=>x.Username == model.Username);
        if (user is null)
        {
            model.BadUsername = true;
            return View(model);
        }

        if (!BCrypt.Net.BCrypt.EnhancedVerify(model.Password, user.PasswordHash))
        {
            model.BadPassword = true;
            return View(model);
        }

        //check e-mail confirmed
        if (!user.EMailConfirmed)
        {
            if (!await _dbContext.EmailConfirmationTokens
                    .Where(x=>!x.Used && x.ValidUntil > DateTimeOffset.UtcNow && x.User == user)
                    .AnyAsync())
            {
                try
                {
                    await this.SendVerificationEmailAsync(_dbContext, _emailService, user);
                }
                catch (Exception)
                {
                    return StatusCode(500, "Email error - consult operators if persists");
                }
                 
            }
           
            return View("EmailConfirmationNotification", new EmailConfirmationNotificationViewModel() { EmailAddress = SafeEmailString(user.EMail) });
        }

        var (principal, properties) = GetAuthenticationData(user,model.RemainLoggedIn);

        if (returnUrl is null)
        {
            properties.RedirectUri = Url.Action("Index","Home");
        }
        
        return SignIn(principal, properties,
            Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet("register")]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        //privacy policy check
        if (!model.AcceptPrivacyPolicy)
        {
            model.DidNotAcceptPrivacyPolicy = true;
        }

        //check username availability
        if (await _dbContext.Users.AnyAsync(x=>x.Username == model.Username))
        {
            model.UsernameTaken = true;
        }
        //check email availability
        if (await _dbContext.Users.AnyAsync(x=>x.EMail == model.EMail))
        {
            model.EMailTaken = true;
        }

        //return error if email or username taken
        if (model.UsernameTaken || model.EMailTaken || model.DidNotAcceptPrivacyPolicy)
        {
            return View(model);

        }
        //check token validity
        var invToken = await _dbContext.InviteTokens
            .Include(token=>token.Class)
            .Where(x=>x.Token == model.Token && x.ValidUntil > DateTimeOffset.UtcNow)
            .FirstOrDefaultAsync();
        if (invToken is null)
        {
            model.BadToken = true;
            return View(model);
        }
        else if (invToken.Usages <= 0 || DateTimeOffset.UtcNow > invToken.ValidUntil)
        {
            model.BadToken = true;
            return View(model);
        }


        //store new user in database and set token as used
        var user = new User()
        {
            ClassId = invToken.ClassId,
            EMail = model.EMail,
            PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(model.Password),
            Role = invToken.Role,
            Username = model.Username,
            Class = invToken.Class,
            RegisterTime = DateTimeOffset.UtcNow,
        };
        

        _dbContext.Users.Add(user);

        invToken.Usages--;


        //email verification
        await this.SendVerificationEmailAsync(_dbContext, _emailService, user);
        
        await _dbContext.SaveChangesAsync();
        return View("EmailConfirmationNotification", new EmailConfirmationNotificationViewModel() { EmailAddress = SafeEmailString(user.EMail) });
    }

    [HttpGet("logout")]
    public IActionResult Logout()
    {
        return SignOut(new AuthenticationProperties() { RedirectUri = Url.Action("Login") }, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
    }

    [HttpGet("passwordreset")]
    public IActionResult ResetPassword()
    {
        return View();
    }

    [HttpPost("passwordreset")]
    public async Task<IActionResult> ResetPassword(PasswordResetRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(x=>x.EMail == model.EMail);
        if (user is not null)
        {
            try
            {
                await this.SendPasswordResetEmailAsync(_dbContext, _emailService, user);
            }
            catch (Exception e)
            {
                return StatusCode(500, "Email error - consult operators if persists");
            }
            
        }
        return View("PasswordResetEmailSent", new PasswordResetEmailSentViewModel() { EMail = SafeEmailString(model.EMail) });
    }

    [HttpGet("newpassword")]
    public async Task<IActionResult> NewPassword(PasswordResetTokenId token)
    {
        var resToken = await _dbContext.PasswordResetTokens
            .Where(x=>!x.Used && x.ValidUntil > DateTimeOffset.UtcNow && x.Token==token)
            .FirstOrDefaultAsync();
        if (resToken is null)
        {
            return View("NewPasswordFailed");
        }
        return View();

    }

    [HttpPost("newpassword")]
    public async Task<IActionResult> NewPassword(PasswordResetTokenId token, NewPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var resToken = await _dbContext.PasswordResetTokens
            .Where(x=>!x.Used && x.ValidUntil > DateTimeOffset.UtcNow && x.Token==token)
            .Include(t=>t.User)
            .FirstOrDefaultAsync();
        if (resToken is null)
        {
            return View("NewPasswordFailed");
        }

        var user = resToken.User;
        user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(model.NewPassword);
        resToken.Used = true;

        await _dbContext.SaveChangesAsync();

        return View("NewPasswordSuccess");
    }

    private string SafeEmailString(string email)
    {
        return email.Substring(0, email.IndexOf('@') / 3) + "***" + email.Substring(email.IndexOf('@') - 2);
    }

    [HttpGet("confirmemail")]
    public async Task<IActionResult> ConfirmEmail(EmailConfirmationTokenId token)
    {
        var cToken = await _dbContext.EmailConfirmationTokens
            .Where(x=>!x.Used && x.ValidUntil > DateTimeOffset.UtcNow && x.Token==token)
            .Include(t=>t.User)
            .FirstOrDefaultAsync();
        if (cToken is null)
        {
            return View("EmailConfirmationFailed");
        }
        var user = cToken.User;
        user.EMailConfirmed = true;
        cToken.Used = true;
        await _dbContext.SaveChangesAsync();
        return View("EmailConfirmationSuccess");
    }

    private (ClaimsPrincipal principal, AuthenticationProperties properties) GetAuthenticationData(User u, bool persistentLogin)
    {
        var claims = new List<Claim>()
        {
            new Claim(ClaimTypes.Name, u.Username),
            new Claim(ClaimTypes.Email, u.EMail),
            new Claim(ClaimTypes.Role,u.Role.ToString()),
            new Claim("Id",u.Id.value.ToString())
        };
        var identity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var authSettings = new AuthenticationProperties
        {
            IsPersistent = persistentLogin,
            ExpiresUtc = persistentLogin ? DateTimeOffset.UtcNow.AddMonths(1) : DateTimeOffset.UtcNow.AddHours(1),
            AllowRefresh = true
        };
        return (principal, authSettings);
    }
}