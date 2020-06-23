using System;
using System.Text.RegularExpressions;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessengerBackend.Controllers
{
    [Route("/api/auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly UserService _userService;

        public AuthController(UserService userService)
        {
            _userService = userService;
        }

        [HttpPost("completeRegister")]
        public IActionResult CompleteRegister()
        {
            return Ok();
        }

        [HttpPost("sendCode")]
        public IActionResult SendCode([FromBody]NumberInput number)
        {
            if (!ModelState.IsValid || !Regex.Match(number.number, @"^\+[1-9]\d{1,14}$").Success )
                return BadRequest();
            return Ok();
        }
        public struct NumberInput
        {
            public string number;
        }
    }
}

namespace MessengerBackend.Models
{
    public class OngoingRegister
    {
        public int ID { get; set; }
        public string Number { get; set; }
        public DateTime CanResendAt { get; set; }
    }
}