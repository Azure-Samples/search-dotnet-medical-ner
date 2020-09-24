using System.Web.Mvc;

namespace WebMedSearch.Controllers
{
    public class PartialController : Controller
    {
        public ActionResult SearchBox()
        {
            return View();
        }

        public ActionResult SearchResults()
        {
            return View();
        }


        public ActionResult CheckBoxFacet()
        {
            return View();
        }

        public ActionResult MultiLevelFacet()
        {
            return View();
        }

    }
}