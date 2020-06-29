using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using MessengerBackend.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MessengerBackend.Controllers
{
    [Route("/api")]
    [ApiController]
    public class GeneralController
    {
        private readonly CryptoService _cryptoService;

        public GeneralController(CryptoService cryptoService)
        {
            _cryptoService = cryptoService;
        }

        [HttpGet("getConnPrefs")]
        public IActionResult GetConnectionPrefs()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            // Can be changed for your needs
            // IDE would not let me use "Ok()", I'm sorry
            return new OkObjectResult(JsonConvert.SerializeObject(new
            {
                realTimeServerIPv4 =
                    host.AddressList.FirstOrDefault(ip =>
                        ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "unknown",

                realTimeServerIPv6 =
                    host.AddressList.FirstOrDefault(ip =>
                        ip.AddressFamily == AddressFamily.InterNetworkV6)?.ToString() ?? "unknown",
                realTimePortIPv4 = RealTime.RealTimeServer.PortV4.ToString(),
                realTimePortIPv6 = RealTime.RealTimeServer.PortV6.ToString()
            }));
        }

        [HttpGet("publicKey")]
        // Same
        public IActionResult GetPublicKey() => new OkObjectResult(_cryptoService.PublicKey.ExportRSAPublicKey());
    }
}