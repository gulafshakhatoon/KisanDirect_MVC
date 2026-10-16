using System.Web.Mvc;

namespace KisanDirect_MVC.Controllers
{
    public class WeatherController : Controller
    {
        public ActionResult Index()
        {
            // Login ke bina Weather page open nahi hoga
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Register", "Account");
            }

            return View();
        }
    }

}