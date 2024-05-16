using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using BoroHFR.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace BoroHFR.Controllers.Api
{
    [Authorize("User")]
    [Route("api/chat")]
    public class ApiChatController : ControllerBase
    {
        private readonly BoroHfrDbContext _dbContext;

        public ApiChatController(BoroHfrDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public record ApiMessage(string message, string sender, bool isOwn, DateTimeOffset time);

        [HttpGet("{id:guid}")]
        [Produces(typeof(ApiMessage[]))]
        public async Task<IActionResult> GetConversation(ConversationId id)
        {
            var user = await _dbContext.Users.Include(x=>x.Class).SingleAsync(x=>x.Id == this.GetUserId());
            var defGroup = await _dbContext.Groups.Where(x => x.Id == user.Class.DefaultGroupId).FirstOrDefaultAsync();
            var conv = await _dbContext.Conversations
                                            .Include(x=>x.Messages)
                                            .ThenInclude(x=>x.Sender)
                                            .Where(x => x.Group==defGroup ||  x.Members.Contains(user) || (x.IsOpen && x.Group.Members.Contains(user)))
                                            .Where(x=>x.Id == id)
                                            .SingleOrDefaultAsync();
            if (conv is null)
            {
                return NotFound();
            }

            var messages = conv.Messages.OrderBy(x => x.SendTime).Select(x=>new ApiMessage(x.Message, x.Sender.Username, user == x.Sender, x.SendTime)).ToArray();
            return Ok(messages);
        }
    }
}
