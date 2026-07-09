using Microsoft.AspNetCore.Mvc;
using SchoolPsychologist.Models;
using System.Diagnostics;

namespace SchoolPsychologist.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

       
        #region Dashboard
        public IActionResult Index()
        {
         
            return View();
        }
        #endregion
      
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
