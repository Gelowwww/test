using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace lrsms.Controllers
{
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        [Route("/Error/Handle/{code:int}")]
        public IActionResult Handle(int code)
        {
            if(code == 404)
            {
                ViewBag.Message = "The page you are looking for does not exist!";
                ViewData["Title"] = "Page not found";
            }
            else if(code == 401)
            {
                ViewBag.Message = "You are not authorized to perform this action.";
                ViewData["Title"] = "Unauthorized";
            }

            return View("~/Views/Shared/Error/Handle.cshtml");
        }

    }
}