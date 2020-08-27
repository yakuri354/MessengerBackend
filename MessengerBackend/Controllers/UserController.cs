using System.Threading.Tasks;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace MessengerBackend.Controllers
{
    [ApiController]
    [Authorize]
    [Microsoft.AspNetCore.Mvc.Route("/api/users")]
    public class UserController : Controller
    {
        private readonly UserService _userService;

        public UserController(UserService userService) => _userService = userService;

        [HttpGet("me")]
        [Produces("application/json")]
        public async Task<IActionResult> Me()
        {
            var user = await _userService.Users.SingleOrDefaultAsync(
                u => u.UserPID == HttpContext.User.FindFirst("uid").Value);
            if (user == null)
            {
                return NotFound();
            }

            return Ok(new
            {
                username = user.Username ?? "",
                firstName = user.FirstName,
                lastName = user.LastName,
                number = user.Number,
                bio = user.Bio ?? "",
                avatarUrl = user.AvatarUrl ?? ""
            });
        }

        [HttpPost("me")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public async Task<IActionResult> EditMe(string? firstName, string? lastName, string? userName, string? bio)
        {
            if (firstName == null && lastName == null && userName == null && bio == null)
            {
                return BadRequest("No changes provided");
            }

            var user = await _userService.Users.SingleOrDefaultAsync(
                u => u.UserPID == HttpContext.User.FindFirst("uid").Value);
            if (user == null)
            {
                return NotFound();
            }

            if (firstName != null)
            {
                user.FirstName = firstName;
            }

            if (lastName != null)
            {
                user.LastName = lastName;
            }

            if (userName != null)
            {
                user.Username = userName;
            }

            if (bio != null)
            {
                user.Bio = bio;
            }

            if (await _userService.SaveUserAsync(user))
            {
                return Ok();
            }

            return Forbid();
        }
    }
}