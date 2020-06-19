using System.Text.Json;
using MessengerBackend.Models;
using MessengerBackend.Serialization.Auth;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Mvc;


namespace MessengerBackend.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("reg", Name = "Register")]
        public ActionResult Register()
        {
            return Ok(JsonSerializer.Serialize(new UserBuf
            {
                FirstName = "John",
                LastName = "Doe",
                Username = "jdoe"
            }));
        }
    }
}