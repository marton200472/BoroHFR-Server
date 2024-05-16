using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using BoroHFR.Models;
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

    public record ApiTask(Guid id, Guid groupId, DateTime deadline, string title, string description, bool done, string subject, string group, Guid userId);

    [HttpGet("")]
    [Produces(typeof(ApiTask[]))]
    public async Task<IActionResult> GetTasks(bool showDone = false, bool showNotDone = true) {
        var user = await _dbContext.Users.Include(x=>x.AssociatedEvents).FirstAsync(x=>x.Id==this.GetUserId());
        var tasks = await _dbContext.Events
                        .Include(x=>x.Group).ThenInclude(x=>x.Subject)
                        .Where(x=>x.Group.Members.Contains(user) || (x.Group.Class.DefaultGroupId == x.Group.Id && user.ClassId == x.Group.ClassId))
                        .Where(x=>x.Type == Models.EventType.Task)
                        .Where(x=>(x.AssociatedUsers.Contains(user) && showDone) || (!x.AssociatedUsers.Contains(user) && showNotDone))
                        .OrderBy(x=>x.Date).ThenBy(x=>x.StartTime)
                        .ToArrayAsync();
        var converted = tasks.Select(x=>new ApiTask(x.Id.value, x.GroupId.value, x.Date.ToDateTime(x.StartTime),x.Title, x.Description ?? "",
        user.AssociatedEvents.Contains(x), x.Group.Subject?.Name ?? "", x.Group.Name + " - " + x.Group.Teacher,user.Id.value));
        return Ok(converted);
    }

    [HttpGet("{id:guid}/delete")]
    public async Task<IActionResult> DeleteTask(Models.EventId id)
    {
        var user = await _dbContext.Users.Include(x => x.Class).FirstAsync(x => x.Id == this.GetUserId());
        var t = await _dbContext.Events.Where(x => (x.Group.Members.Contains(user) || user.Class.DefaultGroupId == x.Group.Id) && (x.Creator == user || user.Role == UserRole.Admin) && x.Type == EventType.Task).SingleOrDefaultAsync(x => x.Id == id);
        if (t is null)
        {
            return BadRequest();
        }

        _dbContext.Events.Remove(t);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    public record ApiTaskCreate(string title, string description, DateTime deadline, Guid groupId);

    [HttpPost("create")]
    public async Task<IActionResult> CreateTask(ApiTaskCreate task)
    {
        var user = await _dbContext.Users.Include(x => x.AssociatedEvents).FirstAsync(x => x.Id == this.GetUserId());
        var group = await _dbContext.Groups.SingleOrDefaultAsync(x => x.Id == new GroupId(task.groupId));
        if (group is null)
        {
            return BadRequest();
        }
        var conv = new Conversation()
        {
            Group = group,
            Name = task.title,
            IsOpen = true
        };
        var t = new Event()
        {
            Conversation = conv,
            CreateTime = DateTimeOffset.Now,
            Creator = user,
            Group = group,
            Title = task.title,
            Description = task.description,
            Date = DateOnly.FromDateTime(task.deadline),
            StartTime = TimeOnly.FromDateTime(task.deadline),
            Type = EventType.Task
        };
        _dbContext.Conversations.Add(conv);
        _dbContext.Events.Add(t);
        await _dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id:guid}/modify")]
    public async Task<IActionResult> ModifyTask(ApiTaskCreate task, Models.EventId id)
    {
        var user = await _dbContext.Users.Include(x => x.Class).FirstAsync(x => x.Id == this.GetUserId());
        var t = await _dbContext.Events.Where(x=>(x.Group.Members.Contains(user) || user.Class.DefaultGroupId == x.Group.Id) && (x.Creator == user || user.Role == UserRole.Admin) && x.Type == EventType.Task).SingleOrDefaultAsync(x=>x.Id == id);
        if (t is null)
        {
            return BadRequest();
        }
        t.Date = DateOnly.FromDateTime(task.deadline);
        t.StartTime = TimeOnly.FromDateTime(task.deadline);
        t.Title = task.title;
        t.Description = task.description;
        var group = await _dbContext.Groups.FirstOrDefaultAsync(x=>x.Id == new GroupId(task.groupId));
        if (group is null)
        {
            return BadRequest();
        }
        t.Group = group;
        await _dbContext.SaveChangesAsync();
        return Ok();
    }


    [HttpGet("{id:guid}/setdone/{done:bool}")]
    public async Task<IActionResult> SetTaskDone(Models.EventId id, bool done)
    {
        var user = await _dbContext.Users.Include(x => x.AssociatedEvents).FirstAsync(x => x.Id == this.GetUserId());
        var ev = await _dbContext.Events.Where(x=>x.Type == EventType.Task && (x.Group!.Members.Contains(user) || x.Group.Class.DefaultGroupId == x.Group.Id)).FirstOrDefaultAsync(x=>x.Id == id);
        if (ev is null)
        {
            return NotFound();
        }
        if (user.AssociatedEvents.Contains(ev) && !done)
        {
            user.AssociatedEvents.Remove(ev);
        }
        else if (!user.AssociatedEvents.Contains(ev) && done)
        {
            user.AssociatedEvents.Add(ev);
        }

        await _dbContext.SaveChangesAsync();
        return Ok();
    }
}