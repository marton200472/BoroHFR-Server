using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Controllers;


[Route("api/tasks")]
[Authorize("User")]
public class ApiTasksController : ControllerBase
{
    private readonly BoroHfrDbContext _dbContext;

    public ApiTasksController(BoroHfrDbContext dbContext)
    {
        this._dbContext = dbContext;
    }

    public record ApiTask(DateTime deadline, string title, string description, bool done, string subject, string group, Guid userId);

    [HttpGet("")]
    public async Task<IActionResult> GetTasks([FromQuery]DateOnly startDate, [FromQuery]DateOnly endDate, bool showDone = false, bool showNotDone = true) {
        var user = await _dbContext.Users.Include(x=>x.AssociatedEvents).FirstAsync(x=>x.Id==this.GetUserId());
        var tasks = await _dbContext.Events
                        .Include(x=>x.Group).ThenInclude(x=>x.Subject)
                        .Where(x=>x.Group.Members.Contains(user) || (x.Group.Class.DefaultGroupId == x.Group.Id && user.ClassId == x.Group.ClassId))
                        .Where(x=>x.Type == Models.EventType.Task)
                        .Where(x=>x.Date >= startDate && x.Date <= endDate)
                        .Where(x=>(x.AssociatedUsers.Contains(user) && showDone) || (!x.AssociatedUsers.Contains(user) && showNotDone))
                        .ToArrayAsync();
        var converted = tasks.Select(x=>new ApiTask(x.Date.ToDateTime(x.StartTime),x.Title, x.Description,
        user.AssociatedEvents.Contains(x), x.Group.Subject?.Name ?? "", x.Group.Name + " - " + x.Group.Teacher,user.Id.value));
        return Ok(converted);
    }
}