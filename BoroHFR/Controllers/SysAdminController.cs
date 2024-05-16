using BoroHFR.Data;
using BoroHFR.Models;
using BoroHFR.Services;
using BoroHFR.ViewModels.SysAdmin;
using Humanizer;
using Humanizer.Bytes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;

namespace BoroHFR.Controllers
{
    [Authorize("SysAdmin")]
    [Route("sysadmin")]
    public class SysAdminController : Controller
    {
        private readonly IConfiguration _config;
        private readonly BoroHfrDbContext _dbContext;
        private readonly FileStorageHandler _fileHandler;

        public SysAdminController(IConfiguration config, BoroHfrDbContext dbContext, FileStorageHandler fileHandler)
        {
            _config = config;
            _dbContext = dbContext;
            _fileHandler = fileHandler;
        }

        [AllowAnonymous]
        [HttpGet("")]
        public IActionResult Index()
        {
            if (!User.IsInRole("SysAdmin"))
            {
                return RedirectToAction("Login");
            }


            return RedirectToAction("Classes");
        }

        [HttpGet("classes")]
        public async Task<IActionResult> Classes()
        {
            var model = new ClassesViewModel() { Classes = await _dbContext.Classes.ToArrayAsync() };
            return View(model);
        }

        [HttpGet("classes/create")]
        public IActionResult CreateClass()
        {
            return View();
        }

        [HttpPost("classes/create")]
        public async Task<IActionResult> CreateClass(CreateClassViewModel data)
        {
            if (!ModelState.IsValid)
            {
                return View(data);
            }

            if (await _dbContext.Classes.AnyAsync(x=>x.Name == data.Name))
            {
                data.NameUsed = true;
                return View(data);
            }
            
            var c = new Class() { Name=data.Name };
            
            var defGroup = new Group() { Name = c.Name + " Általános", Class = c, Subject = null, Teacher = "" };
            var defConv = new Conversation() { Group = defGroup, Name = c.Name + " általános", IsOpen = true };
            _dbContext.Classes.Add(c);
            _dbContext.Groups.Add(defGroup);
            _dbContext.Conversations.Add(defConv);
            await _dbContext.SaveChangesAsync();

            c.DefaultGroupId = defGroup.Id;
            defGroup.DefaultConversationId = defConv.Id;
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("ClassInfo",new { id = c.Id.value});
        }

        [HttpGet("classes/{id:guid}")]
        public async Task<IActionResult> ClassInfo(ClassId id)
        {
            var c = await _dbContext.Classes.Include(x=>x.Members!.OrderByDescending(y=>y.Role)).AsSplitQuery().FirstOrDefaultAsync(x => x.Id == id);
            if (c is null)
            {
                return NotFound();
            }

            var storage = await _dbContext.Files.Where(x=>x.Class == c).SumAsync(x=>x.Size);
            ByteSize size = new ByteSize(storage);
            return View(new ClassInfoViewModel() { Class = c, StorageUsed = size.Humanize()});
        }

        [HttpGet("classes/{id:guid}/delete")]
        public async Task<IActionResult> DeleteClass(ClassId id)
        {
            var c = await _dbContext.Classes.SingleOrDefaultAsync(x=>x.Id == id);
            if (c is null)
            {
                return NotFound();
            }
            var model = new DeleteClassViewModel();
            model.Name = c.Name;
            model.Id = c.Id.value;
            model.UserCount = await _dbContext.Users.Where(x=>x.Class == c).CountAsync();
            model.EventCount = await _dbContext.Events.Where(x=>x.Group!.Class == c).CountAsync();
            model.ChatMessageCount = await _dbContext.ConversationMessages.Where(x => x.Conversation.Group.Class == c).CountAsync();
            return View(model);
        }

        [HttpGet("classes/{id:guid}/delete/confirm")]
        public async Task<IActionResult> ConfirmDeleteClass(ClassId id)
        {
            foreach (var f in _dbContext.Files.Where(x => x.ClassId == id).AsEnumerable())
            {
                _fileHandler.DeleteFileFromStorage(f.Id.value);
            }
            await _dbContext.Classes.Where(x => x.Id == id).ExecuteDeleteAsync();
            return RedirectToAction("Classes");
        }

        [HttpGet("classes/{id:guid}/admintoken")]
        public async Task<IActionResult> CreateAdminToken(ClassId id)
        {
            var c = await _dbContext.Classes.FirstOrDefaultAsync(x=>x.Id == id);
            if (c is null)
            {
                return RedirectToAction("Classes");
            }
            var tokenstring = InviteToken.GenerateToken();
            while (await _dbContext.InviteTokens.AnyAsync(x=>x.Token == tokenstring))
            {
                tokenstring = InviteToken.GenerateToken();
            }
            var token = new InviteToken()
            {
                Class = c,
                Token = tokenstring,
                Usages = 1,
                ValidUntil = DateTime.Now.AddMonths(1),
                Role = UserRole.Admin
            };
            _dbContext.InviteTokens.Add(token);
            await _dbContext.SaveChangesAsync();
            return View(new CreateAdminTokenViewModel() { Token = tokenstring, ClassId = id});
        }

        [HttpGet("classes/{cid:guid}/users/{uid:guid}/resetpassword")]
        public async Task<IActionResult> ResetPassword(ClassId cid, UserId uid)
        {
            var user = await _dbContext.Users.Include(x=>x.Class).FirstOrDefaultAsync(x => x.Id == uid);
            if (user is null || user.Class.Id != cid)
            {
                return RedirectToAction("ClassInfo", new {id=cid.value });
            }

            return View(new ResetPasswordViewModel() { Confirmed = false, User=user });
        }

        [HttpGet("classes/{cid:guid}/users/{uid:guid}/resetpassword/confirm")]
        public async Task<IActionResult> ConfirmResetPassword(ClassId cid, UserId uid)
        {
            var user = await _dbContext.Users.Include(x => x.Class).FirstOrDefaultAsync(x => x.Id == uid);
            if (user is null || user.Class.Id != cid)
            {
                return RedirectToAction("ClassInfo", new { id = cid.value });
            }

            var newPass = RandomNumberGenerator.GetString("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz",10);
            user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(newPass);
            await _dbContext.SaveChangesAsync();

            return View("ResetPassword",new ResetPasswordViewModel() { Confirmed = true, User = user, Password = newPass });
        }

        [HttpGet("classes/{cid:guid}/users/{uid:guid}/delete")]
        public async Task<IActionResult> DeleteUser(ClassId cid, UserId uid)
        {
            var user = await _dbContext.Users.Include(x => x.Class).FirstOrDefaultAsync(x => x.Id == uid);
            if (user is null || user.Class.Id != cid)
            {
                return RedirectToAction("ClassInfo", new { id = cid.value });
            }

            return View(user);
        }

        [HttpGet("classes/{cid:guid}/users/{uid:guid}/delete/confirm")]
        public async Task<IActionResult> ConfirmDeleteUser(ClassId cid, UserId uid)
        {
            var user = await _dbContext.Users.Include(x => x.Class).FirstOrDefaultAsync(x => x.Id == uid);
            if (user is null || user.Class.Id != cid)
            {
                return RedirectToAction("ClassInfo", new { id = cid.value });
            }

            //delete files
            foreach (var f in _dbContext.Files.Where(x => x.Owner == user).AsEnumerable())
            {
                _fileHandler.DeleteFileFromStorage(f.Id.value);
            }

            _dbContext.Users.Remove(user);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("ClassInfo", new { id = cid.value });
        }

        [HttpGet("classes/{cid:guid}/users/{uid:guid}/makeadmin")]
        public async Task<IActionResult> MakeUserAdmin(ClassId cid, UserId uid)
        {
            var user = await _dbContext.Users.Include(x => x.Class).FirstOrDefaultAsync(x => x.Id == uid);
            if (user is null || user.Class.Id != cid)
            {
                return RedirectToAction("ClassInfo", new { id = cid.value });
            }

            user.Role = UserRole.Admin;
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("ClassInfo", new { id = cid.value });
        }

        [HttpGet("classes/{cid:guid}/users/{uid:guid}/demote")]
        public async Task<IActionResult> DemoteUser(ClassId cid, UserId uid)
        {
            var user = await _dbContext.Users.Include(x => x.Class).FirstOrDefaultAsync(x => x.Id == uid);
            if (user is null || user.Class.Id != cid)
            {
                return RedirectToAction("ClassInfo", new { id = cid.value });
            }

            user.Role = UserRole.User;
            await _dbContext.SaveChangesAsync();

            return RedirectToAction("ClassInfo", new { id = cid.value });
        }

        [AllowAnonymous]
        [HttpGet("auth")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpGet("auth/logout")]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            return SignOut(new AuthenticationProperties { RedirectUri = Url.Action("Login") });
        }

        [AllowAnonymous]
        [HttpPost("auth")]
        public async Task<IActionResult> Login(AuthViewModel data)
        {
            if (!ModelState.IsValid)
            {
                return View(data);
            }
            if (data.Email != _config["SysAdmin:Email"] || !BCrypt.Net.BCrypt.EnhancedVerify(data.Password, _config["SysAdmin:PasswordHash"]))
            {   
                data.WrongAuthData = true;
                return View(data);
            }
            var principal = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Role, "SysAdmin")
            },"Cookies"));
            await HttpContext.SignInAsync("Cookies", principal);
            return RedirectToAction("Index");
        }
    }
}
