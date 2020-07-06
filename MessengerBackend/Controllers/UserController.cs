using MessengerBackend.Services;
using MessengerBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Route = Microsoft.AspNetCore.Mvc.RouteAttribute;

namespace MessengerBackend.Controllers
{
    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("/api/users")]
    public class UserController : Controller
    {
        private readonly UserService _userService;

        public UserController(UserService userService) => _userService = userService;

        public IActionResult SearchForUser() => Ok();
    }

    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("/api/users/me")]
    [Produces("application/json")]
    public class MeController : Controller
    {
        private readonly UserService _userService;

        public MeController(UserService userService) => _userService = userService;

        [Authorize]
        [HttpGet("getProfile")]
        [Produces("application/json")]
        public IActionResult Me()
        {
            var user = _userService.FirstOrDefault(
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

        [Authorize]
        [HttpPost("editProfile")]
        [Consumes("application/json")]
        [Produces("application/json")]
        public IActionResult EditProfile()
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
                return BadRequest();
            var user = _userService.FirstOrDefault(
                u => u.UserPID == HttpContext.User.FindFirst("uid").Value);
            if (user == null) return NotFound();
            if (input.firstName != null) user.FirstName = input.firstName;

            if (input.lastName != null) user.LastName = input.lastName;

            if (input.userName != null) user.Username = input.lastName;

            if (input.bio != null) user.Bio = input.bio;
            if (_userService.SaveUser(user))
                return Ok();
            return Forbid();
        }
    }
}