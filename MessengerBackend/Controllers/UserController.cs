using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MessengerBackend.Controllers
{
    [Route("/api/auth")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly UserService _userService;
        private readonly VerificationService _verificationService;

        private List<NumberBan> _numberBans;

        public UserController(UserService userService, VerificationService verificationService)
        {
            _userService = userService;
            _verificationService = verificationService;
        }

        [HttpPost("verifyCode")]
        public async Task<IActionResult> VerifyCode()
        {
            var input = new
            {
                number = "",
                code = ""
            };
            try
            {
                input = JsonConvert.DeserializeAnonymousType(
                    Request.Body.ToString(), input);
            }
            catch (JsonException)
            {
                return BadRequest();
            }

            if (input.code.Length != _verificationService.TwilioService.CodeLength) return Forbid();


            var result = await _verificationService.CheckVerificationAsync(input.number, input.code);
            if (!result.IsValid) return BadRequest(result.Error);
            var newUser = _userService.Add(input.number);
            return Ok(JsonConvert.SerializeObject(new
            {
                uid = newUser.PublicUID
            }));
        }

        [HttpPost("sendCode")]
        public async Task<IActionResult> SendCode()
        {
            var input = new
            {
                number = "",
                channel = ""
            };
            try
            {
                input = JsonConvert.DeserializeAnonymousType(Request.Body.GetString(), input);
            }
            catch (JsonException)
            {
                return BadRequest();
            }

            if (!Regex.Match(input.number, @"^\+[1-9]\d{1,14}$").Success)
                return BadRequest();
            var currentBan = _numberBans.FirstOrDefault(q => q.Number == input.number);
            if (currentBan != null)
            {
                Response.Headers.Add("Retry-After", (currentBan.ExpiresAt - DateTime.Now).Seconds.ToString());
                return new StatusCodeResult((int) HttpStatusCode.TooManyRequests);
            }

            var result = await _verificationService.StartVerificationAsync(input.number, input.channel);
            if (!result.IsValid) return BadRequest(result.Error);

            var ban = new NumberBan
            {
                Number = input.number,
                ExpirationTimer = new Timer(_verificationService.ResendInterval),
                ExpiresAt = DateTime.Now.AddSeconds(_verificationService.ResendInterval)
            };

            _numberBans.Add(ban);
            ban.ExpirationTimer.Elapsed += (source, e) => { _numberBans.Remove(ban); };
            ban.ExpirationTimer.Enabled = true;

            return Ok();
        }

        // [HttpPost("resendCode")]
        // public async Task<IActionResult> ResendCode()
        // {
        //     
        // }

        private class NumberBan
        {
            public string Number;
            public Timer ExpirationTimer;
            public DateTime ExpiresAt;
        }
    }
}