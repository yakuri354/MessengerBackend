using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using MessengerBackend.RealTime;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MessengerBackend.Controllers
{
    [Route("/api")]
    [ApiController]
    public class GeneralController : Controller
    {
        private readonly CryptoService _cryptoService;

        public GeneralController(CryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("getConnPrefs")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [return: Description("Realtime server IP and port")]
        public IActionResult GetConnectionPrefs()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return Ok(JsonSerializer.Serialize(new
            {
                realTimeServerIPv4 =
                    host.AddressList.FirstOrDefault(ip =>
                        ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "unknown",

                realTimeServerIPv6 =
                    host.AddressList.FirstOrDefault(ip =>
                        ip.AddressFamily == AddressFamily.InterNetworkV6)?.ToString() ?? "unknown",
                realTimePortIPv4 = RealTimeServer.PortV4.ToString(),
                realTimePortIPv6 = RealTimeServer.PortV6.ToString()
            }));
        }

        [Produces("application/octet-stream")]
        [return: Description("Gets RSA JWT Public Key in the PKCS#1 format")]
        [HttpGet("publicKey")]
        // Same
        public IActionResult GetPublicKey()
        {
            return Ok(_cryptoService.PublicKey.ExportRSAPublicKey());
        }
    }
    
    [Route("/error")]
    [ApiController]
    public class ErrorController : Controller
    {
        [Route("404")]
        public IActionResult PageNotFound()
        {
            // var originalPath = "unknown";
            // if (HttpContext.Items.ContainsKey("originalPath"))
            // {
            //     originalPath = HttpContext.Items["originalPath"] as string;
            // }

            return View();
        }
    }
}