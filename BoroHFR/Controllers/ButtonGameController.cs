using BoroHFR.Controllers.Helpers;
using BoroHFR.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoroHFR.Controllers
{
    [Route("button")]
    [Authorize("User")]
    public class ButtonGameController : Controller
    {
        private readonly BoroHfrDbContext _dbContext;

        public ButtonGameController(BoroHfrDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _dbContext.Users.FirstAsync(x=>x.Id == this.GetUserId());

            return View(user.ButtonGameClicks);
        }

        [HttpGet("click")]
        public async Task<IActionResult> Click()
        {
            var user = await _dbContext.Users.FirstAsync(x => x.Id == this.GetUserId());
            user.ButtonGameClicks++;
            await _dbContext.SaveChangesAsync();
            return Ok(user.ButtonGameClicks);
        }
    }
}
