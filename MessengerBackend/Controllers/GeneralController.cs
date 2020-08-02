using MessengerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace MessengerBackend.Controllers
{
    [Route("/api")]
    [ApiController]
    public class GeneralController : Controller
    {
        private readonly CryptoService _cryptoService;

        public GeneralController(CryptoService cryptoService) => _cryptoService = cryptoService;

        [HttpGet]
        public IActionResult Index() => View();

// #if USERSA
//         [Produces("application/octet-stream")]
//         [return: Description("Gets RSA JWT Public Key in the PKCS#1 format")]
//         [HttpGet("publicKey")]
//         // Same
//         public IActionResult GetPublicKey()
//         {
//             return Ok(_cryptoService.PublicKey.ExportRSAPublicKey());
//         }
// #endif
    }
    
}