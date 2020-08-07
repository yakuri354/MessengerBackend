using System.Threading.Tasks;
using MessengerBackend.Errors;
using MessengerBackend.Services;
using MessengerBackend.Utils;
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
            var user = await _userService.Users.FirstOrDefaultAsync(
                u => u.UserPID == HttpContext.User.FindFirst("uid").Value);
            if (user == null) return NotFound();
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
        public async Task<IActionResult> EditMe()
        {
            var input = MyJsonDeserializer.DeserializeAnonymousType(Request.Body.GetString(), new
            {
                firstName = "",
                lastName = "",
                userName = "",
                bio = ""
            }, true);
            if (input == null || input.firstName == null && input.lastName == null && input.userName == null &&
                input.bio == null)
                throw new JsonParseException("No changes provided");
            var user = await _userService.Users.FirstOrDefaultAsync(
                u => u.UserPID == HttpContext.User.FindFirst("uid").Value);
            if (user == null) return NotFound();
            if (input.firstName != null) user.FirstName = input.firstName;

            if (input.lastName != null) user.LastName = input.lastName;

            if (input.userName != null) user.Username = input.userName;

            if (input.bio != null) user.Bio = input.bio;
            if (await _userService.SaveUserAsync(user))
                return Ok();
            return Forbid();
        }
    }
}