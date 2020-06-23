using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MessengerBackend.Controllers
{
    [Route("/api")]
    [ApiController]
    public class GeneralController
    {
        [HttpGet("getConnPrefs")]
        public IActionResult GetConnectionPrefs()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            // Can be changed for your needs
            // IDE would not let me use "Ok()", I'm sorry
            return new OkObjectResult(JsonConvert.SerializeObject(new Dictionary<string, string>
            {
                {
                    "realTimeServerIPv4",
                    host.AddressList.FirstOrDefault(ip =>
                        ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? "unknown"
                },
                {
                    "realTimeServerIPv6",
                    host.AddressList.FirstOrDefault(ip =>
                        ip.AddressFamily == AddressFamily.InterNetworkV6)?.ToString() ?? "unknown"
                },
                { "realTimePortIPv4", RealTime.RealTimeServer.PortV4.ToString() },
                { "realTimePortIPv6", RealTime.RealTimeServer.PortV6.ToString() }
            }));
        }
    }
}