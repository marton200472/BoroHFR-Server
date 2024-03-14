using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using BoroHFR.ViewModels.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Controllers
{
    [Route("tasks")]
    [Authorize("User")]
    public class TasksController : Controller
    {
        private readonly BoroHfrDbContext _dbContext;

        public TasksController(BoroHfrDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var user = await _dbContext.Users.Include(x=>x.Class).FirstAsync(x=>x.Id == this.GetUserId());
            var tasks = await _dbContext.Events.Where(x =>
                x.Type == Models.EventType.Task &&
                (x.Group!.Id == user.Class.DefaultGroupId || x.Group.Members.Contains(user)) &&
                !x.AssociatedUsers.Contains(user)
            ).Include(x=>x.AttachedFiles).OrderBy(x=>x.Date).ThenBy(x=>x.StartTime).ToArrayAsync();
            var model = new TasksViewModel()
            {
                Tasks = tasks, 
                IsUserAdmin = user.Role == Models.UserRole.Admin,
                UserId = user.Id
            };
            return View(model);
        }

        [HttpGet("{id:guid}/markdone")]
        public async Task<IActionResult> MarkDone(Models.EventId id)
        {
            var ev = await _dbContext.Events.Include(x=>x.AssociatedUsers).FirstAsync(x=>x.Id == id);
            var user = await _dbContext.Users.FirstAsync(x => x.Id == this.GetUserId());
            if (!ev.AssociatedUsers.Contains(user))
            {
                ev.AssociatedUsers.Add(user);
            }
            await _dbContext.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}
