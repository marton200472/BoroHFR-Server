using BoroHFR.Data;
using BoroHFR.Models;
using BoroHFR.ViewModels.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BoroHFR.Controllers.Helpers;
using Humanizer.Bytes;
using Humanizer;

namespace BoroHFR.Controllers;

[Route("settings")]
[Authorize("User")]
public class SettingsController : Controller
{
    private readonly BoroHfrDbContext _dbContext;

    public SettingsController(BoroHfrDbContext dbConnection)
    {
        _dbContext = dbConnection;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("UserData");
    }

    [HttpGet("userdata")]
    public async Task<IActionResult> UserData()
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(x=>x.Id == this.GetUserId());
        var storage = new ByteSize(await _dbContext.Files.Where(x=>x.Owner == user).SumAsync(x=>x.Size));
        var model = new UserDataViewModel()
        {
            Username = user.Username,
            Role = user.Role,
            RegDate = DateOnly.FromDateTime(user.RegisterTime.Date),
            EventsAttended = await _dbContext.Events.Where(x=>x.Type == EventType.Event && x.AssociatedUsers.Contains(user)).CountAsync(),
            HomeworksDone = await _dbContext.Events.Where(x=>x.Type == EventType.Task && x.AssociatedUsers.Contains(user)).CountAsync(),
            EventsCreated = await _dbContext.Events.Where(x => x.Creator == user).CountAsync(),
            StorageUsed = storage.Humanize()
        };
        return View(model);
    }

    [HttpGet("changepassword")]
    public IActionResult ChangePassword()
    {
        return View();
    }

    [HttpPost("changepassword")]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            model.Success = false;
        }
        else
        {
            var user = await GetCurrentUserAsync();
            if (BCrypt.Net.BCrypt.EnhancedVerify(model.OldPassword, user.PasswordHash))
            {
                user.PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword(model.NewPassword);
                model.Success = true;
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                model.Success = false;
                model.ErrorMessage = "Hibás a jelenlegi jelszó.";
            }
        }
        
        return View(model);
    }

    [HttpGet("groups")]
    public async Task<IActionResult> GroupSettings()
    {
        var user = await GetCurrentUserAsync();
        var model = new GroupSettingsViewModel
        {
            GroupMemberships = await _dbContext.Groups
                .Where(x=>x.Members.Contains(user))
                .Include(g=>g.Subject)
                .ToListAsync()
        };
        return View(model);
    }

    public record SubjectSearchData(string Query);


    [HttpPost("subjectsearch")]
    public async Task<IActionResult> SubjectSearch([FromBody] SubjectSearchData data)
    {
        var user = await GetCurrentUserAsync();
        var subjectsRes = await _dbContext.Subjects
            .Where(x => x.Class == user.Class && x.Groups.Any(y=>!y.Members.Contains(user)) && EF.Functions.Like(x.Name,"%"+data.Query+"%"))
            .Include(sub=>sub.Groups.Where(x => !x.Members.Contains(user))).ToArrayAsync();
        var model = new SubjectSearchResultViewModel() { Subjects = subjectsRes };
        return PartialView("_SubjectSearchPartial", model);
    }

    [HttpGet("joinedgroups")]
    public async Task<IActionResult> JoinedGroups()
    {
        var user = await GetCurrentUserAsync();
        var model = new GroupSettingsViewModel();
        model.GroupMemberships = await _dbContext.Groups
            .Where(x => x.Members.Contains(user))
            .Include(g => g.Subject)
            .ToListAsync();
        return PartialView("_JoinedGroupsPartial", model);

    }

    [HttpGet("joingroup/{id:guid}")]
    public async Task<IActionResult> JoinGroup(GroupId id)
    {
        var user = await GetCurrentUserAsync();
        var group = await _dbContext.Groups
            .Where(x=>x.Subject.Class.Id == user.ClassId && x.Id == id)
            .SingleOrDefaultAsync();
        if (group is null)
        {
            return BadRequest("User cannot join that group.");
        }
        group.Members.Add(user);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("leavegroup/{id:guid}")]
    public async Task<IActionResult> LeaveGroup(GroupId id)
    {
        var user = await GetCurrentUserAsync();
        var group = await _dbContext.Groups
            .Include(g=>g.Members)
            .SingleOrDefaultAsync(x=>x.Id == id);

        if(group is not null)
        {
            group.Members.Remove(user);
            await _dbContext.SaveChangesAsync();
            return Ok();
        }
        
        return BadRequest();
    }


    private async Task<User> GetCurrentUserAsync()
    {
        var id = new UserId(Guid.Parse(User.Claims.Single(x => x.Type == "Id").Value));
        var user = await _dbContext.Users.Include(u=>u.Class).FirstOrDefaultAsync(x=>x.Id==id);
        if (user is null)
        {
            throw new Exception("Current user cannot be found in database.");
        }
        return user;
    }


    public record GroupJoinData(GroupId Id);
}