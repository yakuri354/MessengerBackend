using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MessengerBackend.Models;
using MessengerBackend.Services;
using MessengerBackend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;

namespace MessengerBackend.Controllers
{
    [Route("/api/auth")]
    [ApiController]
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly CryptoService _cryptoService;
        private readonly PhoneNumberHelper _phoneHelper = new PhoneNumberHelper();

        private readonly UserService _userService;
        private readonly VerificationService _verificationService;

        public AuthController(
            UserService userService,
            VerificationService verificationService,
            AuthService authService,
            CryptoService cryptoService)
        {
            _userService = userService;
            _verificationService = verificationService;
            _authService = authService;
            _cryptoService = cryptoService;
        }

        /**
         * <summary>Sends a code to specified number via specified channel</summary>
         * <param name="number">Number</param>
         * <param name="channel">Channel</param>
         * <response code="200">Code sent</response>
         * <response code="400">Code sending error</response>
         * <response code="429">Too many attempts</response>
         */
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("sendCode")]
        public async Task<IActionResult> SendCode(string number, string channel)
        {
            var parsedNum = _phoneHelper.ParseNumber(number);

            await _verificationService.StartVerificationAsync(parsedNum, channel);

            return Ok();
        }

        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("verifyCode")]
        public async Task<IActionResult> VerifyCode(string number, string code)
        {
            var formattedNumber = _phoneHelper.ParseNumber(number);

            var success = await _verificationService.CheckVerificationAsync(formattedNumber, code);
            if (!success)
            {
                return Forbid();
            }

            return Ok(new
            {
                authToken = _cryptoService.CreateAuthJwt(HttpContext.Connection.RemoteIpAddress, formattedNumber),
                isRegistered = await _userService.Users.AnyAsync(u => u.Number == formattedNumber)
            });
        }

        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("register")]
        [Authorize(Policy = "AuthToken")]
        public async Task<IActionResult> Register(string firstName, string lastName, string username)
        {
            var newUser = await _userService.AddUserAsync(
                HttpContext.User.FindFirst("num").Value, firstName, lastName, username);

            if (newUser == null)
            {
                return Forbid();
            }

            var fingerprint = Request.Headers["X-Fingerprint"];
            var newSession = await _authService.AddSessionAsync(new Session
            {
                ExpiresAt = DateTime.UtcNow.AddDays(CryptoService.JwtOptions.RefreshTokenLifetimeDays),
                Fingerprint = StringValues.IsNullOrEmpty(fingerprint) ? fingerprint.ToString() : null,
                IPHash = SHA256.Create()
                    .ComputeHash(HttpContext.Connection.RemoteIpAddress.GetAddressBytes()),
                UserAgent = Request.Headers["User-Agent"],
                User = newUser,
                UpdatedAt = DateTime.UtcNow
            });
            return Ok(new
            {
                refreshToken = newSession.Entity.RefreshToken,
                refreshExpirationDate =
                    DateTimeOffset.UtcNow.AddDays(CryptoService.JwtOptions.RefreshTokenLifetimeDays)
                        .ToUnixTimeSeconds(),
                accessToken = _cryptoService.CreateAccessJwt(HttpContext.Connection.RemoteIpAddress,
                    newSession.Entity.User.UserPID)
            });
        }

        [HttpPost("login")]
        [Produces("application/json")]
        [Authorize(Policy = "AuthToken")]
        public async Task<IActionResult> Login()
        {
            var user = await _userService.Users.SingleOrDefaultAsync(
                u => u.Number == HttpContext.User.FindFirst("num").Value);
            if (user == null)
            {
                return Forbid();
            }

            var fingerprint = Request.Headers["X-Fingerprint"];
            var newSession = await _authService.AddSessionAsync(new Session
            {
                ExpiresAt = DateTime.UtcNow.AddDays(CryptoService.JwtOptions.RefreshTokenLifetimeDays),
                Fingerprint = StringValues.IsNullOrEmpty(fingerprint) ? fingerprint.ToString() : null,
                IPHash = SHA256.Create()
                    .ComputeHash(HttpContext!.Connection.RemoteIpAddress.GetAddressBytes()),
                UserAgent = Request.Headers["User-Agent"],
                User = user,
                UpdatedAt = DateTime.UtcNow
            });
            return Ok(new
            {
                refreshToken = newSession.Entity.RefreshToken,
                refreshExpirationDate =
                    DateTimeOffset.UtcNow.AddDays(CryptoService.JwtOptions.RefreshTokenLifetimeDays)
                        .ToUnixTimeSeconds(),
                accessToken = _cryptoService.CreateAccessJwt(HttpContext.Connection.RemoteIpAddress,
                    newSession.Entity.User.UserPID)
            });
        }

        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [HttpPost("refresh")]
        [Authorize(Policy = "RefreshTokenOnly")]
        public async Task<IActionResult> Refresh()
        {
            var token = Request.Headers[HeaderNames.Authorization];
            var fingerprint = Request.Headers["X-Fingerprint"];
            var session = await _authService.GetAndDeleteSessionAsync(token);
            if (session == null)
            {
                return Forbid();
            }

            if (session.Fingerprint != null && session.Fingerprint !=
                (StringValues.IsNullOrEmpty(fingerprint) ? fingerprint.ToString() : null)
                // || session.IPHash != HttpContext.Connection.RemoteIpAddress.GetAddressBytes()
                // || session.UserAgent != Request.Headers["User-Agent"]
            )
            {
                return Forbid();
            }

            if (session.ExpiresAt >= DateTime.UtcNow)
            {
                return Unauthorized();
            }

            var newSession = await _authService.AddSessionAsync(new Session
            {
                ExpiresAt = DateTime.UtcNow.AddDays(CryptoService.JwtOptions.RefreshTokenLifetimeDays),
                Fingerprint = StringValues.IsNullOrEmpty(fingerprint) ? fingerprint.ToString() : null,
                IPHash = session.IPHash,
                UserAgent = session.UserAgent,
                UpdatedAt = DateTime.Now,
                User = session.User
            });
            return Ok(new
            {
                refreshToken = newSession.Entity.RefreshToken,
                accessToken = _cryptoService.CreateAccessJwt(HttpContext.Connection.RemoteIpAddress,
                    newSession.Entity.User.UserPID)
            });
        }
    }
}