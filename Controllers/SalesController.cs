using CRMSystem.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CRMSystem.Controllers
{
    public class SalesController : Controller
    {
        private readonly IAuthService _authService;

        public SalesController(IAuthService authService)
        {
            _authService = authService;
        }

        public IActionResult Index()
        {
            return View();
        }
    }
}