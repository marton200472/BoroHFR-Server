using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using BoroHFR.ViewModels.Groups;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Controllers;

[Authorize("User")]
[Route("groups")]
public class GroupsController : Controller
{
    private readonly BoroHfrDbContext dbContext;

    public GroupsController(BoroHfrDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var uid = this.GetUserId();
        var user = await dbContext.Users.Include(x => x.Groups).Include(x => x.Class).FirstAsync(x => x.Id == uid);
        IndexViewModel VM = new IndexViewModel() { Groups = await dbContext.Groups.Where(x => x.Members.Contains(user) || x.Id == user.Class.DefaultGroupId).ToArrayAsync() };
        return View(VM);
    }
}

