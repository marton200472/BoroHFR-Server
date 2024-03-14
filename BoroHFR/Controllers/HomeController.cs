using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using BoroHFR.Models;
using BoroHFR.ViewModels.Home;
using Humanizer;
using Humanizer.Bytes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Cmp;
using Org.BouncyCastle.Bcpg;
using System.Runtime.InteropServices;
using EventId = BoroHFR.Models.EventId;
/*using BoroHFR.Models;
using BoroHFR.ViewModels;*/

namespace BoroHFR.Controllers;

[Authorize("User")]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly BoroHfrDbContext dbContext;

    public HomeController(ILogger<HomeController> logger, BoroHfrDbContext dbContext, IConfiguration config)
    {
        _logger = logger;
        this.dbContext = dbContext;
    }

    [HttpGet("user/{id:guid}")]
    public async Task<IActionResult> UserInfo(UserId id)
    {
        var currUser = await dbContext.Users.FindAsync(this.GetUserId());
        var user = await dbContext.Users.FindAsync(id);
        if (user is null || currUser is null || currUser.ClassId != user.ClassId)
        {
            return NotFound();
        }
        var storage = new ByteSize(await dbContext.Files.Where(x => x.Owner == user).SumAsync(x => x.Size));
        var model = new ViewModels.Settings.UserDataViewModel()
        {
            Username = user.Username,
            Role = user.Role,
            RegDate = DateOnly.FromDateTime(user.RegisterTime.Date),
            EventsAttended = await dbContext.Events.Where(x => x.Type == EventType.Event && x.AssociatedUsers.Contains(user)).CountAsync(),
            HomeworksDone = await dbContext.Events.Where(x => x.Type == EventType.Task && x.AssociatedUsers.Contains(user)).CountAsync(),
            EventsCreated = await dbContext.Events.Where(x => x.Creator == user).CountAsync(),
            StorageUsed = storage.Humanize()
        };

        return View(model);
    }
    
    [HttpGet("calendardata")]
    public async Task<IActionResult> LoadCalendarData(DateTime start, DateTime end)
    {
        DateOnly startDate = DateOnly.FromDateTime(start);
        DateOnly endDate = DateOnly.FromDateTime(end);

        var uid = this.GetUserId();
        var user = await dbContext.Users.Include(x => x.Class).FirstAsync(x => x.Id == uid);
        var events = await dbContext.Events.Include(x=>x.AssociatedUsers).Where(x => x.Group.Members.Contains(user) || x.Group.Id == user.Class.DefaultGroupId).ToArrayAsync();
        var convertedevs = events
            .Select(x => new CalDataCell() { 
                Id = x.Id.value.ToString(),
                Start = x.Date.ToDateTime(x.StartTime).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"),
                End = x.Date.ToDateTime(x.EndTime ?? x.StartTime.AddHours(1)).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"),
                Title = x.Title,
                BackgroundColor = (x.Type == EventType.Event ? (x.AssociatedUsers.Contains(user)? "#166182" : "#2dace3") : (x.AssociatedUsers.Contains(user) ? "#8a6213" : "#e3a62d")),
                ExtendedProps = new() { Type = "event" }
            } );
        var lessons = await dbContext.Lessons.Where(x => x.Group.Class.Members.Contains(user)).ToArrayAsync();
        var convertedlessons = lessons
            .SelectMany(x => x.GetLessonsInRange(startDate, endDate))
            .Select(x =>new CalDataCell() {
                Id = x.Id.ToString(),
                Title = x.Label, 
                Start = x.Date.ToDateTime(x.StartTime).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"), 
                End = x.Date.ToDateTime(x.EndTime).ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss"), 
                BackgroundColor = "#32a852", 
                ExtendedProps = new() { Type = "lesson" } 
            })
            .ToArray();

        return Ok(convertedevs.Concat(convertedlessons));
    }

    [HttpGet("editevent/{evid:guid}")]
    public async Task<IActionResult> EditEvent(EventId evid)
    {
        Event? hw = await dbContext.Events.Include(x=>x.AttachedFiles).FirstOrDefaultAsync(x => x.Id == evid);
        if (hw is null)
        {
            return NotFound();
        }
        EditEventViewModel VM = new EditEventViewModel() { Event = hw };
        return View(VM);
    }

    [HttpPost("editevent/{evid:guid}")]
    public async Task<IActionResult> EditEvent(EventId evid, EditEventViewModel VM)
    {
        var uid = this.GetUserId();
        var user = await dbContext.Users.Include(x=>x.Class).FirstAsync(x => x.Id == uid);
        var files = await dbContext.Files.Where(x => VM.Attachments.Contains(x.Id) && x.Class == user.Class).ToListAsync();
        if (!ModelState.IsValid)
        {
            VM.Event.AttachedFiles = files;
            return View(VM);
        }
        
        var ev = await dbContext.Events.Include(x=>x.AttachedFiles).FirstAsync(x => x.Id == evid);

        if (user.Role == UserRole.Admin || ev.Creator == user)
        {
            ev.Title = VM.Event.Title;
            ev.Date = VM.Event.Date;
            ev.StartTime = VM.Event.StartTime;

            if (ev.Type == EventType.Event)
            {
                ev.EndTime = VM.Event.EndTime;
            }
            ev.ModifyTime = DateTimeOffset.Now;
            ev.Modifier = user;
            ev.Description = VM.Event.Description;
            ev.AttachedFiles = files;

            await dbContext.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpGet("event/{id:guid}/setassociated")]
    public async Task<IActionResult> SetAssociated(EventId id)
    {
        var user = await dbContext.Users.Include(x=>x.AssociatedEvents).FirstAsync(x=>x.Id == this.GetUserId());
        var ev =  await dbContext.Events.FirstAsync(x=>x.Id == id);
        if (!user.AssociatedEvents.Contains(ev))
        {
            user.AssociatedEvents.Add(ev);
        }
        
        await dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("event/{id:guid}/removeassociated")]
    public async Task<IActionResult> RemoveAssociated(EventId id)
    {
        var user = await dbContext.Users.Include(x => x.AssociatedEvents).FirstAsync(x => x.Id == this.GetUserId());
        var ev = await dbContext.Events.FirstAsync(x => x.Id == id);
        user.AssociatedEvents.Remove(ev);
        await dbContext.SaveChangesAsync();
        return Ok();
    }

    [HttpGet("createevent")]
    public async Task<IActionResult> CreateEvent()
    {
        var uid = this.GetUserId();
        var user = await dbContext.Users.Include(x => x.Groups).Include(x => x.Class).FirstAsync(x => x.Id == uid);
        var defgr = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == user.Class.DefaultGroupId);
        if (defgr is null) return NotFound();
        CreateEventViewModel VM = new CreateEventViewModel() { Groups = user.Groups!.Concat(new Group[] { defgr }).ToArray(), EvType = EventType.Event };

        return View(VM);
    }

    [HttpPost("createevent")]
    public async Task<IActionResult> CreateEvent(CreateEventViewModel VM)
    {
        var uid = this.GetUserId();
        var user = await dbContext.Users.Include(x=>x.Class).Include(x=>x.Groups).FirstAsync(x => x.Id == uid);
        
        var defgr = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == user.Class.DefaultGroupId);
        if (defgr is null) return NotFound();

        var files = await dbContext.Files.Where(x => VM.Attachments.Contains(x.Id) && x.Class == user.Class).ToListAsync();

        if (!ModelState.IsValid)
        {
            VM.Groups = user.Groups!.Concat(new Group[] { defgr }).ToArray();
            VM.EvType = EventType.Event;
            VM.Event.AttachedFiles = files;
            return View(VM);
        }

        GroupId grid = new GroupId(VM.SelectedGroupGuid);
        var group = await dbContext.Groups.Include(x => x.Subject).ThenInclude(x => x.Class).FirstOrDefaultAsync(x => x.Id == grid);
        
        
        Conversation con = new Conversation() { Name = VM.Event.Title, Group = group, IsOpen = true };
        dbContext.Conversations.Add(con);
        Event ev = new Event() { Description = VM.Event.Description, StartTime = VM.Event.StartTime, EndTime = VM.Event.EndTime, Date = VM.Event.Date, Title = VM.Event.Title, Conversation = con, CreateTime = DateTimeOffset.Now, Creator = user, Group = group, Type = EventType.Event };
        ev.AttachedFiles = files;
        dbContext.Events.Add(ev);
        await dbContext.SaveChangesAsync();
        
        return RedirectToAction("Index");
    }

    [HttpGet("createtask")]
    public async Task<IActionResult> CreateTask()
    {
        var uid = this.GetUserId();
        var user = await dbContext.Users.Include(x => x.Groups).Include(x => x.Class).FirstAsync(x => x.Id == uid);
        var defgr = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == user.Class.DefaultGroupId);
        if (defgr is null) return NotFound();
        CreateEventViewModel VM = new CreateEventViewModel() { Groups = user.Groups!.Concat(new Group[] { defgr }).ToArray(), EvType = EventType.Task };

        return View("CreateEvent", VM);
    }

    [HttpPost("createtask")]
    public async Task<IActionResult> CreateTask(CreateEventViewModel VM)
    {
        var uid = this.GetUserId();
        var user = await dbContext.Users.Include(x => x.Class).Include(x => x.Groups).FirstAsync(x => x.Id == uid);

        var defgr = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == user.Class.DefaultGroupId);
        if (defgr is null) return NotFound();

        var files = await dbContext.Files.Where(x => VM.Attachments.Contains(x.Id) && x.Class == user.Class).ToListAsync();

        if (!ModelState.IsValid)
        {
            VM.Groups = user.Groups!.Concat(new Group[] { defgr }).ToArray();
            VM.EvType = EventType.Task;
            VM.Event.AttachedFiles = files;
            return View("CreateEvent",VM);
        }

        GroupId grid = new GroupId(VM.SelectedGroupGuid);
        var group = await dbContext.Groups.Include(x => x.Subject).ThenInclude(x => x.Class).FirstOrDefaultAsync(x => x.Id == grid);

        if (group is null) return NotFound();
        Conversation con = new Conversation() { Name = VM.Event.Title, Group = group, IsOpen = true };
        dbContext.Conversations.Add(con);
        Event ev = new Event() { Description = VM.Event.Description, StartTime = VM.Event.StartTime, Date = VM.Event.Date, Title = VM.Event.Title, Conversation = con, CreateTime = DateTimeOffset.Now, Creator = user, Group = group, Type = EventType.Task };
        ev.AttachedFiles = files;
        dbContext.Events.Add(ev);
        await dbContext.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    [HttpGet("deleteevent/{evid:guid}")]
    public async Task<IActionResult> DeleteEvent(EventId evid)
    {
        var uid = this.GetUserId();
        var user = await dbContext.Users.FirstAsync(x => x.Id == uid);
        var ev = await dbContext.Events.FirstAsync(x => x.Id == evid);

        if (user == ev.Creator || user.Role == UserRole.Admin)
        {
            dbContext.Events.Remove(ev);
            await dbContext.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        return await Index(DateOnly.FromDateTime(DateTime.Today));
    }

    [HttpGet, Route("{start}")]
    public async Task<IActionResult> Index(DateOnly start)
    {
        var userId = this.GetUserId();
        User User = await dbContext.Users.Include(x=>x.Class).FirstAsync(x=>x.Id == userId);
        Event[] homeworks = await dbContext.Events.Where(x => x.Date > DateOnly.MinValue && x.Group.Members.Contains(User)).ToArrayAsync();
        var defGroup = await dbContext.Groups.FindAsync(User.Class.DefaultGroupId);
        IndexViewModel VM = new IndexViewModel() { Events = homeworks, DefaultConversation = defGroup.DefaultConversationId, StartDate = start };
        return View(VM);
    }

    [HttpGet("eventinfo/{evid:guid}")]
    public async Task<IActionResult> EventInfo(EventId evid)
    {
        var uid = this.GetUserId();
        var user = await dbContext.Users.FirstAsync(x => x.Id == uid);
        var ev = await dbContext.Events.Include(x=>x.AssociatedUsers).Include(x=>x.AttachedFiles).Include(x=>x.Creator).Include(x=>x.Modifier).FirstOrDefaultAsync(x => x.Id == evid);
        if (ev is null)
        {
            return NotFound();
        }
        EventInfoPartialViewModel VM = new EventInfoPartialViewModel() { CanModify = (user.Role == UserRole.Admin || user == ev.Creator), Event = ev, IsAssociated = ev.AssociatedUsers.Contains(user) };

        return PartialView("EventInfoPartial", VM);
    }

    [HttpGet("contributors")]
    public IActionResult Contributors()
    {
        return View();
    }

    class CalDataCell
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public string BackgroundColor { get; set; }
        public ExtendedCalDataCellProps ExtendedProps { get; set; }
    }

    class ExtendedCalDataCellProps
    {
        public string Type { get; set; }
    }
}
