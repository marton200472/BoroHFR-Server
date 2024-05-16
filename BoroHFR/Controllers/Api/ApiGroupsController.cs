using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Xml;

namespace BoroHFR.Controllers.Api
{
    [Route("api/groups")]
    [Authorize("User")]
    public class ApiGroupsController : ControllerBase
    {
        private readonly BoroHfrDbContext _dbContext;

        public ApiGroupsController(BoroHfrDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public record ApiGroup(Guid id, string name, string subject, Guid chatId);

        [HttpGet("")]
        [Produces(typeof(ApiGroup[]))]
        public async Task<IActionResult> GetGroups()
        {
            var user = await _dbContext.Users.Include(x=>x.Class).SingleAsync(x=>x.Id == this.GetUserId());
            var groups = await _dbContext.Groups.Where(x=>x.Id==user.Class.DefaultGroupId || x.Members.Contains(user)).ToArrayAsync();
            return Ok(groups.Select(x=>new ApiGroup(x.Id.value,x.Name, x.Subject?.Name ?? "(nincs tárgy)", x.DefaultConversationId.value)));
        }
    }
}
