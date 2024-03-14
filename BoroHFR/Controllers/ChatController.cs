using Microsoft.AspNetCore.Mvc;
using BoroHFR.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BoroHFR.Models;

namespace BoroHFR.Controllers;

[Route("chat")]
[Authorize("User")]
public class ChatController : Controller
{
    private readonly BoroHfrDbContext _dbContext;

    public ChatController(BoroHfrDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public IActionResult Chat()
    {
        return View();
    }

    [HttpGet("{convId:guid}")]
    public async Task<IActionResult> ChatPartial(ConversationId convId)
    {
        var conv = await _dbContext.Conversations.Include(c=>c.Messages).ThenInclude(msg=>msg.Sender).Include(c=>c.Messages).ThenInclude(msg=>msg.Attachments).AsSplitQuery().FirstOrDefaultAsync(x=>x.Id==convId);
        if (conv is null)
            return NotFound();

        return PartialView("_ChatPartial",conv);
    }
}