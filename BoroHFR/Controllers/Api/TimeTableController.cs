using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using BoroHFR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Controllers.Api
{
    [Route("api/[controller]")]
    [Authorize("User")]
    [ApiController]
    public class TimeTableController : ControllerBase
    {
        private readonly BoroHfrDbContext _dbContext;

        public TimeTableController(BoroHfrDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetTimetable(DateOnly startDate, DateOnly endDate)
        {
            var user = await _dbContext.Users.FindAsync(this.GetUserId());
            if (user is null)
                return Unauthorized();
            var lessons = (await _dbContext.Lessons.FromSql($"SELECT * FROM lessons WHERE GroupId IN (SELECT GroupsId FROM groupuser WHERE MembersId = {user.Id})").ToArrayAsync())
                .SelectMany(x=>x.GetLessonsInRange(startDate, endDate)).ToArray();
                
            return Ok(lessons);
        }


    }
}
