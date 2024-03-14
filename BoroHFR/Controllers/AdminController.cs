using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using BoroHFR.Models;
using BoroHFR.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;

namespace BoroHFR.Controllers
{
    [Route("administration")]
    [Authorize("Admin")]
    public class AdminController : Controller
    {
        private readonly BoroHfrDbContext dbContext;

        public AdminController(BoroHfrDbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var uid = this.GetUserId();
            var user = await dbContext.Users.Include(x => x.Class).FirstAsync(x => x.Id == uid);
            var cl = user.Class;
            var subjects = await dbContext.Subjects.Where(x => x.Class == cl).ToArrayAsync();
            var groups = await dbContext.Groups.Where(x => x.Class == cl && x.Id != user.Class.DefaultGroupId && x.Subject != null).ToArrayAsync();
            var members = await dbContext.Users.Where(x => x.Class == cl).ToArrayAsync();
            var lessons = await dbContext.Lessons.Where(x => groups.Contains(x.Group)).ToArrayAsync();
            IndexViewModel VM = new IndexViewModel { Groups = groups, Members = members, Class = cl, Subjects = subjects, Lessons = lessons, Default = new Subject() { Class = cl, Name = "(nincs tárgy)", Groups = await dbContext.Groups.Where(x => x.Subject == null).ToListAsync() } };

            return View(VM);
        }

        [HttpGet("invite")]
        public IActionResult InviteUsers()
        {
            return View();
        }

        [HttpPost("invite")]
        public async Task<IActionResult> InviteUsers(InviteUsersViewModel data)
        {
            if (!ModelState.IsValid)
            {
                return View(data);
            }
            var user = await dbContext.Users.Include(x=>x.Class).FirstAsync(x=>x.Id == this.GetUserId());
            if (data.MultiUse)
            {
                var token = new InviteToken() { Class = user.Class, ValidUntil = data.Validity, Token = InviteToken.GenerateToken(), Usages = data.Quantity,Role=UserRole.User };
                dbContext.InviteTokens.Add(token);
                data.Tokens = [token.Token];
            }
            else
            {
                var tokens = Enumerable.Range(0, data.Quantity).Select(x => new InviteToken() { Class = user.Class, Role = UserRole.User, Token = InviteToken.GenerateToken(), Usages = 1, ValidUntil = data.Validity }).ToArray();
                dbContext.InviteTokens.AddRange(tokens);
                data.Tokens = tokens.Select(x => x.Token).ToArray();
            }
            await dbContext.SaveChangesAsync();
            return View(data);
        }


        [HttpGet("editgroup/{gid:guid}")]
        public async Task<IActionResult> EditGroup(GroupId gid)
        {
            var group = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == gid);
            EditGroupViewModel VM = new EditGroupViewModel() { Teacher = group.Teacher, GroupName = group.Name, GrId = group.Id };
            
            return View(VM);
        }

        [HttpPost("editgroup/{gid:guid}")]
        public async Task<IActionResult> EditGroup(GroupId gid, EditGroupViewModel VM)
        {
            if (!ModelState.IsValid)
            {
                return View(VM);
            }
            var group = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == gid);

            group.Name = VM.GroupName;
            group.Teacher = VM.Teacher;
            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet("deletegroup/{gid:guid}")]
        public async Task<IActionResult> DeleteGroup(GroupId gid)
        {
            var group = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == gid);

            dbContext.Groups.Remove(group);
            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet("creategroup")]
        public async Task<IActionResult> CreateGroup()
        {
            var uid = this.GetUserId();
            var user = await dbContext.Users.Include(x => x.Class).FirstAsync(x => x.Id == uid);
            var subjects = dbContext.Subjects.Where(x => x.Class == user.Class).ToArray();
            CreateGroupViewModel VM = new CreateGroupViewModel() { Subjects = subjects };

            return View(VM);
        }

        [HttpPost("creategroup")]
        public async Task<IActionResult> CreateGroup(CreateGroupViewModel VM)
        {
            var uid = this.GetUserId();
            var user = await dbContext.Users.Include(x => x.Class).FirstAsync(x => x.Id == uid);
            Subject? subj = null;
            if (VM.SelectedSubjectId is not null)
            {
                SubjectId subjid = new SubjectId(VM.SelectedSubjectId.Value);
                subj = await dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == subjid);
            }
            if (!ModelState.IsValid)
            {
                var subjects = dbContext.Subjects.Where(x => x.Class == user.Class).ToArray();
                VM.Subjects = subjects;
                return View(VM);
            }

            Group group = new Group() { Name = VM.Name, Class = user.Class, Subject = subj, Teacher = (subj is null ? "" : VM.Teacher) };
            dbContext.Groups.Add(group);
            await dbContext.SaveChangesAsync();
            Conversation cv = new Conversation() { Group = group, Name = group.Name + " Általános", IsOpen = true };
            dbContext.Conversations.Add(cv);
            await dbContext.SaveChangesAsync();
            group.DefaultConversationId = cv.Id;
            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet("editsubject/{sid:guid}")]
        public async Task<IActionResult> EditSubject(SubjectId SId)
        {
            var subject = await dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == SId);
            EditSubjectViewModel VM = new EditSubjectViewModel() { Name = subject.Name, SubjId = subject.Id };

            return View(VM);
        }

        [HttpPost("editsubject/{sid:guid}")]
        public async Task<IActionResult> EditSubject(SubjectId SId, EditSubjectViewModel VM)
        {
            if (!ModelState.IsValid)
            {
                return View(VM);
            }
            var subject = await dbContext.Subjects.FirstOrDefaultAsync(x => x.Id == SId);
            subject.Name = VM.Name;
            dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet("createsubject")]
        public async Task<IActionResult> CreateSubject()
        {
            return View();
        }

        [HttpPost("createsubject")]
        public async Task<IActionResult> CreateSubject(CreateGroupViewModel VM)
        {
            var uid = this.GetUserId();
            var user = await dbContext.Users.Include(x => x.Class).FirstAsync(x => x.Id == uid);
            Subject subject = new Subject() { Class = user.Class, Name = VM.Name };

            dbContext.Subjects.Add(subject);
            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet("createlesson")]
        public async Task<IActionResult> CreateLesson()
        {
            var uid = this.GetUserId();
            var user = await dbContext.Users.Include(x => x.Class).ThenInclude(x => x.Groups).FirstAsync(x => x.Id == uid);
            var cl = await dbContext.Classes.FirstOrDefaultAsync(x => x.Id == user.Class.Id);
            Group[] groups = await dbContext.Groups.Where(x => cl.Groups.Contains(x) && x.Id != user.Class.DefaultGroupId).ToArrayAsync();
            CreateLessonViewModel VM = new CreateLessonViewModel() { Groups = groups, FirstDate = DateOnly.FromDateTime(DateTime.Now), LastDate = DateOnly.FromDateTime(DateTime.Now), StartTime = TimeOnly.FromDateTime(DateTime.Now), EndTime = TimeOnly.FromDateTime(DateTime.Now) };
            return View(VM);
        }

        [HttpPost("createlesson")]
        public async Task<IActionResult> CreateLesson(CreateLessonViewModel VM)
        {
            if (!ModelState.IsValid)
            {
                var uid = this.GetUserId();
                var user = await dbContext.Users.Include(x => x.Class).ThenInclude(x => x.Groups).FirstAsync(x => x.Id == uid);
                var cl = await dbContext.Classes.FirstOrDefaultAsync(x => x.Id == user.Class.Id);
                Group[] groups = await dbContext.Groups.Where(x => cl.Groups.Contains(x)).ToArrayAsync();
                VM.Groups = groups;
                return View(VM);
            }
            var grid = new GroupId(VM.SelectedGroupGuid);
            var group = await dbContext.Groups.FirstOrDefaultAsync(x => x.Id == grid);
            DbLesson lesson = new DbLesson() { Label = VM.Label, StartTime = VM.StartTime, EndTime = VM.EndTime, Group = group, LastDate = VM.LastDate, RepeatWeeks = VM.RepeatWeeks == 0 ? null : VM.RepeatWeeks, FirstDate = VM.FirstDate };
            dbContext.Lessons.Add(lesson);
            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet("editlesson/{lesid:guid}")]
        public async Task<IActionResult> EditLesson(DbLessonId lesid)
        {
            var lesson = await dbContext.Lessons.FirstOrDefaultAsync(x => x.Id == lesid);
            EditLessonViewModel VM = new EditLessonViewModel() { Lesson = lesson };

            return View(VM);
        }

        [HttpPost("editlesson/{lesid:guid}")]
        public async Task<IActionResult> EditLesson(DbLessonId lesid, EditLessonViewModel VM)
        {
            if (!ModelState.IsValid)
            {
                return View(VM);
            }
            var lesson = await dbContext.Lessons.FirstOrDefaultAsync(x => x.Id == lesid);

            lesson.Label = VM.Lesson.Label;
            lesson.FirstDate = VM.Lesson.FirstDate;
            lesson.LastDate = VM.Lesson.LastDate;
            lesson.RepeatWeeks = VM.Lesson.RepeatWeeks;
            lesson.StartTime = VM.Lesson.StartTime;
            lesson.EndTime = VM.Lesson.EndTime;
            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }

        [HttpGet("deletelesson/{lesid:guid}")]
        public async Task<IActionResult> DeleteLesson(DbLessonId lesid)
        {
            var lesson = await dbContext.Lessons.FirstOrDefaultAsync(x => x.Id == lesid);

            dbContext.Lessons.Remove(lesson);
            await dbContext.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
