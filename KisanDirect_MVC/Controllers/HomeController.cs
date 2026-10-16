using System.Web.Mvc;

namespace KisanDirect_MVC.Controllers
{
    public class HomeController : Controller
    {
        // Homepage
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Register", "Account");
            }

            return View();
        }

        // About page
        public ActionResult About()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Register", "Account");
            }

            return View();
        }

        // Contact page
        public ActionResult Contact()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Register", "Account");
            }

            return View();
        }

        // Research page
        public ActionResult Research()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Register", "Account");
            }

            return View();
        }
    }

}