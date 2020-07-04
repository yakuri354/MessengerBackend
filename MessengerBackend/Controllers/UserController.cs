using System.Text.Json;
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

        public UserController(UserService userService)
        {
            _userService = userService;
        }

        public IActionResult SearchForUser()
        {
            return Ok();
        }
    }

    [ApiController]
    [Microsoft.AspNetCore.Mvc.Route("/api/users/me")]
    public class MeController : Controller
    {
        private readonly UserService _userService;

        public MeController(UserService userService)
        {
            _userService = userService;
        }

        [Authorize]
        [HttpGet("getProfile")]
        public IActionResult Me()
        {
            var user = _userService.FindOneStrict(uid: HttpContext.User.FindFirst("uid").Value);
            if (user == null) return NotFound();
            return Ok(JsonSerializer.Serialize(new
            {
                username = user.Username,
                firstName = user.FirstName,
                lastName = user.LastName,
                number = user.Number,
                bio = user.Bio ?? "",
                avatarUrl = user.AvatarUrl ?? ""
            }));
        }

        [Authorize]
        [HttpPost("editProfile")]
        public IActionResult EditProfile()
        {
            var input = MyJsonDeserializer.DeserializeAnonymousType(Request.Body.GetString(), new
            {
                firstName = "",
                lastName = "",
                userName = "",
                bio = ""
            }, true);
            var user = _userService.FindOneStrict(HttpContext.User.FindFirst("uid").Value);
            if (input.firstName != null) user.FirstName = input.firstName;

            if (input.lastName != null) user.LastName = input.lastName;

            if (input.userName != null) user.Username = input.lastName;

            if (input.bio != null) user.Bio = input.bio;
            _userService.SaveUser(user);
            return Ok();
        }
    }
}