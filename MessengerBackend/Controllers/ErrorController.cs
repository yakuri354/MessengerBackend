using Microsoft.AspNetCore.Mvc;

namespace MessengerBackend.Controllers
{
    [Route("/error/")]
    public class ErrorController : Controller
    {
        [Route("404")]
        public IActionResult PageNotFound() =>
            View();
    }
}