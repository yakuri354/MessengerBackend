using Microsoft.AspNetCore.Mvc;

namespace MessengerBackend.Controllers
{
    [Route("/error/")]
    public class ErrorController : Controller
    {
        [Route("404")]
        public IActionResult PageNotFound() =>
            // var originalPath = "unknown";
            // if (HttpContext.Items.ContainsKey("originalPath"))
            // {
            //     originalPath = HttpContext.Items["originalPath"] as string;
            // }
            View();
    }
}