using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoroHFR.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize("User")]
    public class UserDataController : ControllerBase
    {
        [HttpGet("userid")]
        public IActionResult GetUserId()
        {
            var uid = User.Claims.FirstOrDefault(x=>x.Type=="Id");
            if (uid is null)
            {
                return Unauthorized();
            }
            return Ok(uid.Value);
        }
    }
}
